using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;
using ProjectAstra.Core.Hub;
using ProjectAstra.Core.UI.Hub;
using ProjectAstra.Core.UI.Dialogue.Choice;
using ProjectAstra.Core.UI.Hub.Marker;
using ProjectAstra.Core.UI.Hub.Objective;
using ProjectAstra.Core.UI.Hub.Prompt;

namespace ProjectAstra.Core.Editor
{
    // Builds the hub HUD as plain boxes and text, for the designer to style by hand afterwards.
    // Rebuilding regenerates all of it, so hand-styling belongs on the saved scene, never here.
    public static class HubHudBuilder
    {
        private const float Scale = 4f;
        private const float CanvasWidth = 1920f;
        private const float CanvasHeight = 1080f;

        private const string GlyphDataPath = "Assets/Gurukul/Data/InputGlyphData.asset";

        private static float Sc(float logical) => logical * Scale;

        public static HubHUDController Build(out ChoiceMenuView choiceMenu, out EdgeIndicatorView edgeIndicators)
        {
            GameObject canvasGo = CreateCanvas();
            InteractionPromptView prompt = CreateInteractionPrompt(canvasGo.transform);
            HubObjectiveView objective = CreateObjectivePanel(canvasGo.transform);
            choiceMenu = CreateChoiceMenu(canvasGo.transform);
            edgeIndicators = CreateEdgeIndicators(canvasGo.transform);

            var controller = canvasGo.AddComponent<HubHUDController>();
            var serialized = new SerializedObject(controller);
            serialized.FindProperty("promptView").objectReferenceValue = prompt;
            serialized.FindProperty("objectiveView").objectReferenceValue = objective;
            serialized.FindProperty("glyphData").objectReferenceValue = LoadOrCreateGlyphData();
            serialized.ApplyModifiedProperties();

            return controller;
        }

        // Top-left, where it stays out of the way of both her and the dialogue box.
        private static HubObjectiveView CreateObjectivePanel(Transform parent)
        {
            var root = new GameObject("ObjectivePanel", typeof(RectTransform));
            root.transform.SetParent(parent, false);

            var content = new GameObject("Content", typeof(RectTransform), typeof(Image));
            content.transform.SetParent(root.transform, false);

            var rect = content.GetComponent<RectTransform>();
            rect.anchorMin = new Vector2(0f, 1f);
            rect.anchorMax = new Vector2(0f, 1f);
            rect.pivot = new Vector2(0f, 1f);
            rect.anchoredPosition = new Vector2(Sc(8f), -Sc(8f));
            rect.sizeDelta = new Vector2(Sc(150f), Sc(16f));

            var background = content.GetComponent<Image>();
            background.color = new Color(0.06f, 0.07f, 0.10f, 0.8f);
            background.raycastTarget = false;

            TextMeshProUGUI objective = AddLabel(content.transform, "Objective", "Objective",
                new Vector2(0f, 0.5f), new Vector2(1f, 0.5f), new Vector2(Sc(5f), 0f),
                new Vector2(-Sc(38f), Sc(12f)), TextAlignmentOptions.Left);

            TextMeshProUGUI counter = AddLabel(content.transform, "Counter", "0/0",
                new Vector2(1f, 0.5f), new Vector2(1f, 0.5f), new Vector2(-Sc(32f), 0f),
                new Vector2(Sc(28f), Sc(12f)), TextAlignmentOptions.Right);

            var view = root.AddComponent<HubObjectiveView>();
            view.content = content;
            view.objectiveLabel = objective;
            view.counterLabel = counter;
            AddCompletionCue(root.transform, view);
            content.SetActive(false);
            return view;
        }

        // Its own box under the objective line, so the cue and the objective that replaced it can be
        // read at the same time.
        private static void AddCompletionCue(Transform parent, HubObjectiveView view)
        {
            var cue = new GameObject("Cue", typeof(RectTransform), typeof(Image));
            cue.transform.SetParent(parent, false);

            var rect = cue.GetComponent<RectTransform>();
            rect.anchorMin = new Vector2(0f, 1f);
            rect.anchorMax = new Vector2(0f, 1f);
            rect.pivot = new Vector2(0f, 1f);
            rect.anchoredPosition = new Vector2(Sc(8f), -Sc(26f));
            rect.sizeDelta = new Vector2(Sc(150f), Sc(14f));

            var background = cue.GetComponent<Image>();
            background.color = new Color(0.18f, 0.30f, 0.18f, 0.85f);
            background.raycastTarget = false;

            view.cueContent = cue;
            view.cueLabel = AddLabel(cue.transform, "CueLabel", "Done",
                new Vector2(0f, 0.5f), new Vector2(1f, 0.5f), new Vector2(Sc(5f), 0f),
                new Vector2(-Sc(10f), Sc(11f)), TextAlignmentOptions.Left);

            cue.SetActive(false);
        }

        // Anchored to the canvas corner with a zero pivot, so the solver's canvas coordinates can be
        // used as anchored positions directly.
        private static EdgeIndicatorView CreateEdgeIndicators(Transform parent)
        {
            var root = new GameObject("EdgeIndicators", typeof(RectTransform));
            root.transform.SetParent(parent, false);

            var content = new GameObject("Content", typeof(RectTransform));
            content.transform.SetParent(root.transform, false);

            var rect = content.GetComponent<RectTransform>();
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.zero;
            rect.pivot = Vector2.zero;
            rect.anchoredPosition = Vector2.zero;
            rect.sizeDelta = new Vector2(CanvasWidth, CanvasHeight);

            GameObject template = CreateArrowTemplate(rect);

            var view = root.AddComponent<EdgeIndicatorView>();
            view.content = content;
            view.indicatorRoot = rect;
            view.indicatorTemplate = template;
            content.SetActive(false);
            return view;
        }

        private static GameObject CreateArrowTemplate(Transform parent)
        {
            var go = new GameObject("EdgeIndicator_Template", typeof(RectTransform));
            go.transform.SetParent(parent, false);

            var rect = go.GetComponent<RectTransform>();
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.zero;
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.sizeDelta = new Vector2(Sc(14f), Sc(14f));

            var label = go.AddComponent<TextMeshProUGUI>();
            label.text = "^";
            label.fontSize = Sc(11f);
            label.alignment = TextAlignmentOptions.Center;
            label.color = new Color(1f, 0.85f, 0.3f, 1f);
            label.raycastTarget = false;

            TMP_FontAsset font = LoadHudFont();
            if (font != null) label.font = font;

            go.SetActive(false);
            return go;
        }

        // Sits above the dialogue box, which occupies the bottom of the screen while a conversation
        // is running.
        private static ChoiceMenuView CreateChoiceMenu(Transform parent)
        {
            var root = new GameObject("ChoiceMenu", typeof(RectTransform));
            root.transform.SetParent(parent, false);

            var content = new GameObject("Content", typeof(RectTransform), typeof(Image));
            content.transform.SetParent(root.transform, false);

            var rect = content.GetComponent<RectTransform>();
            rect.anchorMin = new Vector2(0.5f, 0f);
            rect.anchorMax = new Vector2(0.5f, 0f);
            rect.pivot = new Vector2(0.5f, 0f);
            rect.anchoredPosition = new Vector2(0f, Sc(74f));
            rect.sizeDelta = new Vector2(Sc(180f), Sc(56f));

            var background = content.GetComponent<Image>();
            background.color = new Color(0.06f, 0.07f, 0.10f, 0.92f);
            background.raycastTarget = false;

            RectTransform rows = CreateRowContainer(content.transform);
            GameObject template = CreateRowTemplate(rows);

            var view = root.AddComponent<ChoiceMenuView>();
            view.content = content;
            view.rowContainer = rows;
            view.rowTemplate = template;
            content.SetActive(false);
            return view;
        }

        private static RectTransform CreateRowContainer(Transform parent)
        {
            var go = new GameObject("Rows", typeof(RectTransform), typeof(VerticalLayoutGroup));
            go.transform.SetParent(parent, false);

            var rect = go.GetComponent<RectTransform>();
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = new Vector2(Sc(6f), Sc(4f));
            rect.offsetMax = new Vector2(-Sc(6f), -Sc(4f));

            var layout = go.GetComponent<VerticalLayoutGroup>();
            layout.childForceExpandHeight = false;
            layout.childControlHeight = true;
            layout.childControlWidth = true;
            layout.spacing = Sc(2f);
            return rect;
        }

        // Cloned per option at runtime, so the menu has no fixed row count.
        private static GameObject CreateRowTemplate(Transform parent)
        {
            var go = new GameObject("Row_Template", typeof(RectTransform), typeof(LayoutElement));
            go.transform.SetParent(parent, false);
            go.GetComponent<LayoutElement>().preferredHeight = Sc(12f);

            var label = go.AddComponent<TextMeshProUGUI>();
            label.text = "Option";
            label.fontSize = Sc(8f);
            label.alignment = TextAlignmentOptions.Left;
            label.color = Color.white;
            label.raycastTarget = false;

            TMP_FontAsset font = LoadHudFont();
            if (font != null) label.font = font;

            go.SetActive(false);
            return go;
        }

        private static GameObject CreateCanvas()
        {
            var go = new GameObject("HubHUD", typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));

            var canvas = go.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 10;

            var scaler = go.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(CanvasWidth, CanvasHeight);
            scaler.matchWidthOrHeight = 0.5f;

            return go;
        }

        // Lower centre, so it reads without covering her or whatever she is standing in front of.
        private static InteractionPromptView CreateInteractionPrompt(Transform parent)
        {
            var root = new GameObject("InteractionPrompt", typeof(RectTransform));
            root.transform.SetParent(parent, false);

            var content = new GameObject("Content", typeof(RectTransform));
            content.transform.SetParent(root.transform, false);

            var rect = content.GetComponent<RectTransform>();
            rect.anchorMin = new Vector2(0.5f, 0f);
            rect.anchorMax = new Vector2(0.5f, 0f);
            rect.pivot = new Vector2(0.5f, 0f);
            rect.anchoredPosition = new Vector2(0f, Sc(20f));
            rect.sizeDelta = new Vector2(Sc(64f), Sc(18f));

            AddBackground(content.transform);
            TextMeshProUGUI glyph = AddLabel(content.transform, "Glyph", "Z",
                new Vector2(0f, 0.5f), new Vector2(0f, 0.5f), new Vector2(Sc(4f), 0f), new Vector2(Sc(14f), Sc(14f)),
                TextAlignmentOptions.Center);
            TextMeshProUGUI verb = AddLabel(content.transform, "Verb", "Talk",
                new Vector2(0f, 0.5f), new Vector2(1f, 0.5f), new Vector2(Sc(11f), 0f), new Vector2(-Sc(24f), Sc(14f)),
                TextAlignmentOptions.Left);

            var view = root.AddComponent<InteractionPromptView>();
            view.content = content;
            view.glyphLabel = glyph;
            view.verbLabel = verb;
            content.SetActive(false);
            return view;
        }

        private static void AddBackground(Transform parent)
        {
            var go = new GameObject("Background", typeof(RectTransform), typeof(Image));
            go.transform.SetParent(parent, false);

            var rect = go.GetComponent<RectTransform>();
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;

            var image = go.GetComponent<Image>();
            image.color = new Color(0.06f, 0.07f, 0.10f, 0.85f);
            image.raycastTarget = false;
        }

        private static TextMeshProUGUI AddLabel(Transform parent, string name, string text,
            Vector2 anchorMin, Vector2 anchorMax, Vector2 position, Vector2 size, TextAlignmentOptions alignment)
        {
            var go = new GameObject(name, typeof(RectTransform));
            go.transform.SetParent(parent, false);

            var rect = go.GetComponent<RectTransform>();
            rect.anchorMin = anchorMin;
            rect.anchorMax = anchorMax;
            rect.pivot = new Vector2(0f, 0.5f);
            rect.anchoredPosition = position;
            rect.sizeDelta = size;

            var label = go.AddComponent<TextMeshProUGUI>();
            label.text = text;
            label.fontSize = Sc(9f);
            label.alignment = alignment;
            label.color = Color.white;
            label.raycastTarget = false;

            TMP_FontAsset font = LoadHudFont();
            if (font != null) label.font = font;
            return label;
        }

        private static TMP_FontAsset LoadHudFont()
        {
            string[] guids = AssetDatabase.FindAssets("NotoSans-SemiBold SDF t:TMP_FontAsset");
            return guids.Length > 0
                ? AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(AssetDatabase.GUIDToAssetPath(guids[0]))
                : null;
        }

        private static InputGlyphData LoadOrCreateGlyphData()
        {
            var existing = AssetDatabase.LoadAssetAtPath<InputGlyphData>(GlyphDataPath);
            if (existing != null) return existing;

            if (!AssetDatabase.IsValidFolder("Assets/Gurukul")) AssetDatabase.CreateFolder("Assets", "Hub");
            if (!AssetDatabase.IsValidFolder("Assets/Gurukul/Data")) AssetDatabase.CreateFolder("Assets/Gurukul", "Data");

            var created = ScriptableObject.CreateInstance<InputGlyphData>();
            AssetDatabase.CreateAsset(created, GlyphDataPath);
            return created;
        }
    }
}
