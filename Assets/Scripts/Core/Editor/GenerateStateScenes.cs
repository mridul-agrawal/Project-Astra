using System;
using System.Collections.Generic;
using System.IO;
using System.Text.RegularExpressions;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using TMPro;
using ProjectAstra.Core;

namespace ProjectAstra.Core.Editor
{
    public static class GenerateStateScenes
    {
        private static readonly Dictionary<GameState, Color> StateColors = new()
        {
            { GameState.TitleScreen,      new Color(0.10f, 0.12f, 0.30f) },
            { GameState.MainMenu,         new Color(0.25f, 0.27f, 0.30f) },
            { GameState.Cutscene,         new Color(0.22f, 0.10f, 0.30f) },
            { GameState.PreBattlePrep,    new Color(0.28f, 0.28f, 0.15f) },
            { GameState.BattleMap,        new Color(0.10f, 0.25f, 0.12f) },
            { GameState.BattleMapPaused,  new Color(0.18f, 0.28f, 0.20f) },
            { GameState.CombatAnimation,  new Color(0.35f, 0.10f, 0.10f) },
            { GameState.Dialogue,         new Color(0.10f, 0.25f, 0.28f) },
            { GameState.ChapterClear,     new Color(0.35f, 0.30f, 0.08f) },
            { GameState.GameOver,         new Color(0.30f, 0.08f, 0.08f) },
            { GameState.SaveMenu,         new Color(0.20f, 0.22f, 0.28f) },
            { GameState.SettingsMenu,     new Color(0.22f, 0.22f, 0.22f) },
        };

        private static readonly GameState[] SceneStates =
        {
            GameState.TitleScreen, GameState.MainMenu, GameState.Cutscene,
            GameState.PreBattlePrep, GameState.BattleMap,
            GameState.ChapterClear, GameState.GameOver
        };

        private static readonly GameState[] OverlayStates =
        {
            GameState.BattleMapPaused, GameState.CombatAnimation,
            GameState.Dialogue, GameState.SaveMenu, GameState.SettingsMenu
        };

        [MenuItem("Project Astra/Generate State Scenes and Overlays")]
        public static void Generate()
        {
            if (!EditorUtility.DisplayDialog(
                "Generate State Scenes",
                "This will create 7 game scenes, 5 overlay prefabs, and update build settings. Continue?",
                "Generate", "Cancel"))
                return;

            var transitionTable = LoadTransitionTable();
            if (transitionTable == null)
            {
                Debug.LogError("Could not find TransitionTable asset in Assets/ScriptableObjects/Core/");
                return;
            }

            EnsureDirectories();

            foreach (var state in SceneStates)
                CreateSceneForState(state, transitionTable);

            foreach (var state in OverlayStates)
                CreateOverlayPrefab(state, transitionTable);

            SetupBootScene(transitionTable);
            UpdateBuildSettings();

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            Debug.Log("[GenerateStateScenes] Done! 7 scenes + 5 overlays created. Build settings updated.");
        }

        private static void EnsureDirectories()
        {
            if (!AssetDatabase.IsValidFolder("Assets/Scenes"))
                AssetDatabase.CreateFolder("Assets", "Scenes");
            if (!AssetDatabase.IsValidFolder("Assets/Prefabs"))
                AssetDatabase.CreateFolder("Assets", "Prefabs");
            if (!AssetDatabase.IsValidFolder("Assets/Prefabs/Core"))
                AssetDatabase.CreateFolder("Assets/Prefabs", "Core");
            if (!AssetDatabase.IsValidFolder("Assets/Prefabs/Core/Overlays"))
                AssetDatabase.CreateFolder("Assets/Prefabs/Core", "Overlays");
        }

        private static GameStateTransitionTable LoadTransitionTable()
        {
            var guids = AssetDatabase.FindAssets("t:GameStateTransitionTable", new[] { "Assets/ScriptableObjects" });
            if (guids.Length == 0) return null;
            return AssetDatabase.LoadAssetAtPath<GameStateTransitionTable>(AssetDatabase.GUIDToAssetPath(guids[0]));
        }

        private static GameStateEventChannel LoadEventChannel()
        {
            var guids = AssetDatabase.FindAssets("t:GameStateEventChannel", new[] { "Assets/ScriptableObjects" });
            if (guids.Length == 0) return null;
            return AssetDatabase.LoadAssetAtPath<GameStateEventChannel>(AssetDatabase.GUIDToAssetPath(guids[0]));
        }

        private static void CreateSceneForState(GameState state, GameStateTransitionTable transitionTable)
        {
            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            string sceneName = state.ToString();

            CreateSceneCamera();
            var canvas = CreateCanvas(0);
            CreateBackground(canvas.transform, StateColors[state]);
            CreateTitle(canvas.transform, FormatName(sceneName));
            CreateNavigationLabel(canvas.transform);
            var buttonContainer = CreateButtonContainer(canvas.transform);
            CreateStateController(state, transitionTable, buttonContainer.transform);

            string path = $"Assets/Scenes/{sceneName}.unity";
            EditorSceneManager.SaveScene(scene, path);
        }

        private static void CreateOverlayPrefab(GameState state, GameStateTransitionTable transitionTable)
        {
            var rootGo = new GameObject($"{state}Overlay");

            var canvas = rootGo.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 10;

            var scaler = rootGo.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920, 1080);
            scaler.matchWidthOrHeight = 0.5f;

            rootGo.AddComponent<GraphicRaycaster>();

            // Semi-transparent dark background
            var dimGo = new GameObject("DimBackground", typeof(RectTransform));
            dimGo.transform.SetParent(rootGo.transform, false);
            var dimImage = dimGo.AddComponent<Image>();
            dimImage.color = new Color(0f, 0f, 0f, 0.6f);
            StretchFull(dimGo.GetComponent<RectTransform>());

            // Centered panel
            var panelGo = new GameObject("Panel", typeof(RectTransform));
            panelGo.transform.SetParent(rootGo.transform, false);
            var panelImage = panelGo.AddComponent<Image>();
            panelImage.color = StateColors[state];
            var panelRect = panelGo.GetComponent<RectTransform>();
            panelRect.anchorMin = new Vector2(0.25f, 0.15f);
            panelRect.anchorMax = new Vector2(0.75f, 0.85f);
            panelRect.offsetMin = Vector2.zero;
            panelRect.offsetMax = Vector2.zero;

            var panelLayout = panelGo.AddComponent<VerticalLayoutGroup>();
            panelLayout.padding = new RectOffset(40, 40, 30, 30);
            panelLayout.spacing = 8;
            panelLayout.childAlignment = TextAnchor.UpperCenter;
            panelLayout.childControlWidth = true;
            panelLayout.childControlHeight = false;
            panelLayout.childForceExpandWidth = true;
            panelLayout.childForceExpandHeight = false;

            // Title inside panel
            var titleGo = new GameObject("Title", typeof(RectTransform));
            titleGo.transform.SetParent(panelGo.transform, false);
            var titleTmp = titleGo.AddComponent<TextMeshProUGUI>();
            titleTmp.text = FormatName(state.ToString());
            titleTmp.fontSize = 36;
            titleTmp.fontStyle = FontStyles.Bold;
            titleTmp.alignment = TextAlignmentOptions.Center;
            titleTmp.color = Color.white;
            var titleLayout = titleGo.AddComponent<LayoutElement>();
            titleLayout.preferredHeight = 60;

            // Navigation label
            var navLabelGo = new GameObject("NavigateLabel", typeof(RectTransform));
            navLabelGo.transform.SetParent(panelGo.transform, false);
            var navTmp = navLabelGo.AddComponent<TextMeshProUGUI>();
            navTmp.text = "Navigate to:";
            navTmp.fontSize = 18;
            navTmp.fontStyle = FontStyles.Italic;
            navTmp.alignment = TextAlignmentOptions.Center;
            navTmp.color = new Color(0.7f, 0.7f, 0.7f);
            var navLayout = navLabelGo.AddComponent<LayoutElement>();
            navLayout.preferredHeight = 30;

            // Button container inside panel
            var containerGo = new GameObject("ButtonContainer", typeof(RectTransform));
            containerGo.transform.SetParent(panelGo.transform, false);
            var containerLayout = containerGo.AddComponent<VerticalLayoutGroup>();
            containerLayout.spacing = 8;
            containerLayout.childControlWidth = true;
            containerLayout.childControlHeight = false;
            containerLayout.childForceExpandWidth = true;
            containerLayout.childForceExpandHeight = false;
            var containerElement = containerGo.AddComponent<LayoutElement>();
            containerElement.flexibleHeight = 1;

            // StateUIController
            var controller = rootGo.AddComponent<StateUIController>();
            SetSerializedField(controller, "_state", state);
            SetSerializedField(controller, "_transitionTable", transitionTable);
            SetSerializedField(controller, "_buttonContainer", containerGo.transform);

            string path = $"Assets/Prefabs/Core/Overlays/{state}Overlay.prefab";
            PrefabUtility.SaveAsPrefabAsset(rootGo, path);
            UnityEngine.Object.DestroyImmediate(rootGo);
        }

        private static void SetupBootScene(GameStateTransitionTable transitionTable)
        {
            string bootPath = "Assets/Scenes/SampleScene.unity";
            if (!File.Exists(bootPath))
            {
                var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
                EditorSceneManager.SaveScene(scene, bootPath);
            }
            else
            {
                EditorSceneManager.OpenScene(bootPath);
            }

            // Find or create GameStateManager
            var gsm = UnityEngine.Object.FindFirstObjectByType<GameStateManager>();
            if (gsm == null)
            {
                var gsmGo = new GameObject("GameStateManager");
                gsm = gsmGo.AddComponent<GameStateManager>();
            }

            // Remove DebugNavigator if present
            var debugNav = gsm.GetComponent<GameStateDebugNavigator>();
            if (debugNav != null)
                UnityEngine.Object.DestroyImmediate(debugNav);

            // Find or create EventSystem
            var eventSystem = UnityEngine.Object.FindFirstObjectByType<EventSystem>();
            if (eventSystem == null)
            {
                var esGo = new GameObject("EventSystem");
                esGo.AddComponent<EventSystem>();
                esGo.AddComponent<InputSystemUIInputModule>();
            }

            // Find or create SceneLoader
            var sceneLoader = UnityEngine.Object.FindFirstObjectByType<SceneLoader>();
            if (sceneLoader == null)
            {
                var loaderGo = new GameObject("SceneLoader");
                sceneLoader = loaderGo.AddComponent<SceneLoader>();
            }

            // Wire up SceneLoader overlay prefab references
            var eventChannel = LoadEventChannel();
            SetSerializedField(sceneLoader, "_stateChangedChannel", eventChannel);

            foreach (var state in OverlayStates)
            {
                string prefabPath = $"Assets/Prefabs/Core/Overlays/{state}Overlay.prefab";
                var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath);
                string fieldName = state switch
                {
                    GameState.BattleMapPaused  => "_battleMapPausedOverlay",
                    GameState.CombatAnimation  => "_combatAnimationOverlay",
                    GameState.Dialogue         => "_dialogueOverlay",
                    GameState.SaveMenu         => "_saveMenuOverlay",
                    GameState.SettingsMenu     => "_settingsMenuOverlay",
                    _ => null
                };
                if (fieldName != null && prefab != null)
                    SetSerializedField(sceneLoader, fieldName, prefab);
            }

            EditorSceneManager.SaveScene(SceneManager.GetActiveScene(), bootPath);
        }

        private static void UpdateBuildSettings()
        {
            var scenes = new List<EditorBuildSettingsScene>
            {
                new("Assets/Scenes/SampleScene.unity", true)
            };

            foreach (var state in SceneStates)
                scenes.Add(new EditorBuildSettingsScene($"Assets/Scenes/{state}.unity", true));

            EditorBuildSettings.scenes = scenes.ToArray();
        }

        // --- UI Creation Helpers ---

        private static void CreateSceneCamera()
        {
            var camGo = new GameObject("Camera");
            var cam = camGo.AddComponent<Camera>();
            cam.orthographic = true;
            cam.orthographicSize = 5;
            // Clear to solid color so the background isn't rendering garbage
            cam.clearFlags = CameraClearFlags.SolidColor;
            cam.backgroundColor = Color.black;
            camGo.transform.position = new Vector3(0, 0, -10);
        }

        private static Canvas CreateCanvas(int sortOrder)
        {
            var canvasGo = new GameObject("Canvas");
            var canvas = canvasGo.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = sortOrder;

            var scaler = canvasGo.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920, 1080);
            scaler.matchWidthOrHeight = 0.5f;

            canvasGo.AddComponent<GraphicRaycaster>();
            return canvas;
        }

        private static void CreateBackground(Transform parent, Color color)
        {
            var bgGo = new GameObject("Background", typeof(RectTransform));
            bgGo.transform.SetParent(parent, false);
            var image = bgGo.AddComponent<Image>();
            image.color = color;
            StretchFull(bgGo.GetComponent<RectTransform>());
        }

        private static void CreateTitle(Transform parent, string text)
        {
            var titleGo = new GameObject("Title", typeof(RectTransform));
            titleGo.transform.SetParent(parent, false);
            var tmp = titleGo.AddComponent<TextMeshProUGUI>();
            tmp.text = text;
            tmp.fontSize = 48;
            tmp.fontStyle = FontStyles.Bold;
            tmp.alignment = TextAlignmentOptions.Center;
            tmp.color = Color.white;

            var rect = titleGo.GetComponent<RectTransform>();
            rect.anchorMin = new Vector2(0, 0.75f);
            rect.anchorMax = new Vector2(1, 0.95f);
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;
        }

        private static void CreateNavigationLabel(Transform parent)
        {
            var labelGo = new GameObject("NavigateLabel", typeof(RectTransform));
            labelGo.transform.SetParent(parent, false);
            var tmp = labelGo.AddComponent<TextMeshProUGUI>();
            tmp.text = "Navigate to:";
            tmp.fontSize = 22;
            tmp.fontStyle = FontStyles.Italic;
            tmp.alignment = TextAlignmentOptions.Center;
            tmp.color = new Color(0.7f, 0.7f, 0.7f);

            var rect = labelGo.GetComponent<RectTransform>();
            rect.anchorMin = new Vector2(0.2f, 0.68f);
            rect.anchorMax = new Vector2(0.8f, 0.74f);
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;
        }

        private static GameObject CreateButtonContainer(Transform parent)
        {
            var containerGo = new GameObject("ButtonContainer", typeof(RectTransform));
            containerGo.transform.SetParent(parent, false);

            var layout = containerGo.AddComponent<VerticalLayoutGroup>();
            layout.spacing = 10;
            layout.childAlignment = TextAnchor.UpperCenter;
            layout.childControlWidth = true;
            layout.childControlHeight = true;
            layout.childForceExpandWidth = true;
            layout.childForceExpandHeight = false;
            layout.padding = new RectOffset(20, 20, 0, 0);

            var rect = containerGo.GetComponent<RectTransform>();
            rect.anchorMin = new Vector2(0.25f, 0.05f);
            rect.anchorMax = new Vector2(0.75f, 0.67f);
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;

            return containerGo;
        }

        private static void CreateStateController(
            GameState state,
            GameStateTransitionTable transitionTable,
            Transform buttonContainer)
        {
            var controllerGo = new GameObject("StateController");
            var controller = controllerGo.AddComponent<StateUIController>();
            SetSerializedField(controller, "_state", state);
            SetSerializedField(controller, "_transitionTable", transitionTable);
            SetSerializedField(controller, "_buttonContainer", buttonContainer);
        }

        private static void StretchFull(RectTransform rect)
        {
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;
        }

        private static void SetSerializedField(UnityEngine.Object target, string fieldName, object value)
        {
            var field = target.GetType().GetField(fieldName,
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            if (field == null)
            {
                Debug.LogWarning($"Could not find field '{fieldName}' on {target.GetType().Name}");
                return;
            }

            field.SetValue(target, value);
            EditorUtility.SetDirty(target);
        }

        private static string FormatName(string pascalCase)
        {
            return Regex.Replace(pascalCase, @"(?<!^)([A-Z])", " $1");
        }
    }
}
