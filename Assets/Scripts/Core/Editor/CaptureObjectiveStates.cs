using System.Collections.Generic;
using System.IO;
using ProjectAstra.Core.Grid;
using ProjectAstra.Core.UI.BattleMap.HUD;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace ProjectAstra.EditorTools
{
    // ==========================================================================================
    // Renders the objectives panel in the states from the spec's §C matrix.
    //
    // Every state is produced by loading a real MapData through the real ObjectiveController, so what
    // gets captured is what the game would show for that map - not a hand-built model. The tile panel
    // round shipped a bug precisely because its capture asserted its own model.
    //
    // The panel is cloned out of BattleMap onto a throwaway camera-space canvas; the scene is opened
    // additively and closed without saving.
    //
    // Run via 'Project Astra/Capture Objective States'.
    // ==========================================================================================
    public static class CaptureObjectiveStates
    {
        const string ScenePath = "Assets/Scenes/BattleMap.unity";
        const string OutputDir = "Assets/Screenshots";
        const int Width = 1920, Height = 1080;

        [MenuItem("Project Astra/Capture Objective States")]
        public static void Capture()
        {
            Scene scene = OpenAdditively(out bool openedHere);
            ObjectiveView source = FindView(scene);
            if (source == null)
            {
                Debug.LogError("[CaptureObjectiveStates] No ObjectiveView in " + ScenePath);
                if (openedHere) EditorSceneManager.CloseScene(scene, true);
                return;
            }

            Directory.CreateDirectory(OutputDir);
            Stage stage = BuildStage();

            var clone = (GameObject)Object.Instantiate(source.gameObject, stage.canvas.transform);
            clone.SetActive(true);
            var view = clone.GetComponent<ObjectiveView>();

            foreach (var state in States())
            {
                ShowBanner(view, state.open);
                view.Render(state.model);
                Shoot(stage, state.file);
            }

            Object.DestroyImmediate(clone);
            TearDown(stage);
            if (openedHere) EditorSceneManager.CloseScene(scene, true);

            AssetDatabase.Refresh();
            Debug.Log("[CaptureObjectiveStates] Wrote state captures to " + OutputDir);
        }

        // Goes through the real SetPeek so the view's own peek state drives where Render parks the
        // banner. The slide coroutine does not tick in edit mode, which is exactly what we want -
        // the banner lands on its end position with no interpolation.
        static void ShowBanner(ObjectiveView view, bool open)
        {
            view.SetPeek(open);
            if (view.Expanded != null) view.Expanded.SetActive(open);
        }

        // ---- the §C matrix ---------------------------------------------------------------------

        struct State
        {
            public string file;
            public ObjectiveModel model;
            public bool open;
        }

        static IEnumerable<State> States()
        {
            // Collapsed, both corners: tab only.
            yield return Build("obj_1_collapsed_right", HudCorner.TopRight, false, null);
            yield return Build("obj_2_collapsed_left", HudCorner.TopLeft, false, null);

            // Held open, simple map: win + lose, no objectives authored.
            yield return Build("obj_3_open_simple", HudCorner.TopRight, true, null);

            // Held open, loaded map: one complete, one with a counter, one long enough to wrap.
            yield return Build("obj_4_open_loaded", HudCorner.TopRight, true, new[]
            {
                new SecondaryObjective { text = "Open every chest", complete = true },
                new SecondaryObjective { text = "Recruit the mercenary", current = 2, max = 5 },
                new SecondaryObjective { text = "Keep every villager alive until the bridge is held" },
            });

            // Same, docked left, to check the tab and banner mirror.
            yield return Build("obj_5_open_loaded_left", HudCorner.TopLeft, true, new[]
            {
                new SecondaryObjective { text = "Open every chest", complete = true },
                new SecondaryObjective { text = "Recruit the mercenary", current = 2, max = 5 },
            });
        }

        static State Build(string file, HudCorner corner, bool open, SecondaryObjective[] objectives)
        {
            var map = ScriptableObject.CreateInstance<MapData>();
            var so = new SerializedObject(map);
            so.FindProperty("width").intValue = 1;
            so.FindProperty("height").intValue = 1;
            so.FindProperty("terrain").arraySize = 1;
            so.FindProperty("winConditionText").stringValue = "Rout the enemy";
            so.FindProperty("loseConditionText").stringValue = "Arjun falls";

            var list = so.FindProperty("secondaryObjectives");
            list.arraySize = objectives != null ? objectives.Length : 0;
            for (int i = 0; i < list.arraySize; i++)
            {
                var e = list.GetArrayElementAtIndex(i);
                e.FindPropertyRelative("text").stringValue = objectives[i].text;
                e.FindPropertyRelative("complete").boolValue = objectives[i].complete;
                e.FindPropertyRelative("current").intValue = objectives[i].current;
                e.FindPropertyRelative("max").intValue = objectives[i].max;
            }
            so.ApplyModifiedPropertiesWithoutUndo();

            MapService.Load(map, null);
            var controller = new ObjectiveController(null);
            ObjectiveModel model = controller.objectiveModel;
            model.Corner = corner;

            Object.DestroyImmediate(map);
            return new State { file = file, model = model, open = open };
        }

        // ---- staging ---------------------------------------------------------------------------

        class Stage
        {
            public GameObject cameraHolder;
            public Camera camera;
            public Canvas canvas;
            public RenderTexture target;
        }

        static Stage BuildStage()
        {
            var stage = new Stage();

            stage.cameraHolder = new GameObject("__ObjCaptureCamera");
            stage.camera = stage.cameraHolder.AddComponent<Camera>();
            stage.camera.clearFlags = CameraClearFlags.SolidColor;
            stage.camera.backgroundColor = new Color32(0x3E, 0x4A, 0x3C, 0xFF);   // stands in for the map
            stage.camera.orthographic = true;

            stage.target = new RenderTexture(Width, Height, 24, RenderTextureFormat.ARGB32);
            stage.camera.targetTexture = stage.target;

            var canvasHolder = new GameObject("__ObjCaptureCanvas");
            stage.canvas = canvasHolder.AddComponent<Canvas>();
            stage.canvas.renderMode = RenderMode.ScreenSpaceCamera;
            stage.canvas.worldCamera = stage.camera;
            stage.canvas.planeDistance = 10f;

            var scaler = canvasHolder.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(Width, Height);
            scaler.matchWidthOrHeight = 0.5f;
            return stage;
        }

        static void TearDown(Stage stage)
        {
            stage.camera.targetTexture = null;
            RenderTexture.active = null;
            Object.DestroyImmediate(stage.canvas.gameObject);
            Object.DestroyImmediate(stage.cameraHolder);
            stage.target.Release();
            Object.DestroyImmediate(stage.target);
        }

        static void Shoot(Stage stage, string name)
        {
            Canvas.ForceUpdateCanvases();
            LayoutRebuilder.ForceRebuildLayoutImmediate(stage.canvas.GetComponent<RectTransform>());
            Canvas.ForceUpdateCanvases();

            stage.camera.Render();

            RenderTexture.active = stage.target;
            var shot = new Texture2D(Width, Height, TextureFormat.RGBA32, false);
            shot.ReadPixels(new Rect(0, 0, Width, Height), 0, 0);
            shot.Apply();
            RenderTexture.active = null;

            File.WriteAllBytes($"{OutputDir}/{name}.png", shot.EncodeToPNG());
            Object.DestroyImmediate(shot);
        }

        static Scene OpenAdditively(out bool openedHere)
        {
            for (int i = 0; i < SceneManager.sceneCount; i++)
            {
                Scene loaded = SceneManager.GetSceneAt(i);
                if (loaded.path == ScenePath && loaded.isLoaded)
                {
                    openedHere = false;
                    return loaded;
                }
            }
            openedHere = true;
            return EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Additive);
        }

        static ObjectiveView FindView(Scene scene)
        {
            foreach (GameObject root in scene.GetRootGameObjects())
            {
                var found = root.GetComponentInChildren<ObjectiveView>(true);
                if (found != null) return found;
            }
            return null;
        }
    }
}
