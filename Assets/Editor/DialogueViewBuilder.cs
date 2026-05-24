using System.IO;
using ProjectAstra.Core.UI.Dialogue;
using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

namespace ProjectAstra.EditorTools
{
    // Builds the shared dialogue view prefab to Assets/Resources/UI/DialogueView.prefab.
    // DialogueService loads and instantiates it once at runtime (persisting across
    // scenes), so it serves both the Cutscene opening and battle-map overlays.
    // Mirrors DialogueSequencePlayerBuilder's conventions (1920x1080 overlay canvas,
    // Background rect pattern, TMP UGUI).
    public static class DialogueViewBuilder
    {
        const float CanvasWidth  = 1920f;
        const float CanvasHeight = 1080f;
        const string PrefabPath  = "Assets/Resources/UI/DialogueView.prefab";

        [MenuItem("Project Astra/Build Dialogue View (prefab)")]
        public static void BuildPrefab()
        {
            EnsureFolderExists();

            var rootGo = BuildCanvas();
            var content = BuildContentRoot(rootGo);
            var fullScreen = BuildFullScreenImage(content);
            var (left, right, center) = BuildPortraits(content);
            var box = BuildDialogueBox(content);
            var nameLabel = BuildNameLabel(box);
            var bodyText = BuildBodyText(box);
            var continueHint = BuildContinueHint(box);

            WireView(rootGo, content, fullScreen, left, right, center, nameLabel, bodyText, continueHint);
            content.SetActive(false);

            SavePrefab(rootGo);
        }

        private static void EnsureFolderExists()
        {
            var dir = Path.GetDirectoryName(PrefabPath);
            if (!string.IsNullOrEmpty(dir) && !Directory.Exists(dir)) Directory.CreateDirectory(dir);
        }

        private static GameObject BuildCanvas()
        {
            var rootGo = new GameObject("DialogueView");
            var canvas = rootGo.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 30;

            var scaler = rootGo.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(CanvasWidth, CanvasHeight);
            scaler.matchWidthOrHeight = 0.5f;

            rootGo.AddComponent<GraphicRaycaster>();
            return rootGo;
        }

        private static GameObject BuildContentRoot(GameObject parent)
        {
            var content = new GameObject("Root", typeof(RectTransform));
            content.transform.SetParent(parent.transform, false);
            StretchFull(content.GetComponent<RectTransform>());
            return content;
        }

        private static Image BuildFullScreenImage(GameObject parent)
        {
            var go = new GameObject("FullScreenImage", typeof(RectTransform));
            go.transform.SetParent(parent.transform, false);
            StretchFull(go.GetComponent<RectTransform>());
            var img = go.AddComponent<Image>();
            img.color = Color.white;
            img.enabled = false;
            return img;
        }

        private static (Image left, Image right, Image center) BuildPortraits(GameObject parent)
        {
            var left = BuildPortrait(parent, "LeftPortrait", new Vector2(0f, 0f), new Vector2(0f, 0f));
            var right = BuildPortrait(parent, "RightPortrait", new Vector2(1f, 0f), new Vector2(1f, 0f));
            var center = BuildPortrait(parent, "CenterPortrait", new Vector2(0.5f, 0f), new Vector2(0.5f, 0f));
            return (left, right, center);
        }

        private static Image BuildPortrait(GameObject parent, string name, Vector2 anchor, Vector2 pivot)
        {
            var go = new GameObject(name, typeof(RectTransform));
            go.transform.SetParent(parent.transform, false);
            var rect = go.GetComponent<RectTransform>();
            rect.anchorMin = anchor;
            rect.anchorMax = anchor;
            rect.pivot = pivot;
            rect.sizeDelta = new Vector2(760f, 980f);
            rect.anchoredPosition = new Vector2(0f, 40f);
            var img = go.AddComponent<Image>();
            img.preserveAspect = true;
            img.enabled = false;
            return img;
        }

        private static GameObject BuildDialogueBox(GameObject parent)
        {
            var box = new GameObject("DialogueBox", typeof(RectTransform));
            box.transform.SetParent(parent.transform, false);
            var rect = box.GetComponent<RectTransform>();
            rect.anchorMin = new Vector2(0.06f, 0.05f);
            rect.anchorMax = new Vector2(0.94f, 0.30f);
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;

            var border = AddChildImage(box, "Border", new Color(0.831f, 0.635f, 0.298f, 0.9f));
            var borderRect = border.rectTransform;
            borderRect.anchorMin = Vector2.zero;
            borderRect.anchorMax = Vector2.one;
            borderRect.offsetMin = new Vector2(-3f, -3f);
            borderRect.offsetMax = new Vector2(3f, 3f);

            var background = AddChildImage(box, "Background", new Color(0.06f, 0.04f, 0.10f, 0.94f));
            StretchFull(background.rectTransform);
            return box;
        }

        private static TMP_Text BuildNameLabel(GameObject box)
        {
            var go = new GameObject("NameLabel", typeof(RectTransform));
            go.transform.SetParent(box.transform, false);
            var rect = go.GetComponent<RectTransform>();
            rect.anchorMin = new Vector2(0.02f, 1f);
            rect.anchorMax = new Vector2(0.4f, 1f);
            rect.pivot = new Vector2(0f, 0f);
            rect.sizeDelta = new Vector2(0f, 48f);
            rect.anchoredPosition = new Vector2(20f, 6f);
            var tmp = go.AddComponent<TextMeshProUGUI>();
            tmp.fontSize = 30;
            tmp.fontStyle = FontStyles.Bold;
            tmp.color = new Color(0.961f, 0.835f, 0.541f, 1f);
            return tmp;
        }

        private static TMP_Text BuildBodyText(GameObject box)
        {
            var go = new GameObject("BodyText", typeof(RectTransform));
            go.transform.SetParent(box.transform, false);
            var rect = go.GetComponent<RectTransform>();
            rect.anchorMin = new Vector2(0.04f, 0.12f);
            rect.anchorMax = new Vector2(0.96f, 0.88f);
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;
            var tmp = go.AddComponent<TextMeshProUGUI>();
            tmp.fontSize = 34;
            tmp.color = new Color(0.961f, 0.898f, 0.776f, 1f);
            tmp.enableWordWrapping = true;
            tmp.alignment = TextAlignmentOptions.TopLeft;
            return tmp;
        }

        private static GameObject BuildContinueHint(GameObject box)
        {
            var go = new GameObject("ContinueHint", typeof(RectTransform));
            go.transform.SetParent(box.transform, false);
            var rect = go.GetComponent<RectTransform>();
            rect.anchorMin = new Vector2(0.85f, 0.04f);
            rect.anchorMax = new Vector2(0.97f, 0.20f);
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;
            var tmp = go.AddComponent<TextMeshProUGUI>();
            tmp.text = "▼";
            tmp.fontSize = 26;
            tmp.alignment = TextAlignmentOptions.Right;
            tmp.color = new Color(0.541f, 0.478f, 0.345f, 1f);
            go.SetActive(false);
            return go;
        }

        private static void WireView(GameObject rootGo, GameObject content, Image fullScreen,
            Image left, Image right, Image center, TMP_Text nameLabel, TMP_Text bodyText, GameObject continueHint)
        {
            var view = rootGo.AddComponent<DialogueView>();
            var so = new SerializedObject(view);
            so.FindProperty("_root").objectReferenceValue = content;
            so.FindProperty("_fullScreenImage").objectReferenceValue = fullScreen;
            so.FindProperty("_leftPortrait").objectReferenceValue = left;
            so.FindProperty("_rightPortrait").objectReferenceValue = right;
            so.FindProperty("_centerPortrait").objectReferenceValue = center;
            so.FindProperty("_nameLabel").objectReferenceValue = nameLabel;
            so.FindProperty("_bodyText").objectReferenceValue = bodyText;
            so.FindProperty("_continueHint").objectReferenceValue = continueHint;
            so.ApplyModifiedPropertiesWithoutUndo();
        }

        private static Image AddChildImage(GameObject parent, string name, Color color)
        {
            var go = new GameObject(name, typeof(RectTransform));
            go.transform.SetParent(parent.transform, false);
            var img = go.AddComponent<Image>();
            img.color = color;
            return img;
        }

        private static void SavePrefab(GameObject rootGo)
        {
            PrefabUtility.SaveAsPrefabAsset(rootGo, PrefabPath, out bool ok);
            Object.DestroyImmediate(rootGo);
            if (ok) Debug.Log($"DialogueView prefab saved to {PrefabPath}");
            else Debug.LogError("DialogueView prefab save failed.");
        }

        private static void StretchFull(RectTransform rect)
        {
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;
        }
    }
}
