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
using ProjectAstra.Core.UI;

namespace ProjectAstra.Core.Editor
{
    /// <summary>Editor utility that generates placeholder scenes, overlay prefabs, and build settings for all GameStates.</summary>
    public static class GenerateStateScenes
    {
        private static readonly Dictionary<GameState, Color> StateColors = new()
        {
            { GameState.TitleScreen,      new Color(0.10f, 0.12f, 0.30f) },
            { GameState.BattleMap,        new Color(0.10f, 0.25f, 0.12f) },
            { GameState.BattleMapPaused,  new Color(0.18f, 0.28f, 0.20f) },
            { GameState.CombatAnimation,  new Color(0.35f, 0.10f, 0.10f) },
            { GameState.Dialogue,         new Color(0.10f, 0.25f, 0.28f) },
            { GameState.SaveMenu,         new Color(0.20f, 0.22f, 0.28f) },
            { GameState.SettingsMenu,     new Color(0.22f, 0.22f, 0.22f) },
        };

        // Only TitleScreen and BattleMap use the generic generator. MainMenu, Cutscene,
        // PreBattlePrep, ChapterClear, and GameOver each have a dedicated scene builder
        // under Assets/Editor/*Builder.cs (shared layout via TitleMenuLayoutBuilder).
        private static readonly GameState[] SceneStates =
        {
            GameState.TitleScreen, GameState.BattleMap,
        };

        private static readonly GameState[] OverlayStates =
        {
            GameState.BattleMapPaused, GameState.CombatAnimation,
            GameState.Dialogue, GameState.SaveMenu, GameState.SettingsMenu
        };

        private struct ButtonDef
        {
            public string Label;
            public string FieldName;
        }

        private static readonly Dictionary<GameState, Type> ControllerTypes = new()
        {
            { GameState.TitleScreen,      typeof(TitleScreenUI) },
            { GameState.BattleMap,        typeof(BattleMapUI) },
            { GameState.BattleMapPaused,  typeof(BattleMapPausedOverlayUI) },
            { GameState.CombatAnimation,  typeof(CombatAnimationOverlayUI) },
            { GameState.Dialogue,         typeof(DialogueOverlayUI) },
            { GameState.SaveMenu,         typeof(SaveMenuOverlayUI) },
            { GameState.SettingsMenu,     typeof(SettingsMenuOverlayUI) },
        };

        private static readonly Dictionary<GameState, ButtonDef[]> ButtonConfigs = new()
        {
            { GameState.BattleMap, new[] {
                new ButtonDef { Label = "Cutscene",          FieldName = "_cutsceneButton" },
                new ButtonDef { Label = "Combat Animation",  FieldName = "_combatAnimationButton" },
                new ButtonDef { Label = "Dialogue",          FieldName = "_dialogueButton" },
                new ButtonDef { Label = "Chapter Clear",     FieldName = "_chapterClearButton" },
                new ButtonDef { Label = "Game Over",         FieldName = "_gameOverButton" },
            }},
            { GameState.BattleMapPaused, new[] {
                new ButtonDef { Label = "End Turn",      FieldName = "_endTurnButton" },
                new ButtonDef { Label = "Resume",        FieldName = "_resumeButton" },
                new ButtonDef { Label = "Save Menu",     FieldName = "_saveMenuButton" },
                new ButtonDef { Label = "Settings Menu", FieldName = "_settingsMenuButton" },
                new ButtonDef { Label = "Quit to Main Menu", FieldName = "_quitButton" },
            }},
            { GameState.CombatAnimation, new[] {
                new ButtonDef { Label = "Return to Battle", FieldName = "_returnButton" },
            }},
            { GameState.Dialogue, new[] {
                new ButtonDef { Label = "End Dialogue", FieldName = "_endDialogueButton" },
            }},
            { GameState.SaveMenu, new[] {
                new ButtonDef { Label = "Return", FieldName = "_returnButton" },
            }},
            { GameState.SettingsMenu, new[] {
                new ButtonDef { Label = "Return", FieldName = "_returnButton" },
            }},
        };

        [MenuItem("Project Astra/Generate State Scenes and Overlays")]
        public static void Generate()
        {
            if (!EditorUtility.DisplayDialog(
                "Generate State Scenes",
                "This will create 2 game scenes, 5 overlay prefabs, a navigation button prefab, and update build settings. Continue?",
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

            Debug.Log("[GenerateStateScenes] Done! 2 scenes + 5 overlays + button prefab created.");
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

            var controller = (Component)canvas.gameObject.AddComponent(ControllerTypes[state]);

            Button advancePhaseButton = null;
            if (state == GameState.BattleMap)
                advancePhaseButton = CreateBattleMapPhaseControls(buttonContainer.transform, controller);

            var buttons = CreateButtonsForState(state, buttonContainer.transform);
            if (advancePhaseButton != null)
                buttons.Insert(0, advancePhaseButton);
            WireController(controller, state, buttons);

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

            var dimGo = new GameObject("DimBackground", typeof(RectTransform));
            dimGo.transform.SetParent(rootGo.transform, false);
            dimGo.AddComponent<Image>().color = new Color(0f, 0f, 0f, 0.6f);
            StretchFull(dimGo.GetComponent<RectTransform>());

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

            var titleGo = new GameObject("Title", typeof(RectTransform));
            titleGo.transform.SetParent(panelGo.transform, false);
            var titleTmp = titleGo.AddComponent<TextMeshProUGUI>();
            titleTmp.text = FormatName(state.ToString());
            titleTmp.fontSize = 36;
            titleTmp.fontStyle = FontStyles.Bold;
            titleTmp.alignment = TextAlignmentOptions.Center;
            titleTmp.color = Color.white;
            titleGo.AddComponent<LayoutElement>().preferredHeight = 60;

            var navGo = new GameObject("NavigateLabel", typeof(RectTransform));
            navGo.transform.SetParent(panelGo.transform, false);
            var navTmp = navGo.AddComponent<TextMeshProUGUI>();
            navTmp.text = "Navigate to:";
            navTmp.fontSize = 18;
            navTmp.fontStyle = FontStyles.Italic;
            navTmp.alignment = TextAlignmentOptions.Center;
            navTmp.color = new Color(0.7f, 0.7f, 0.7f);
            navGo.AddComponent<LayoutElement>().preferredHeight = 30;

            var containerGo = new GameObject("ButtonContainer", typeof(RectTransform));
            containerGo.transform.SetParent(panelGo.transform, false);
            var containerLayout = containerGo.AddComponent<VerticalLayoutGroup>();
            containerLayout.spacing = 8;
            containerLayout.childControlWidth = true;
            containerLayout.childControlHeight = true;
            containerLayout.childForceExpandWidth = true;
            containerLayout.childForceExpandHeight = false;
            containerGo.AddComponent<LayoutElement>().flexibleHeight = 1;

            var controller = (Component)rootGo.AddComponent(ControllerTypes[state]);

            var buttons = CreateButtonsForState(state, containerGo.transform);
            WireController(controller, state, buttons);

            PrefabUtility.SaveAsPrefabAsset(rootGo, $"Assets/Resources/Overlays/{state}.prefab");
            UnityEngine.Object.DestroyImmediate(rootGo);
        }

        private static List<Button> CreateButtonsForState(GameState state, Transform container)
        {
            var buttons = new List<Button>();
            if (!ButtonConfigs.TryGetValue(state, out var defs)) return buttons;

            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Resources/UI/NavigationButton.prefab");

            foreach (var def in defs)
            {
                Button button;
                if (prefab != null)
                {
                    var instance = (GameObject)PrefabUtility.InstantiatePrefab(prefab);
                    instance.transform.SetParent(container, false);
                    instance.name = def.Label;
                    var tmp = instance.GetComponentInChildren<TextMeshProUGUI>();
                    if (tmp != null) tmp.text = def.Label;
                    button = instance.GetComponent<Button>();
                }
                else
                {
                    var go = CreateFallbackButton(container, def.Label);
                    button = go.GetComponent<Button>();
                }

                buttons.Add(button);
            }

            return buttons;
        }

        private static void WireController(Component controller, GameState state, List<Button> buttons)
        {
            if (!ButtonConfigs.TryGetValue(state, out var defs)) return;

            for (int i = 0; i < defs.Length && i < buttons.Count; i++)
            {
                if (defs[i].FieldName != null)
                    SetField(controller, defs[i].FieldName, buttons[i]);
            }
        }

        private static Button CreateBattleMapPhaseControls(Transform container, Component controller)
        {
            // Phase label
            var labelGo = new GameObject("PhaseLabel", typeof(RectTransform));
            labelGo.transform.SetParent(container, false);
            labelGo.GetComponent<RectTransform>().sizeDelta = new Vector2(0, 35);
            var phaseTmp = labelGo.AddComponent<TextMeshProUGUI>();
            phaseTmp.text = "Battle Phase:  Player Phase";
            phaseTmp.fontSize = 22;
            phaseTmp.alignment = TextAlignmentOptions.Center;
            phaseTmp.color = new Color(0.85f, 0.85f, 0.85f);
            phaseTmp.raycastTarget = false;
            labelGo.AddComponent<LayoutElement>().preferredHeight = 35;

            SetField(controller, "_phaseLabel", phaseTmp);

            // Advance Phase button
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Resources/UI/NavigationButton.prefab");
            Button advanceButton;
            if (prefab != null)
            {
                var instance = (GameObject)PrefabUtility.InstantiatePrefab(prefab);
                instance.transform.SetParent(container, false);
                instance.name = "Advance Phase";
                var tmp = instance.GetComponentInChildren<TextMeshProUGUI>();
                if (tmp != null) tmp.text = "Advance Phase";
                advanceButton = instance.GetComponent<Button>();
            }
            else
            {
                advanceButton = CreateFallbackButton(container, "Advance Phase").GetComponent<Button>();
            }

            SetField(controller, "_advancePhaseButton", advanceButton);

            // Has Allies toggle
            var toggleGo = CreateToggle(container, "Has Allies", true);
            var toggle = toggleGo.GetComponent<Toggle>();
            SetField(controller, "_hasAlliesToggle", toggle);

            // Spacer
            var spacer = new GameObject("Spacer", typeof(RectTransform));
            spacer.transform.SetParent(container, false);
            spacer.AddComponent<LayoutElement>().preferredHeight = 20;

            return advanceButton;
        }

        private static GameObject CreateToggle(Transform container, string label, bool initialValue)
        {
            var toggleGo = new GameObject(label, typeof(RectTransform));
            toggleGo.transform.SetParent(container, false);
            toggleGo.GetComponent<RectTransform>().sizeDelta = new Vector2(0, 40);
            toggleGo.AddComponent<LayoutElement>().preferredHeight = 40;

            var toggle = toggleGo.AddComponent<Toggle>();
            toggle.isOn = initialValue;

            var bgGo = new GameObject("Background", typeof(RectTransform));
            bgGo.transform.SetParent(toggleGo.transform, false);
            var bgImage = bgGo.AddComponent<Image>();
            bgImage.color = new Color(0.3f, 0.3f, 0.3f);
            var bgRect = bgGo.GetComponent<RectTransform>();
            bgRect.anchorMin = new Vector2(0f, 0.5f);
            bgRect.anchorMax = new Vector2(0f, 0.5f);
            bgRect.pivot = new Vector2(0f, 0.5f);
            bgRect.sizeDelta = new Vector2(30, 30);
            bgRect.anchoredPosition = new Vector2(10, 0);

            var checkGo = new GameObject("Checkmark", typeof(RectTransform));
            checkGo.transform.SetParent(bgGo.transform, false);
            var checkImage = checkGo.AddComponent<Image>();
            checkImage.color = Color.white;
            var checkRect = checkGo.GetComponent<RectTransform>();
            checkRect.anchorMin = new Vector2(0.15f, 0.15f);
            checkRect.anchorMax = new Vector2(0.85f, 0.85f);
            checkRect.offsetMin = Vector2.zero;
            checkRect.offsetMax = Vector2.zero;

            toggle.targetGraphic = bgImage;
            toggle.graphic = checkImage;

            var textGo = new GameObject("Label", typeof(RectTransform));
            textGo.transform.SetParent(toggleGo.transform, false);
            var tmp = textGo.AddComponent<TextMeshProUGUI>();
            tmp.text = label;
            tmp.fontSize = 20;
            tmp.alignment = TextAlignmentOptions.MidlineLeft;
            tmp.color = new Color(0.85f, 0.85f, 0.85f);
            var textRect = textGo.GetComponent<RectTransform>();
            textRect.anchorMin = Vector2.zero;
            textRect.anchorMax = Vector2.one;
            textRect.offsetMin = new Vector2(50, 0);
            textRect.offsetMax = Vector2.zero;

            toggle.onValueChanged.AddListener(val => { });

            return toggleGo;
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

            // Remove legacy StateUIController if present
            var legacyGo = GameObject.Find("StateUIController");
            if (legacyGo != null)
                UnityEngine.Object.DestroyImmediate(legacyGo);

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

        private static GameObject CreateFallbackButton(Transform container, string label)
        {
            var buttonGo = new GameObject(label, typeof(RectTransform));
            buttonGo.transform.SetParent(container, false);
            buttonGo.GetComponent<RectTransform>().sizeDelta = new Vector2(0, 50);
            var img = buttonGo.AddComponent<Image>();
            img.color = new Color(0.2f, 0.2f, 0.2f, 0.9f);
            buttonGo.AddComponent<Button>();
            buttonGo.AddComponent<LayoutElement>().preferredHeight = 50;

            var textGo = new GameObject("Label", typeof(RectTransform));
            textGo.transform.SetParent(buttonGo.transform, false);
            var text = textGo.AddComponent<TextMeshProUGUI>();
            text.text = label;
            text.fontSize = 22;
            text.alignment = TextAlignmentOptions.Center;
            text.color = Color.white;
            StretchFull(textGo.GetComponent<RectTransform>());

            return buttonGo;
        }

        private static void StretchFull(RectTransform rect)
        {
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;
        }

        private static void SetField(object target, string fieldName, object value)
        {
            var type = target.GetType();
            while (type != null)
            {
                var field = type.GetField(fieldName,
                    System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                if (field != null)
                {
                    field.SetValue(target, value);
                    if (target is UnityEngine.Object uObj)
                        EditorUtility.SetDirty(uObj);
                    return;
                }
                type = type.BaseType;
            }
            Debug.LogWarning($"Could not find field '{fieldName}' on {target.GetType().Name} or its parents");
        }

        private static string FormatName(string pascalCase)
        {
            return Regex.Replace(pascalCase, @"(?<!^)([A-Z])", " $1");
        }
    }
}
