using System.IO;
using ProjectAstra.Core.UI;
using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

namespace ProjectAstra.EditorTools
{
    // UM-02 — DialogueSequencePlayer overlay prefab. Saved to
    // Assets/Resources/Overlays/DialogueSequence.prefab so LordDeathWatcher
    // (and any future caller) can instantiate it into BattleMap at runtime,
    // matching the pattern used by Dialogue.prefab.
    //
    // Canvas 1920x1080 (reference res), modal-sized (60% width × 25% height
    // centered low). Confirm (handled by DialogueSequencePlayer) advances.
    public static class DialogueSequencePlayerBuilder
    {
        const float CanvasWidth  = 1920f;
        const float CanvasHeight = 1080f;
        const bool  IsFullScreen = false;
        const float ModalWidthFrac  = 0.60f;
        const float ModalHeightFrac = 0.25f;
        const float Scale        = 1f;

        const string PrefabPath = "Assets/Resources/Overlays/DialogueSequence.prefab";

        [MenuItem("Project Astra/Build Dialogue Sequence Overlay (prefab)")]
        public static void BuildPrefab()
        {
            if (IsFullScreen && (Mathf.Abs(ModalWidthFrac - 1f) > 0.001f || Mathf.Abs(ModalHeightFrac - 1f) > 0.001f))
                Debug.LogError("IsFullScreen=true but modal fractions != 1. Fix constants.");

            var dir = Path.GetDirectoryName(PrefabPath);
            if (!string.IsNullOrEmpty(dir) && !Directory.Exists(dir)) Directory.CreateDirectory(dir);

            var rootGo = new GameObject("DialogueSequence");
            var canvas = rootGo.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 20;

            var scaler = rootGo.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(CanvasWidth, CanvasHeight);
            scaler.matchWidthOrHeight = 0.5f;

            rootGo.AddComponent<GraphicRaycaster>();

            var overlayGo = new GameObject("OverlayRoot", typeof(RectTransform));
            overlayGo.transform.SetParent(rootGo.transform, false);

            // DimBackground must be a CHILD of overlayGo (not a sibling) so it
            // inherits overlayGo.SetActive(false) and doesn't dim the scene
            // while the overlay is dormant.
            var dim = new GameObject("DimBackground", typeof(RectTransform));
            dim.transform.SetParent(overlayGo.transform, false);
            dim.AddComponent<Image>().color = new Color(0f, 0f, 0f, 0.75f);
            StretchFull(dim.GetComponent<RectTransform>());

            var panelGo = new GameObject("Panel", typeof(RectTransform));
            panelGo.transform.SetParent(overlayGo.transform, false);
            var panelRect = panelGo.GetComponent<RectTransform>();
            panelRect.anchorMin = new Vector2(0.5f - ModalWidthFrac / 2f, 0.08f);
            panelRect.anchorMax = new Vector2(0.5f + ModalWidthFrac / 2f, 0.08f + ModalHeightFrac);
            panelRect.offsetMin = Vector2.zero;
            panelRect.offsetMax = Vector2.zero;

            var bgGo = new GameObject("Background", typeof(RectTransform));
            bgGo.transform.SetParent(panelGo.transform, false);
            var bgImg = bgGo.AddComponent<Image>();
            bgImg.color = new Color(0.06f, 0.04f, 0.10f, 0.96f);
            StretchFull(bgGo.GetComponent<RectTransform>());

            var borderGo = new GameObject("Border", typeof(RectTransform));
            borderGo.transform.SetParent(panelGo.transform, false);
            var borderImg = borderGo.AddComponent<Image>();
            borderImg.color = new Color(0.831f, 0.635f, 0.298f, 0.85f);
            var borderRect = borderGo.GetComponent<RectTransform>();
            borderRect.anchorMin = Vector2.zero;
            borderRect.anchorMax = Vector2.one;
            borderRect.offsetMin = new Vector2(-3f, -3f);
            borderRect.offsetMax = new Vector2(3f, 3f);
            borderGo.transform.SetAsFirstSibling();

            var lineGo = new GameObject("LineText", typeof(RectTransform));
            lineGo.transform.SetParent(panelGo.transform, false);
            var lineRect = lineGo.GetComponent<RectTransform>();
            lineRect.anchorMin = new Vector2(0.05f, 0.25f);
            lineRect.anchorMax = new Vector2(0.95f, 0.95f);
            lineRect.offsetMin = Vector2.zero;
            lineRect.offsetMax = Vector2.zero;
            var lineTmp = lineGo.AddComponent<TextMeshProUGUI>();
            lineTmp.text = "";
            lineTmp.fontSize = 38;
            lineTmp.alignment = TextAlignmentOptions.Center;
            lineTmp.color = new Color(0.961f, 0.898f, 0.776f, 1f);
            lineTmp.enableWordWrapping = true;

            var hintGo = new GameObject("ContinueHint", typeof(RectTransform));
            hintGo.transform.SetParent(panelGo.transform, false);
            var hintRect = hintGo.GetComponent<RectTransform>();
            hintRect.anchorMin = new Vector2(0.05f, 0.05f);
            hintRect.anchorMax = new Vector2(0.95f, 0.20f);
            hintRect.offsetMin = Vector2.zero;
            hintRect.offsetMax = Vector2.zero;
            var hintTmp = hintGo.AddComponent<TextMeshProUGUI>();
            hintTmp.text = "▼  Press Confirm to continue";
            hintTmp.fontSize = 18;
            hintTmp.fontStyle = FontStyles.Italic;
            hintTmp.alignment = TextAlignmentOptions.Center;
            hintTmp.color = new Color(0.541f, 0.478f, 0.345f, 1f);

            var player = rootGo.AddComponent<DialogueSequencePlayer>();
            var so = new SerializedObject(player);
            so.FindProperty("_overlayRoot").objectReferenceValue = overlayGo;
            so.FindProperty("_lineText").objectReferenceValue = lineTmp;
            so.ApplyModifiedPropertiesWithoutUndo();

            overlayGo.SetActive(false);

            PrefabUtility.SaveAsPrefabAsset(rootGo, PrefabPath, out bool ok);
            Object.DestroyImmediate(rootGo);

            if (ok) Debug.Log($"DialogueSequence prefab saved to {PrefabPath}");
            else    Debug.LogError("DialogueSequence prefab save failed.");
        }

        static void StretchFull(RectTransform rect)
        {
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;
        }
    }
}
