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
                "This will create 7 game scenes, 5 overlay prefabs, a navigation button prefab, and update build settings. Continue?",
                "Generate", "Cancel"))
                return;

            EnsureDirectories();
            CreateNavigationButtonPrefab();

            foreach (var state in SceneStates)
                CreateSceneForState(state);

            foreach (var state in OverlayStates)
                CreateOverlayPrefab(state);

            SetupBootScene();
            UpdateBuildSettings();

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            Debug.Log("[GenerateStateScenes] Done! 7 scenes + 5 overlays + button prefab created.");
        }

        private static void EnsureDirectories()
        {
            EnsureFolder("Assets/Scenes");
            EnsureFolder("Assets/Resources");
            EnsureFolder("Assets/Resources/UI");
            EnsureFolder("Assets/Resources/Overlays");
        }

        private static void EnsureFolder(string path)
        {
            if (AssetDatabase.IsValidFolder(path)) return;
            var parent = Path.GetDirectoryName(path).Replace('\\', '/');
            var folder = Path.GetFileName(path);
            AssetDatabase.CreateFolder(parent, folder);
        }

        private static void CreateNavigationButtonPrefab()
        {
            var buttonGo = new GameObject("NavigationButton", typeof(RectTransform));

            var image = buttonGo.AddComponent<Image>();
            image.color = new Color(0.2f, 0.2f, 0.2f, 0.9f);

            var button = buttonGo.AddComponent<Button>();
            var colors = button.colors;
            colors.highlightedColor = new Color(0.35f, 0.35f, 0.35f);
            colors.pressedColor = new Color(0.15f, 0.15f, 0.15f);
            button.colors = colors;

            var layout = buttonGo.AddComponent<LayoutElement>();
            layout.preferredHeight = 50;

            var textGo = new GameObject("Label", typeof(RectTransform));
            textGo.transform.SetParent(buttonGo.transform, false);
            var tmp = textGo.AddComponent<TextMeshProUGUI>();
            tmp.text = "Button";
            tmp.fontSize = 22;
            tmp.alignment = TextAlignmentOptions.Center;
            tmp.color = Color.white;
            StretchFull(textGo.GetComponent<RectTransform>());

            PrefabUtility.SaveAsPrefabAsset(buttonGo, "Assets/Resources/UI/NavigationButton.prefab");
            UnityEngine.Object.DestroyImmediate(buttonGo);
        }

        private static void CreateSceneForState(GameState state)
        {
            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

            CreateSceneCamera();
            var canvas = CreateCanvas(0);
            CreateBackground(canvas.transform, StateColors[state]);
            CreateTitle(canvas.transform, FormatName(state.ToString()));
            CreateNavigationLabel(canvas.transform);
            var buttonContainer = CreateButtonContainer(canvas.transform);

            var uiRoot = canvas.gameObject.AddComponent<SceneUIRoot>();
            SetField(uiRoot, "_buttonContainer", buttonContainer.transform);

            EditorSceneManager.SaveScene(scene, $"Assets/Scenes/{state}.unity");
        }

        private static void CreateOverlayPrefab(GameState state)
        {
            var rootGo = new GameObject(state.ToString());

            var canvas = rootGo.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 10;

            var scaler = rootGo.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920, 1080);
            scaler.matchWidthOrHeight = 0.5f;

            rootGo.AddComponent<GraphicRaycaster>();

            // Dim background
            var dimGo = new GameObject("DimBackground", typeof(RectTransform));
            dimGo.transform.SetParent(rootGo.transform, false);
            dimGo.AddComponent<Image>().color = new Color(0f, 0f, 0f, 0.6f);
            StretchFull(dimGo.GetComponent<RectTransform>());

            // Panel
            var panelGo = new GameObject("Panel", typeof(RectTransform));
            panelGo.transform.SetParent(rootGo.transform, false);
            panelGo.AddComponent<Image>().color = StateColors[state];
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
            panelLayout.childControlHeight = true;
            panelLayout.childForceExpandWidth = true;
            panelLayout.childForceExpandHeight = false;

            // Title
            var titleGo = new GameObject("Title", typeof(RectTransform));
            titleGo.transform.SetParent(panelGo.transform, false);
            var titleTmp = titleGo.AddComponent<TextMeshProUGUI>();
            titleTmp.text = FormatName(state.ToString());
            titleTmp.fontSize = 36;
            titleTmp.fontStyle = FontStyles.Bold;
            titleTmp.alignment = TextAlignmentOptions.Center;
            titleTmp.color = Color.white;
            titleGo.AddComponent<LayoutElement>().preferredHeight = 60;

            // Navigate label
            var navGo = new GameObject("NavigateLabel", typeof(RectTransform));
            navGo.transform.SetParent(panelGo.transform, false);
            var navTmp = navGo.AddComponent<TextMeshProUGUI>();
            navTmp.text = "Navigate to:";
            navTmp.fontSize = 18;
            navTmp.fontStyle = FontStyles.Italic;
            navTmp.alignment = TextAlignmentOptions.Center;
            navTmp.color = new Color(0.7f, 0.7f, 0.7f);
            navGo.AddComponent<LayoutElement>().preferredHeight = 30;

            // Button container
            var containerGo = new GameObject("ButtonContainer", typeof(RectTransform));
            containerGo.transform.SetParent(panelGo.transform, false);
            var containerLayout = containerGo.AddComponent<VerticalLayoutGroup>();
            containerLayout.spacing = 8;
            containerLayout.childControlWidth = true;
            containerLayout.childControlHeight = true;
            containerLayout.childForceExpandWidth = true;
            containerLayout.childForceExpandHeight = false;
            containerGo.AddComponent<LayoutElement>().flexibleHeight = 1;

            // SceneUIRoot marker
            var uiRoot = rootGo.AddComponent<SceneUIRoot>();
            SetField(uiRoot, "_buttonContainer", containerGo.transform);

            PrefabUtility.SaveAsPrefabAsset(rootGo, $"Assets/Resources/Overlays/{state}.prefab");
            UnityEngine.Object.DestroyImmediate(rootGo);
        }

        private static void SetupBootScene()
        {
            string bootPath = "Assets/Scenes/BootScene.unity";
            if (!File.Exists(bootPath))
            {
                var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
                EditorSceneManager.SaveScene(scene, bootPath);
            }
            else
            {
                EditorSceneManager.OpenScene(bootPath);
            }

            var eventChannel = LoadAsset<GameStateEventChannel>("t:GameStateEventChannel", "Assets/ScriptableObjects");
            var transitionTable = LoadAsset<GameStateTransitionTable>("t:GameStateTransitionTable", "Assets/ScriptableObjects");

            // GameStateManager
            var gsm = UnityEngine.Object.FindFirstObjectByType<GameStateManager>();
            if (gsm == null)
            {
                var go = new GameObject("GameStateManager");
                gsm = go.AddComponent<GameStateManager>();
            }

            // EventSystem
            if (UnityEngine.Object.FindFirstObjectByType<EventSystem>() == null)
            {
                var go = new GameObject("EventSystem");
                go.AddComponent<EventSystem>();
                go.AddComponent<InputSystemUIInputModule>();
            }

            // SceneLoader
            var loader = UnityEngine.Object.FindFirstObjectByType<SceneLoader>();
            if (loader == null)
            {
                var go = new GameObject("SceneLoader");
                loader = go.AddComponent<SceneLoader>();
            }
            SetField(loader, "_stateChangedChannel", eventChannel);

            // StateUIController (single global instance)
            var uiController = UnityEngine.Object.FindFirstObjectByType<StateUIController>();
            if (uiController == null)
            {
                var go = new GameObject("StateUIController");
                uiController = go.AddComponent<StateUIController>();
            }
            SetField(uiController, "_stateChangedChannel", eventChannel);
            SetField(uiController, "_transitionTable", transitionTable);

            EditorSceneManager.SaveScene(SceneManager.GetActiveScene(), bootPath);
        }

        private static void UpdateBuildSettings()
        {
            var scenes = new List<EditorBuildSettingsScene>
            {
                new("Assets/Scenes/BootScene.unity", true)
            };

            foreach (var state in SceneStates)
                scenes.Add(new EditorBuildSettingsScene($"Assets/Scenes/{state}.unity", true));

            EditorBuildSettings.scenes = scenes.ToArray();
        }

        // --- Helpers ---

        private static T LoadAsset<T>(string filter, string folder) where T : UnityEngine.Object
        {
            var guids = AssetDatabase.FindAssets(filter, new[] { folder });
            return guids.Length > 0
                ? AssetDatabase.LoadAssetAtPath<T>(AssetDatabase.GUIDToAssetPath(guids[0]))
                : null;
        }

        private static void CreateSceneCamera()
        {
            var go = new GameObject("Camera");
            var cam = go.AddComponent<Camera>();
            cam.orthographic = true;
            cam.orthographicSize = 5;
            cam.clearFlags = CameraClearFlags.SolidColor;
            cam.backgroundColor = Color.black;
            go.transform.position = new Vector3(0, 0, -10);
        }

        private static Canvas CreateCanvas(int sortOrder)
        {
            var go = new GameObject("Canvas");
            var canvas = go.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = sortOrder;
            var scaler = go.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920, 1080);
            scaler.matchWidthOrHeight = 0.5f;
            go.AddComponent<GraphicRaycaster>();
            return canvas;
        }

        private static void CreateBackground(Transform parent, Color color)
        {
            var go = new GameObject("Background", typeof(RectTransform));
            go.transform.SetParent(parent, false);
            go.AddComponent<Image>().color = color;
            StretchFull(go.GetComponent<RectTransform>());
        }

        private static void CreateTitle(Transform parent, string text)
        {
            var go = new GameObject("Title", typeof(RectTransform));
            go.transform.SetParent(parent, false);
            var tmp = go.AddComponent<TextMeshProUGUI>();
            tmp.text = text;
            tmp.fontSize = 48;
            tmp.fontStyle = FontStyles.Bold;
            tmp.alignment = TextAlignmentOptions.Center;
            tmp.color = Color.white;
            var rect = go.GetComponent<RectTransform>();
            rect.anchorMin = new Vector2(0, 0.75f);
            rect.anchorMax = new Vector2(1, 0.95f);
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;
        }

        private static void CreateNavigationLabel(Transform parent)
        {
            var go = new GameObject("NavigateLabel", typeof(RectTransform));
            go.transform.SetParent(parent, false);
            var tmp = go.AddComponent<TextMeshProUGUI>();
            tmp.text = "Navigate to:";
            tmp.fontSize = 22;
            tmp.fontStyle = FontStyles.Italic;
            tmp.alignment = TextAlignmentOptions.Center;
            tmp.color = new Color(0.7f, 0.7f, 0.7f);
            var rect = go.GetComponent<RectTransform>();
            rect.anchorMin = new Vector2(0.2f, 0.68f);
            rect.anchorMax = new Vector2(0.8f, 0.74f);
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;
        }

        private static GameObject CreateButtonContainer(Transform parent)
        {
            var go = new GameObject("ButtonContainer", typeof(RectTransform));
            go.transform.SetParent(parent, false);
            var layout = go.AddComponent<VerticalLayoutGroup>();
            layout.spacing = 10;
            layout.childAlignment = TextAnchor.UpperCenter;
            layout.childControlWidth = true;
            layout.childControlHeight = true;
            layout.childForceExpandWidth = true;
            layout.childForceExpandHeight = false;
            layout.padding = new RectOffset(20, 20, 0, 0);
            var rect = go.GetComponent<RectTransform>();
            rect.anchorMin = new Vector2(0.25f, 0.05f);
            rect.anchorMax = new Vector2(0.75f, 0.67f);
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;
            return go;
        }

        private static void StretchFull(RectTransform rect)
        {
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;
        }

        private static void SetField(UnityEngine.Object target, string fieldName, object value)
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
