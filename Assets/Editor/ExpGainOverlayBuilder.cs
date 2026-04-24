using System.IO;
using ProjectAstra.Core.UI;
using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

namespace ProjectAstra.EditorTools
{
    // Experience Scaling — corner EXP-gain popup. Saved to
    // Assets/Resources/Overlays/ExpGain.prefab so ExpGranter instantiates it
    // into BattleMap at scene build time.
    public static class ExpGainOverlayBuilder
    {
        const float CanvasWidth  = 1920f;
        const float CanvasHeight = 1080f;
        const float PanelWidth   = 520f;
        const float PanelHeight  = 160f;

        const string PrefabPath = "Assets/Resources/Overlays/ExpGain.prefab";

        [MenuItem("Project Astra/Build Exp Gain Overlay (prefab)")]
        public static void BuildPrefab()
        {
            var dir = Path.GetDirectoryName(PrefabPath);
            if (!string.IsNullOrEmpty(dir) && !Directory.Exists(dir)) Directory.CreateDirectory(dir);

            var rootGo = new GameObject("ExpGain");
            var canvas = rootGo.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 15;

            var scaler = rootGo.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(CanvasWidth, CanvasHeight);
            scaler.matchWidthOrHeight = 0.5f;

            rootGo.AddComponent<GraphicRaycaster>();

            var overlayGo = new GameObject("OverlayRoot", typeof(RectTransform));
            overlayGo.transform.SetParent(rootGo.transform, false);
            var overlayRt = overlayGo.GetComponent<RectTransform>();
            overlayRt.anchorMin = Vector2.zero;
            overlayRt.anchorMax = Vector2.one;
            overlayRt.offsetMin = Vector2.zero;
            overlayRt.offsetMax = Vector2.zero;

            var cg = overlayGo.AddComponent<CanvasGroup>();
            cg.blocksRaycasts = false;
            cg.alpha = 0f;

            var panelGo = new GameObject("Panel", typeof(RectTransform));
            panelGo.transform.SetParent(overlayGo.transform, false);
            var panelRt = panelGo.GetComponent<RectTransform>();
            panelRt.anchorMin = new Vector2(1f, 0f);
            panelRt.anchorMax = new Vector2(1f, 0f);
            panelRt.pivot = new Vector2(1f, 0f);
            panelRt.sizeDelta = new Vector2(PanelWidth, PanelHeight);
            panelRt.anchoredPosition = new Vector2(-40f, 40f);

            var bgGo = new GameObject("Background", typeof(RectTransform));
            bgGo.transform.SetParent(panelGo.transform, false);
            var bgImg = bgGo.AddComponent<Image>();
            bgImg.color = new Color(0.06f, 0.04f, 0.10f, 0.92f);
            StretchFull(bgGo.GetComponent<RectTransform>());

            var borderGo = new GameObject("Border", typeof(RectTransform));
            borderGo.transform.SetParent(panelGo.transform, false);
            var borderImg = borderGo.AddComponent<Image>();
            borderImg.color = new Color(0.831f, 0.635f, 0.298f, 0.85f);
            var borderRt = borderGo.GetComponent<RectTransform>();
            borderRt.anchorMin = Vector2.zero;
            borderRt.anchorMax = Vector2.one;
            borderRt.offsetMin = new Vector2(-2f, -2f);
            borderRt.offsetMax = new Vector2(2f, 2f);
            borderGo.transform.SetAsFirstSibling();

            var unitNameGo = new GameObject("UnitName", typeof(RectTransform));
            unitNameGo.transform.SetParent(panelGo.transform, false);
            var unitNameRt = unitNameGo.GetComponent<RectTransform>();
            unitNameRt.anchorMin = new Vector2(0f, 0.55f);
            unitNameRt.anchorMax = new Vector2(1f, 0.95f);
            unitNameRt.offsetMin = new Vector2(18f, 0f);
            unitNameRt.offsetMax = new Vector2(-18f, 0f);
            var unitNameTmp = unitNameGo.AddComponent<TextMeshProUGUI>();
            unitNameTmp.text = "Unit";
            unitNameTmp.fontSize = 26;
            unitNameTmp.fontStyle = FontStyles.Bold;
            unitNameTmp.alignment = TextAlignmentOptions.MidlineLeft;
            unitNameTmp.color = new Color(0.961f, 0.898f, 0.776f, 1f);
            unitNameTmp.raycastTarget = false;

            var gainGo = new GameObject("GainText", typeof(RectTransform));
            gainGo.transform.SetParent(panelGo.transform, false);
            var gainRt = gainGo.GetComponent<RectTransform>();
            gainRt.anchorMin = new Vector2(0f, 0.55f);
            gainRt.anchorMax = new Vector2(1f, 0.95f);
            gainRt.offsetMin = new Vector2(0f, 0f);
            gainRt.offsetMax = new Vector2(-18f, 0f);
            var gainTmp = gainGo.AddComponent<TextMeshProUGUI>();
            gainTmp.text = "+0 EXP";
            gainTmp.fontSize = 28;
            gainTmp.fontStyle = FontStyles.Bold;
            gainTmp.alignment = TextAlignmentOptions.MidlineRight;
            gainTmp.color = new Color(0.961f, 0.824f, 0.478f, 1f);
            gainTmp.raycastTarget = false;

            var counterGo = new GameObject("CounterText", typeof(RectTransform));
            counterGo.transform.SetParent(panelGo.transform, false);
            var counterRt = counterGo.GetComponent<RectTransform>();
            counterRt.anchorMin = new Vector2(0f, 0.08f);
            counterRt.anchorMax = new Vector2(1f, 0.48f);
            counterRt.offsetMin = new Vector2(18f, 0f);
            counterRt.offsetMax = new Vector2(-18f, 0f);
            var counterTmp = counterGo.AddComponent<TextMeshProUGUI>();
            counterTmp.text = "0 / 100";
            counterTmp.fontSize = 32;
            counterTmp.alignment = TextAlignmentOptions.Center;
            counterTmp.color = new Color(0.961f, 0.898f, 0.776f, 1f);
            counterTmp.raycastTarget = false;

            var ui = rootGo.AddComponent<ExpGainOverlayUI>();
            var so = new SerializedObject(ui);
            so.FindProperty("_overlayRoot").objectReferenceValue = overlayGo;
            so.FindProperty("_canvasGroup").objectReferenceValue = cg;
            so.FindProperty("_unitNameText").objectReferenceValue = unitNameTmp;
            so.FindProperty("_counterText").objectReferenceValue = counterTmp;
            so.FindProperty("_gainText").objectReferenceValue = gainTmp;
            so.ApplyModifiedPropertiesWithoutUndo();

            overlayGo.SetActive(false);

            PrefabUtility.SaveAsPrefabAsset(rootGo, PrefabPath, out bool ok);
            Object.DestroyImmediate(rootGo);

            if (ok) Debug.Log($"ExpGain prefab saved to {PrefabPath}");
            else    Debug.LogError("ExpGain prefab save failed.");
        }

        static void StretchFull(RectTransform rt)
        {
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.offsetMin = Vector2.zero;
            rt.offsetMax = Vector2.zero;
        }
    }
}
