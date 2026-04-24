using System.Collections.Generic;
using System.IO;
using ProjectAstra.Core;
using ProjectAstra.Core.UI;
using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

namespace ProjectAstra.EditorTools
{
    // Experience Scaling — modal level-up screen. Saved to
    // Assets/Resources/Overlays/LevelUpScreen.prefab and instantiated into
    // BattleMap by ExpGranter at scene build time. Follows UM-02's overlay
    // pattern (DialogueSequencePlayer).
    public static class LevelUpScreenBuilder
    {
        const float CanvasWidth  = 1920f;
        const float CanvasHeight = 1080f;
        const float ModalWidth   = 1200f;
        const float ModalHeight  = 720f;

        const string PrefabPath = "Assets/Resources/Overlays/LevelUpScreen.prefab";

        static readonly StatIndex[] Stats =
        {
            StatIndex.HP, StatIndex.Str, StatIndex.Mag, StatIndex.Skl, StatIndex.Spd,
            StatIndex.Def, StatIndex.Res, StatIndex.Con, StatIndex.Niyati,
        };

        [MenuItem("Project Astra/Build Level Up Screen (prefab)")]
        public static void BuildPrefab()
        {
            var dir = Path.GetDirectoryName(PrefabPath);
            if (!string.IsNullOrEmpty(dir) && !Directory.Exists(dir)) Directory.CreateDirectory(dir);

            var rootGo = new GameObject("LevelUpScreen");
            var canvas = rootGo.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 22;

            var scaler = rootGo.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(CanvasWidth, CanvasHeight);
            scaler.matchWidthOrHeight = 0.5f;

            rootGo.AddComponent<GraphicRaycaster>();

            var overlayGo = new GameObject("OverlayRoot", typeof(RectTransform));
            overlayGo.transform.SetParent(rootGo.transform, false);
            StretchFull(overlayGo.GetComponent<RectTransform>());

            // DimBackground must be a CHILD of overlayGo (not a sibling) so it
            // inherits overlayGo.SetActive(false) and doesn't dim the scene
            // while the overlay is dormant.
            var dim = new GameObject("DimBackground", typeof(RectTransform));
            dim.transform.SetParent(overlayGo.transform, false);
            var dimImg = dim.AddComponent<Image>();
            dimImg.color = new Color(0f, 0f, 0f, 0.75f);
            StretchFull(dim.GetComponent<RectTransform>());

            var panelGo = new GameObject("Panel", typeof(RectTransform));
            panelGo.transform.SetParent(overlayGo.transform, false);
            var panelRt = panelGo.GetComponent<RectTransform>();
            panelRt.anchorMin = new Vector2(0.5f, 0.5f);
            panelRt.anchorMax = new Vector2(0.5f, 0.5f);
            panelRt.pivot = new Vector2(0.5f, 0.5f);
            panelRt.sizeDelta = new Vector2(ModalWidth, ModalHeight);
            panelRt.anchoredPosition = Vector2.zero;

            var bgGo = new GameObject("Background", typeof(RectTransform));
            bgGo.transform.SetParent(panelGo.transform, false);
            var bgImg = bgGo.AddComponent<Image>();
            bgImg.color = new Color(0.059f, 0.043f, 0.118f, 0.98f);
            StretchFull(bgGo.GetComponent<RectTransform>());

            var borderGo = new GameObject("Border", typeof(RectTransform));
            borderGo.transform.SetParent(panelGo.transform, false);
            var borderImg = borderGo.AddComponent<Image>();
            borderImg.color = new Color(0.831f, 0.635f, 0.298f, 0.9f);
            var borderRt = borderGo.GetComponent<RectTransform>();
            borderRt.anchorMin = Vector2.zero;
            borderRt.anchorMax = Vector2.one;
            borderRt.offsetMin = new Vector2(-3f, -3f);
            borderRt.offsetMax = new Vector2(3f, 3f);
            borderGo.transform.SetAsFirstSibling();

            var titleGo = new GameObject("Title", typeof(RectTransform));
            titleGo.transform.SetParent(panelGo.transform, false);
            var titleRt = titleGo.GetComponent<RectTransform>();
            titleRt.anchorMin = new Vector2(0f, 0.88f);
            titleRt.anchorMax = new Vector2(1f, 1f);
            titleRt.offsetMin = Vector2.zero;
            titleRt.offsetMax = Vector2.zero;
            var titleTmp = titleGo.AddComponent<TextMeshProUGUI>();
            titleTmp.text = "LEVEL UP";
            titleTmp.fontSize = 48;
            titleTmp.fontStyle = FontStyles.Bold;
            titleTmp.alignment = TextAlignmentOptions.Center;
            titleTmp.color = new Color(0.961f, 0.824f, 0.478f, 1f);
            titleTmp.raycastTarget = false;

            var portraitGo = new GameObject("Portrait", typeof(RectTransform));
            portraitGo.transform.SetParent(panelGo.transform, false);
            var portraitRt = portraitGo.GetComponent<RectTransform>();
            portraitRt.anchorMin = new Vector2(0.03f, 0.30f);
            portraitRt.anchorMax = new Vector2(0.03f, 0.85f);
            portraitRt.pivot = new Vector2(0f, 0.5f);
            portraitRt.sizeDelta = new Vector2(280f, 0f);
            portraitRt.anchoredPosition = Vector2.zero;
            var portraitImg = portraitGo.AddComponent<Image>();
            portraitImg.color = Color.white;
            portraitImg.preserveAspect = true;
            portraitImg.enabled = false;

            var unitNameGo = new GameObject("UnitName", typeof(RectTransform));
            unitNameGo.transform.SetParent(panelGo.transform, false);
            var unitNameRt = unitNameGo.GetComponent<RectTransform>();
            unitNameRt.anchorMin = new Vector2(0.03f, 0.20f);
            unitNameRt.anchorMax = new Vector2(0.32f, 0.28f);
            unitNameRt.offsetMin = Vector2.zero;
            unitNameRt.offsetMax = Vector2.zero;
            var unitNameTmp = unitNameGo.AddComponent<TextMeshProUGUI>();
            unitNameTmp.text = "Unit";
            unitNameTmp.fontSize = 32;
            unitNameTmp.fontStyle = FontStyles.Bold;
            unitNameTmp.alignment = TextAlignmentOptions.Center;
            unitNameTmp.color = new Color(0.961f, 0.898f, 0.776f, 1f);
            unitNameTmp.raycastTarget = false;

            var levelTransGo = new GameObject("LevelTransition", typeof(RectTransform));
            levelTransGo.transform.SetParent(panelGo.transform, false);
            var levelTransRt = levelTransGo.GetComponent<RectTransform>();
            levelTransRt.anchorMin = new Vector2(0.03f, 0.11f);
            levelTransRt.anchorMax = new Vector2(0.32f, 0.19f);
            levelTransRt.offsetMin = Vector2.zero;
            levelTransRt.offsetMax = Vector2.zero;
            var levelTransTmp = levelTransGo.AddComponent<TextMeshProUGUI>();
            levelTransTmp.text = "Level 1  →  2";
            levelTransTmp.fontSize = 24;
            levelTransTmp.alignment = TextAlignmentOptions.Center;
            levelTransTmp.color = new Color(0.541f, 0.478f, 0.345f, 1f);
            levelTransTmp.raycastTarget = false;

            var statRows = BuildStatRows(panelGo.transform);

            var hintGo = new GameObject("ConfirmHint", typeof(RectTransform));
            hintGo.transform.SetParent(panelGo.transform, false);
            var hintRt = hintGo.GetComponent<RectTransform>();
            hintRt.anchorMin = new Vector2(0f, 0.02f);
            hintRt.anchorMax = new Vector2(1f, 0.09f);
            hintRt.offsetMin = Vector2.zero;
            hintRt.offsetMax = Vector2.zero;
            var hintTmp = hintGo.AddComponent<TextMeshProUGUI>();
            hintTmp.text = "▼  Press Confirm to continue";
            hintTmp.fontSize = 18;
            hintTmp.fontStyle = FontStyles.Italic;
            hintTmp.alignment = TextAlignmentOptions.Center;
            hintTmp.color = new Color(0.541f, 0.478f, 0.345f, 1f);
            hintTmp.raycastTarget = false;
            hintGo.SetActive(false);

            var ui = rootGo.AddComponent<LevelUpScreenUI>();
            var so = new SerializedObject(ui);
            so.FindProperty("_overlayRoot").objectReferenceValue = overlayGo;
            so.FindProperty("_portraitImage").objectReferenceValue = portraitImg;
            so.FindProperty("_unitNameText").objectReferenceValue = unitNameTmp;
            so.FindProperty("_levelTransitionText").objectReferenceValue = levelTransTmp;
            so.FindProperty("_confirmHintText").objectReferenceValue = hintTmp;

            var rowsProp = so.FindProperty("_statRows");
            rowsProp.arraySize = statRows.Count;
            for (int i = 0; i < statRows.Count; i++)
            {
                var (stat, cg, label, before, gain, after) = statRows[i];
                var elem = rowsProp.GetArrayElementAtIndex(i);
                elem.FindPropertyRelative("stat").enumValueIndex = (int)stat;
                elem.FindPropertyRelative("rowCanvasGroup").objectReferenceValue = cg;
                elem.FindPropertyRelative("labelText").objectReferenceValue = label;
                elem.FindPropertyRelative("beforeValueText").objectReferenceValue = before;
                elem.FindPropertyRelative("gainText").objectReferenceValue = gain;
                elem.FindPropertyRelative("afterValueText").objectReferenceValue = after;
            }

            so.ApplyModifiedPropertiesWithoutUndo();

            overlayGo.SetActive(false);

            PrefabUtility.SaveAsPrefabAsset(rootGo, PrefabPath, out bool ok);
            Object.DestroyImmediate(rootGo);

            if (ok) Debug.Log($"LevelUpScreen prefab saved to {PrefabPath}");
            else    Debug.LogError("LevelUpScreen prefab save failed.");
        }

        static List<(StatIndex stat, CanvasGroup cg, TMP_Text label, TMP_Text before, TMP_Text gain, TMP_Text after)>
            BuildStatRows(Transform parent)
        {
            var rows = new List<(StatIndex, CanvasGroup, TMP_Text, TMP_Text, TMP_Text, TMP_Text)>();

            // Right half of the modal: 9 stat rows stacked vertically.
            const float regionXMin = 0.35f, regionXMax = 0.97f;
            const float regionYMin = 0.15f, regionYMax = 0.85f;
            float rowHeight = (regionYMax - regionYMin) / Stats.Length;

            for (int i = 0; i < Stats.Length; i++)
            {
                var stat = Stats[i];
                var rowGo = new GameObject($"Row_{stat}", typeof(RectTransform));
                rowGo.transform.SetParent(parent, false);
                var rowRt = rowGo.GetComponent<RectTransform>();
                rowRt.anchorMin = new Vector2(regionXMin, regionYMax - rowHeight * (i + 1));
                rowRt.anchorMax = new Vector2(regionXMax, regionYMax - rowHeight * i);
                rowRt.offsetMin = Vector2.zero;
                rowRt.offsetMax = Vector2.zero;

                var cg = rowGo.AddComponent<CanvasGroup>();
                cg.alpha = 0f;

                var label = MakeRowText(rowGo.transform, "Label", 0.0f, 0.30f, TextAlignmentOptions.MidlineLeft,
                    new Color(0.961f, 0.898f, 0.776f, 1f), FontStyles.Bold, 26, stat.ToString());

                var before = MakeRowText(rowGo.transform, "Before", 0.30f, 0.55f, TextAlignmentOptions.Center,
                    new Color(0.961f, 0.898f, 0.776f, 1f), FontStyles.Normal, 26, "0");

                var gain = MakeRowText(rowGo.transform, "Gain", 0.55f, 0.75f, TextAlignmentOptions.Center,
                    new Color(0.486f, 0.827f, 0.416f, 1f), FontStyles.Bold, 26, "+0");

                var after = MakeRowText(rowGo.transform, "After", 0.75f, 1.0f, TextAlignmentOptions.Center,
                    new Color(0.961f, 0.898f, 0.776f, 1f), FontStyles.Bold, 26, "0");

                rows.Add((stat, cg, label, before, gain, after));
            }

            return rows;
        }

        static TMP_Text MakeRowText(Transform parent, string name, float anchorXMin, float anchorXMax,
            TextAlignmentOptions align, Color color, FontStyles style, int size, string text)
        {
            var go = new GameObject(name, typeof(RectTransform));
            go.transform.SetParent(parent, false);
            var rt = go.GetComponent<RectTransform>();
            rt.anchorMin = new Vector2(anchorXMin, 0f);
            rt.anchorMax = new Vector2(anchorXMax, 1f);
            rt.offsetMin = new Vector2(6f, 0f);
            rt.offsetMax = new Vector2(-6f, 0f);
            var tmp = go.AddComponent<TextMeshProUGUI>();
            tmp.text = text;
            tmp.fontSize = size;
            tmp.fontStyle = style;
            tmp.alignment = align;
            tmp.color = color;
            tmp.raycastTarget = false;
            return tmp;
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
