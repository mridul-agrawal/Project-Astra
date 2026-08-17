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
    // Renders the tile info panel in every state from the spec's §8 matrix, so the restyle can be
    // checked against the document row by row.
    //
    // The panel is cloned out of BattleMap onto a throwaway camera-space canvas. The scene is
    // opened additively and closed without saving, so nothing in it is disturbed - and the clone
    // is driven through the real TileInfoView.Render, not a copy of its logic.
    //
    // Run via 'Project Astra/Capture Tile Info States'.
    // ==========================================================================================
    public static class CaptureTileInfoStates
    {
        const string ScenePath = "Assets/Scenes/BattleMap.unity";
        const string OutputDir = "Assets/Screenshots";
        const int Width = 1920, Height = 1080;

        [MenuItem("Project Astra/Capture Tile Info States")]
        public static void Capture()
        {
            Scene scene = OpenAdditively(out bool openedHere);
            TileInfoView source = FindView(scene);
            if (source == null)
            {
                Debug.LogError("[CaptureTileInfoStates] No TileInfoView in " + ScenePath);
                if (openedHere) EditorSceneManager.CloseScene(scene, true);
                return;
            }

            Directory.CreateDirectory(OutputDir);
            Stage stage = BuildStage();

            var clone = (GameObject)Object.Instantiate(source.gameObject, stage.canvas.transform);
            clone.SetActive(true);
            var view = clone.GetComponent<TileInfoView>();

            foreach (var state in States())
            {
                view.Render(state.model);
                Shoot(stage, state.file);
            }

            Object.DestroyImmediate(clone);
            TearDown(stage);

            if (openedHere) EditorSceneManager.CloseScene(scene, true);
            AssetDatabase.Refresh();
            Debug.Log("[CaptureTileInfoStates] Wrote state captures to " + OutputDir);
        }

        // Reports the resolved geometry of every rectangle in the panel, which is the quickest way
        // to find a zero-sized layout or an image that never got switched on.
        [MenuItem("Project Astra/Diagnose Tile Info Panel")]
        public static void Diagnose()
        {
            Scene scene = OpenAdditively(out bool openedHere);
            TileInfoView source = FindView(scene);
            if (source == null)
            {
                Debug.LogError("[DiagnoseTileInfo] No TileInfoView found.");
                return;
            }

            Stage stage = BuildStage();
            var clone = (GameObject)Object.Instantiate(source.gameObject, stage.canvas.transform);
            clone.SetActive(true);
            var view = clone.GetComponent<TileInfoView>();

            var strip = new TileEffectStrip();
            strip.Chips.Add(TileEffectChip.Stat("Avo", 30));
            var model = new TileInfoModel { TerrainName = "Protection Tile", Corner = HudCorner.BottomLeft };
            model.Strips.Add(strip);

            view.Render(model);
            Canvas.ForceUpdateCanvases();
            LayoutRebuilder.ForceRebuildLayoutImmediate(clone.GetComponent<RectTransform>());
            Canvas.ForceUpdateCanvases();

            var report = new System.Text.StringBuilder("[DiagnoseTileInfo]\n");
            report.AppendLine($"  canvas scaleFactor={stage.canvas.scaleFactor} rect={stage.canvas.GetComponent<RectTransform>().rect}");
            report.AppendLine($"  CanvasGroup alpha={(view.Fader != null ? view.Fader.alpha.ToString() : "no fader")}");
            Describe(report, clone.transform, 1);
            Debug.Log(report.ToString());

            Object.DestroyImmediate(clone);
            TearDown(stage);
            if (openedHere) EditorSceneManager.CloseScene(scene, true);
        }

        static void Describe(System.Text.StringBuilder report, Transform t, int depth)
        {
            var rect = t as RectTransform;
            string pad = new string(' ', depth * 2);
            var graphic = t.GetComponent<Graphic>();
            string art = graphic == null
                ? ""
                : $" [{graphic.GetType().Name} enabled={graphic.enabled} colorA={graphic.color.a:0.00}" +
                  $" sprite={(graphic is Image img && img.sprite != null ? img.sprite.name : "-")}]";

            report.AppendLine($"{pad}{t.name} active={t.gameObject.activeSelf}" +
                              (rect != null ? $" size={rect.rect.size} pos={rect.anchoredPosition}" : "") + art);

            for (int i = 0; i < t.childCount; i++)
                Describe(report, t.GetChild(i), depth + 1);
        }

        // ---- the §8 matrix ---------------------------------------------------------------------

        struct State
        {
            public string file;
            public TileInfoModel model;
        }

        static IEnumerable<State> States()
        {
            yield return Make("tile_1_plain", "Plain", HudCorner.BottomLeft);

            yield return Make("tile_2_single_buff", "Forest", HudCorner.BottomLeft,
                TileEffectChip.Stat("Avo", 20), TileEffectChip.Stat("Def", 1));

            yield return Make("tile_3_multi_buff_flag", "Protection Tile", HudCorner.BottomLeft,
                TileEffectChip.Stat("Avo", 30), TileEffectChip.Stat("Heal/Turn", 10),
                TileEffectChip.Flag("Unbreakable"));

            yield return Make("tile_4_hazard", "Ground + Flames", HudCorner.BottomLeft,
                TileEffectChip.Stat("Damage/Turn", -10));

            // Derived from the authored terrain table through the real controller, not hand-built.
            // The hand-built version of this row is what hid the Impassable bug: it showed the flag
            // in a screenshot while the live game could never produce it.
            foreach (var real in new[] { TerrainType.River, TerrainType.Rock, TerrainType.Campfire,
                                         TerrainType.Village, TerrainType.Mountain })
                yield return FromTerrain("tile_5_real_" + real.ToString().ToLower(), real);

            yield return Make("tile_6_long_name",
                "Destructible Wall + Campfire Remains", HudCorner.BottomLeft,
                TileEffectChip.Flag("Impassable"));

            // Same content docked right, to check the §3 ragged edges mirror.
            yield return Make("tile_7_mirrored_right", "Protection Tile", HudCorner.BottomRight,
                TileEffectChip.Stat("Avo", 30), TileEffectChip.Stat("Heal/Turn", 10),
                TileEffectChip.Flag("Unbreakable"));
        }

        // Loads the authored terrain table against a one-tile map and runs the panel's own
        // derivation, so what gets captured is exactly what the game would show for that terrain.
        static State FromTerrain(string file, TerrainType terrain)
        {
            var table = AssetDatabase.LoadAssetAtPath<TerrainStatTable>(
                "Assets/ScriptableObjects/Map/TerrainStatTable.asset");

            var map = ScriptableObject.CreateInstance<MapData>();
            var so = new SerializedObject(map);
            so.FindProperty("width").intValue = 1;
            so.FindProperty("height").intValue = 1;
            so.FindProperty("terrain").arraySize = 1;
            so.FindProperty("terrain").GetArrayElementAtIndex(0).intValue = (int)terrain;
            so.ApplyModifiedPropertiesWithoutUndo();

            MapService.Load(map, table);
            TileInfoModel model = new TileInfoController(null).BuildModel(Vector2Int.zero, HudCorner.BottomLeft);

            Object.DestroyImmediate(map);
            return new State { file = file, model = model };
        }

        static State Make(string file, string name, HudCorner corner, params TileEffectChip[] chips)
        {
            var model = new TileInfoModel { TerrainName = name, Corner = corner };
            if (chips != null && chips.Length > 0)
            {
                var strip = new TileEffectStrip();
                strip.Chips.AddRange(chips);
                model.Strips.Add(strip);
            }
            return new State { file = file, model = model };
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

            stage.cameraHolder = new GameObject("__TileCaptureCamera");
            stage.camera = stage.cameraHolder.AddComponent<Camera>();
            stage.camera.clearFlags = CameraClearFlags.SolidColor;
            // A mid green-grey stands in for the map, so the panel's translucency is visible.
            stage.camera.backgroundColor = new Color32(0x3E, 0x4A, 0x3C, 0xFF);
            stage.camera.orthographic = true;

            stage.target = new RenderTexture(Width, Height, 24, RenderTextureFormat.ARGB32);
            stage.camera.targetTexture = stage.target;

            var canvasHolder = new GameObject("__TileCaptureCanvas");
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

        static TileInfoView FindView(Scene scene)
        {
            foreach (GameObject root in scene.GetRootGameObjects())
            {
                var found = root.GetComponentInChildren<TileInfoView>(true);
                if (found != null) return found;
            }
            return null;
        }
    }
}
