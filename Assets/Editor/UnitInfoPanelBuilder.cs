using System.Collections.Generic;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;

namespace ProjectAstra.EditorTools
{
    // ===========================================================================================
    // Unit Info Panel (temp) — full-screen 1920×1080 info screen.
    //
    // Target resolution: 1920×1080 (matches the Canvas reference resolution).
    // Screen kind:       FULL_SCREEN (not a modal popup — the panel fills the whole canvas).
    //
    // The Figma source was originally authored at 1197×673, then rescaled to 1920×1080 via the
    // Figma Plugin API (all child positions, sizes, stroke widths, and font sizes scaled
    // proportionally by ~1.6037×). The ORIGINAL 1197×673 authoring values are kept in this file
    // so the code remains readable against the original Figma design, and every dimensional
    // value is wrapped in V2(...) or Sc(...) which multiplies by Scale at use time.
    //
    // If the panel is ever retargeted to a new resolution, change only the `Scale` constant.
    //
    // See `docs/UI_WORKFLOW.md` for the full Figma-to-Unity pipeline.
    // ===========================================================================================
    public static class UnitInfoPanelBuilder
    {
        // ---- target resolution & screen kind ----
        const float CanvasWidth  = 1920f;
        const float CanvasHeight = 1080f;
        const bool  IsFullScreen = true;

        // Design reference scale. Original Figma panel frame was 1197.2249×673.4375; the new
        // target is 1920×1080. 1920 / 1197.2249 ≈ 1.6037086.
        const float Scale = 1.6037086f;

        // ---- paths ----
        const string SpriteDir = "Assets/UI/UnitInfoPanel/Icons/";
        const string FrameDir  = "Assets/UI/UnitInfoPanel/Sprites/";
        const string FontDir   = "Assets/UI/UnitInfoPanel/Fonts/";

        // ---- colours ----
        static readonly Color ColBorderBrown   = new Color32(0x2a, 0x18, 0x10, 0xff);
        static readonly Color ColBorderGold    = new Color32(0xd4, 0xa1, 0x42, 0xff);
        static readonly Color ColParchment     = new Color32(0xd4, 0xac, 0x68, 0xff);
        static readonly Color ColFrameGold     = new Color32(0xf4, 0xd2, 0x7a, 0xff);

        static readonly Color ColTextBrown     = new Color32(0x3a, 0x20, 0x08, 0xff);
        static readonly Color ColTextBrownDk   = new Color32(0x5a, 0x36, 0x18, 0xff);
        static readonly Color ColTextRed       = new Color32(0x96, 0x20, 0x12, 0xff);
        static readonly Color ColTextNavy      = new Color32(0x0c, 0x18, 0x32, 0xff);
        static readonly Color ColTextBlue      = new Color32(0x1a, 0x2c, 0x5a, 0xff);
        static readonly Color ColTextGold      = new Color32(0xf6, 0xd9, 0x7a, 0xff);
        static readonly Color ColTextLabel     = new Color32(0xa7, 0xc0, 0xee, 0xff);

        // ---- fonts (assigned in Build()) ----
        static TMP_FontAsset cinzel, cinzelDecor, cormorant, cormorantItalic;

        // ==================================================================
        // entry point
        // ==================================================================

        [MenuItem("Project Astra/Build Unit Info Panel (temp)")]
        public static void Build()
        {
            // Sanity check: if the screen is declared full-screen, the outer panel
            // dimensions (below) must equal the canvas reference resolution.
            if (IsFullScreen && (Mathf.Abs(Sc(1197) - CanvasWidth) > 1f || Mathf.Abs(Sc(673) - CanvasHeight) > 1f))
            {
                Debug.LogError($"UnitInfoPanelBuilder: panel dimensions ({Sc(1197)}×{Sc(673)}) " +
                               $"do not match canvas reference ({CanvasWidth}×{CanvasHeight}). " +
                               "Fix the Scale constant or set IsFullScreen = false.");
                return;
            }

            cinzel          = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(FontDir + "Cinzel SDF.asset");
            cinzelDecor     = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(FontDir + "CinzelDecorative SDF.asset");
            cormorant       = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(FontDir + "CormorantGaramond SDF.asset");
            cormorantItalic = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(FontDir + "CormorantGaramondItalic SDF.asset");

            var canvas = EnsureCanvas();
            var existing = canvas.transform.Find("UnitInfoPanel");
            if (existing != null) Object.DestroyImmediate(existing.gameObject);

            var panel = BuildPanel(canvas.transform);

            EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());
            Selection.activeGameObject = panel;
            Debug.Log("UnitInfoPanel built.");
        }

        // ==================================================================
        // canvas
        // ==================================================================

        static Canvas EnsureCanvas()
        {
            var existing = Object.FindObjectOfType<Canvas>();
            if (existing != null && existing.renderMode == RenderMode.ScreenSpaceOverlay)
                return existing;

            var go = new GameObject("UICanvas",
                typeof(Canvas),
                typeof(CanvasScaler),
                typeof(GraphicRaycaster));
            var c = go.GetComponent<Canvas>();
            c.renderMode = RenderMode.ScreenSpaceOverlay;
            c.sortingOrder = 10;

            var scaler = go.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(CanvasWidth, CanvasHeight);
            scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
            scaler.matchWidthOrHeight = 0.5f;
            return c;
        }

        // ==================================================================
        // panel
        // ==================================================================

        static GameObject BuildPanel(Transform parent)
        {
            // Triple border as nested solid-color Images (Figma uses a box-shadow
            // hack for the layered brown/gold/brown outline which doesn't translate
            // 1:1; nesting Images is the cleanest Unity equivalent).
            var outer = NewImage("UnitInfoPanel", parent, ColBorderBrown);
            SetCenter(outer, Sc(1197), Sc(673));

            var gold = NewImage("BorderGold", outer.transform, ColBorderGold);
            SetStretch(gold, Sc(4));

            var innerBrown = NewImage("BorderInnerBrown", gold.transform, ColBorderBrown);
            SetStretch(innerBrown, Sc(4));

            // Innermost parchment surface — uses the Figma-exported gradient sprite
            // (node 11:2 Background). Clean, no children/text baked in.
            var parchment = NewImage("Parchment", innerBrown.transform, Color.white);
            SetStretch(parchment, Sc(5));
            var parchmentImg = parchment.GetComponent<Image>();
            parchmentImg.sprite = LoadFrame("panel_bg.png");
            parchmentImg.type = Image.Type.Simple;

            // Pure layout container for the left/right sections.
            var content = NewRect("Content", parchment.transform);
            SetStretch(content, 0);
            content.offsetMin = V2(59, 33);
            content.offsetMax = V2(-59, -33);

            BuildLeftSection(content);
            BuildRightSection(content);

            return outer;
        }

        // ==================================================================
        // left section
        // ==================================================================

        static void BuildLeftSection(RectTransform parent)
        {
            var left = NewRect("LeftSection", parent);
            left.anchorMin = new Vector2(0, 1);
            left.anchorMax = new Vector2(0, 1);
            left.pivot = new Vector2(0, 1);
            left.anchoredPosition = Vector2.zero;
            left.sizeDelta = V2(409, 606);

            BuildPortraitFrame(left);
            BuildNameBlock(left);
            BuildClassBlock(left);
        }

        static void BuildPortraitFrame(RectTransform parent)
        {
            // Portrait frame background — Figma node 11:3 Background:
            // navy gradient + 2.4px gold stroke baked in, no corners, no figure.
            var frame = NewImage("PortraitFrame", parent, Color.white);
            var rt = frame.GetComponent<RectTransform>();
            rt.anchorMin = new Vector2(0, 1);
            rt.anchorMax = new Vector2(0, 1);
            rt.pivot = new Vector2(0, 1);
            rt.anchoredPosition = Vector2.zero;
            rt.sizeDelta = V2(409, 411);
            var img = frame.GetComponent<Image>();
            img.sprite = LoadFrame("portrait_bg.png");
            img.type = Image.Type.Simple;

            // Placeholder character silhouette — Figma node 3:189. Shown when
            // no real portrait is assigned; swap its sprite at runtime.
            var placeholder = NewImage("PortraitPlaceholder", rt, Color.white);
            SetStretch(placeholder, Sc(4));
            var placeholderImg = placeholder.GetComponent<Image>();
            placeholderImg.sprite = LoadFrame("portrait_placeholder.png");
            placeholderImg.preserveAspect = true;

            // Corner ornaments layered on top of the frame (individual children).
            AddCorner(rt, "Corner_TL", new Vector2(0, 1), V2(8, -8),  0);
            AddCorner(rt, "Corner_TR", new Vector2(1, 1), V2(-8, -8), 90);
            AddCorner(rt, "Corner_BL", new Vector2(0, 0), V2(8, 8),   -90);
            AddCorner(rt, "Corner_BR", new Vector2(1, 0), V2(-8, 8),  180);
        }

        static void AddCorner(RectTransform parent, string name, Vector2 anchor, Vector2 pos, float rot)
        {
            var go = NewImage(name, parent, ColFrameGold);
            var rt = go.GetComponent<RectTransform>();
            rt.anchorMin = anchor;
            rt.anchorMax = anchor;
            rt.pivot = anchor;
            rt.sizeDelta = V2(36, 36);
            rt.anchoredPosition = pos;
            rt.localRotation = Quaternion.Euler(0, 0, rot);
            go.GetComponent<Image>().sprite = LoadSprite("corner_ornament_a.png");
            go.GetComponent<Image>().preserveAspect = true;
        }

        static void BuildNameBlock(RectTransform parent)
        {
            var nameText = NewText("UnitName", parent, "Tana", cormorantItalic, Sc(54), ColTextBrown,
                TextAlignmentOptions.Center, FontStyles.Bold | FontStyles.Italic);
            var rt = nameText.rectTransform;
            rt.anchorMin = new Vector2(0, 1);
            rt.anchorMax = new Vector2(1, 1);
            rt.pivot = new Vector2(0.5f, 1);
            rt.anchoredPosition = V2(0, -424);
            rt.sizeDelta = V2(0, 50);
        }

        static void BuildClassBlock(RectTransform parent)
        {
            var block = NewRect("ClassBlock", parent);
            block.anchorMin = new Vector2(0, 1);
            block.anchorMax = new Vector2(1, 1);
            block.pivot = new Vector2(0.5f, 1);
            block.anchoredPosition = V2(0, -484);
            block.sizeDelta = V2(0, 122);

            var className = NewText("ClassName", block, "Wyvern Knight", cinzelDecor, Sc(28), ColTextBrown,
                TextAlignmentOptions.TopLeft, FontStyles.Bold);
            className.characterSpacing = 4f;
            var cnRt = className.rectTransform;
            cnRt.anchorMin = new Vector2(0, 1);
            cnRt.anchorMax = new Vector2(1, 1);
            cnRt.pivot = new Vector2(0, 1);
            cnRt.anchoredPosition = V2(8, -2);
            cnRt.sizeDelta = V2(-16, 40);

            BuildStatRow(block, "LvRow",
                new[] { ("LV", false), ("12", true), ("E", false), ("76", true) },
                V2(8, -44));

            BuildStatRow(block, "HpRow",
                new[] { ("HP", false), ("45", true), ("/", false), ("45", true) },
                V2(8, -85));

            var classIcon = NewImage("ClassIcon", block, ColTextBrown);
            var ciRt = classIcon.GetComponent<RectTransform>();
            ciRt.anchorMin = new Vector2(1, 1);
            ciRt.anchorMax = new Vector2(1, 1);
            ciRt.pivot = new Vector2(1, 1);
            ciRt.sizeDelta = V2(78, 78);
            ciRt.anchoredPosition = V2(-8, -43);
            classIcon.GetComponent<Image>().sprite = LoadSprite("class_wyvern_knight.png");
            classIcon.GetComponent<Image>().preserveAspect = true;
        }

        static void BuildStatRow(RectTransform parent, string name, (string text, bool isValue)[] parts, Vector2 pos)
        {
            var row = NewRect(name, parent);
            row.anchorMin = new Vector2(0, 1);
            row.anchorMax = new Vector2(0, 1);
            row.pivot = new Vector2(0, 1);
            row.anchoredPosition = pos;
            row.sizeDelta = V2(300, 34);

            var layout = row.gameObject.AddComponent<HorizontalLayoutGroup>();
            layout.childAlignment = TextAnchor.MiddleLeft;
            layout.spacing = Sc(10);
            layout.childForceExpandWidth = false;
            layout.childForceExpandHeight = false;
            layout.childControlWidth = true;
            layout.childControlHeight = true;

            foreach (var (text, isValue) in parts)
            {
                var t = NewText(text, row, text,
                    isValue ? cormorant : cinzel,
                    Sc(25),
                    isValue ? ColTextRed : ColTextBrownDk,
                    TextAlignmentOptions.MidlineLeft,
                    FontStyles.Bold);
                if (!isValue) t.characterSpacing = 8f;
                var fitter = t.gameObject.AddComponent<ContentSizeFitter>();
                fitter.horizontalFit = ContentSizeFitter.FitMode.PreferredSize;
                fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;
            }
        }

        // ==================================================================
        // right section
        // ==================================================================

        static void BuildRightSection(RectTransform parent)
        {
            var right = NewRect("RightSection", parent);
            right.anchorMin = new Vector2(1, 1);
            right.anchorMax = new Vector2(1, 1);
            right.pivot = new Vector2(1, 1);
            right.anchoredPosition = Vector2.zero;
            right.sizeDelta = V2(625, 606);

            BuildItemsBox(right);
            BuildEquipmentBox(right);
        }

        static void BuildItemsBox(RectTransform parent)
        {
            var box = NewRect("ItemsBox", parent);
            box.anchorMin = new Vector2(0, 1);
            box.anchorMax = new Vector2(1, 1);
            box.pivot = new Vector2(0.5f, 1);
            box.anchoredPosition = Vector2.zero;
            box.sizeDelta = V2(0, 434);

            BuildItemsHeader(box);

            var count = NewText("ItemsCount", box, "2 / 3", cormorant, Sc(23), ColTextRed,
                TextAlignmentOptions.TopRight, FontStyles.Bold);
            var cRt = count.rectTransform;
            cRt.anchorMin = new Vector2(1, 1);
            cRt.anchorMax = new Vector2(1, 1);
            cRt.pivot = new Vector2(1, 1);
            cRt.sizeDelta = V2(60, 28);
            cRt.anchoredPosition = V2(-6, -6);

            BuildItemsList(box);
        }

        static void BuildItemsHeader(RectTransform parent)
        {
            var header = NewRect("ItemsHeader", parent);
            header.anchorMin = new Vector2(0, 1);
            header.anchorMax = new Vector2(1, 1);
            header.pivot = new Vector2(0.5f, 1);
            header.anchoredPosition = V2(0, -24);
            header.sizeDelta = V2(0, 46);

            var layout = header.gameObject.AddComponent<HorizontalLayoutGroup>();
            layout.childAlignment = TextAnchor.MiddleCenter;
            layout.spacing = Sc(14);
            layout.childForceExpandWidth = false;
            layout.childForceExpandHeight = false;
            layout.childControlWidth = true;
            layout.childControlHeight = true;

            var dl = NewImage("DividerLeft", header, ColTextBrown);
            var dlRt = dl.GetComponent<RectTransform>();
            dlRt.sizeDelta = V2(140, 18);
            dl.GetComponent<Image>().sprite = LoadSprite("items_divider_left.png");
            dl.GetComponent<Image>().preserveAspect = true;
            var dlEl = dl.AddComponent<LayoutElement>();
            dlEl.preferredWidth = Sc(140); dlEl.preferredHeight = Sc(18);

            var title = NewText("ItemsTitle", header, "Items", cinzelDecor, Sc(34), ColTextNavy,
                TextAlignmentOptions.Midline, FontStyles.Bold);
            title.characterSpacing = 5f;
            var tFitter = title.gameObject.AddComponent<ContentSizeFitter>();
            tFitter.horizontalFit = ContentSizeFitter.FitMode.PreferredSize;

            var dr = NewImage("DividerRight", header, ColTextBrown);
            var drRt = dr.GetComponent<RectTransform>();
            drRt.sizeDelta = V2(140, 18);
            dr.GetComponent<Image>().sprite = LoadSprite("items_divider_right.png");
            dr.GetComponent<Image>().preserveAspect = true;
            var drEl = dr.AddComponent<LayoutElement>();
            drEl.preferredWidth = Sc(140); drEl.preferredHeight = Sc(18);
        }

        static readonly (string name, string icon, string value, string slash, string max, string equipped)[] Items =
        {
            ("Iron Lance", "icon_iron_lance.png",  "18", "/", "45", "E"),
            ("Javelin",    "icon_javelin.png",     "12", "/", "20", ""),
            ("Heavy Spear","icon_heavy_spear.png", "3",  "/", "16", ""),
            ("Elixir",     "icon_elixir.png",      "3",  "/", "3",  ""),
            ("Chest Key",  "icon_chest_key.png",   "1",  "/", "1",  ""),
        };

        static void BuildItemsList(RectTransform parent)
        {
            var list = NewRect("ItemsList", parent);
            list.anchorMin = new Vector2(0, 1);
            list.anchorMax = new Vector2(1, 1);
            list.pivot = new Vector2(0.5f, 1);
            list.anchoredPosition = V2(0, -76);
            list.sizeDelta = V2(-12, 358);

            var layout = list.gameObject.AddComponent<VerticalLayoutGroup>();
            layout.childAlignment = TextAnchor.UpperCenter;
            layout.spacing = Sc(2);
            int pad = Mathf.RoundToInt(Sc(6));
            layout.padding = new RectOffset(pad, pad, pad, pad);
            layout.childForceExpandWidth = true;
            layout.childForceExpandHeight = false;
            layout.childControlWidth = true;
            layout.childControlHeight = true;

            for (int i = 0; i < Items.Length; i++)
                BuildItemRow(list, Items[i], i < Items.Length - 1);
        }

        static void BuildItemRow(RectTransform parent,
            (string name, string icon, string value, string slash, string max, string equipped) item,
            bool showDivider)
        {
            var row = NewRect("ItemRow_" + item.name.Replace(" ", ""), parent);
            row.sizeDelta = V2(0, 47);

            var layout = row.gameObject.AddComponent<HorizontalLayoutGroup>();
            layout.childAlignment = TextAnchor.MiddleLeft;
            layout.spacing = Sc(16);
            int padX = Mathf.RoundToInt(Sc(8));
            int padY = Mathf.RoundToInt(Sc(5));
            layout.padding = new RectOffset(padX, padX, padY, padY);
            layout.childForceExpandWidth = false;
            layout.childForceExpandHeight = false;
            layout.childControlWidth = true;
            layout.childControlHeight = true;

            var le = row.gameObject.AddComponent<LayoutElement>();
            le.preferredHeight = Sc(47);

            if (showDivider)
            {
                var divider = NewImage("Divider", row, new Color(0.23f, 0.12f, 0.03f, 0.25f));
                var dRt = divider.GetComponent<RectTransform>();
                dRt.anchorMin = new Vector2(0, 0);
                dRt.anchorMax = new Vector2(1, 0);
                dRt.pivot = new Vector2(0.5f, 0);
                dRt.sizeDelta = V2(0, 1);
                dRt.anchoredPosition = Vector2.zero;
                divider.transform.SetAsLastSibling();
                var ignored = divider.AddComponent<LayoutElement>();
                ignored.ignoreLayout = true;
            }

            var icon = NewImage("Icon", row, ColTextBrown);
            var iRt = icon.GetComponent<RectTransform>();
            iRt.sizeDelta = V2(30, 30);
            icon.GetComponent<Image>().sprite = LoadSprite(item.icon);
            icon.GetComponent<Image>().preserveAspect = true;
            var iEl = icon.AddComponent<LayoutElement>();
            iEl.preferredWidth = Sc(30); iEl.preferredHeight = Sc(30);

            var nameText = NewText("Name", row, item.name, cinzel, Sc(27), ColTextBrown,
                TextAlignmentOptions.MidlineLeft, FontStyles.Bold);
            var nEl = nameText.gameObject.AddComponent<LayoutElement>();
            nEl.flexibleWidth = 1;

            var uses = NewRect("Uses", row);
            uses.sizeDelta = V2(130, 34);
            var usesLayout = uses.gameObject.AddComponent<HorizontalLayoutGroup>();
            usesLayout.childAlignment = TextAnchor.MiddleRight;
            usesLayout.spacing = Sc(4);
            usesLayout.childForceExpandWidth = false;
            usesLayout.childForceExpandHeight = false;
            usesLayout.childControlWidth = true;
            usesLayout.childControlHeight = true;
            var uEl = uses.gameObject.AddComponent<LayoutElement>();
            uEl.preferredWidth = Sc(130);

            AddUsesText(uses, "Value", item.value, cormorant, ColTextRed, Sc(27));
            AddUsesText(uses, "Slash", item.slash, cormorant, new Color(0.59f, 0.13f, 0.07f, 0.55f), Sc(27));
            AddUsesText(uses, "Max",   item.max,   cormorant, ColTextRed, Sc(27));

            if (!string.IsNullOrEmpty(item.equipped))
                AddUsesText(uses, "Equipped", item.equipped, cinzel, ColTextBlue, Sc(22));
        }

        static void AddUsesText(RectTransform parent, string name, string text, TMP_FontAsset font, Color color, float size)
        {
            var t = NewText(name, parent, text, font, size, color, TextAlignmentOptions.Midline, FontStyles.Bold);
            var fitter = t.gameObject.AddComponent<ContentSizeFitter>();
            fitter.horizontalFit = ContentSizeFitter.FitMode.PreferredSize;
            fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;
        }

        // ==================================================================
        // equipment box
        // ==================================================================

        static void BuildEquipmentBox(RectTransform parent)
        {
            // Equipment box background — Figma node 11:4 Background:
            // navy gradient + 2.4px gold stroke baked in. Clean, no text baked.
            var outer = NewImage("EquipmentBox", parent, Color.white);
            var oRt = outer.GetComponent<RectTransform>();
            oRt.anchorMin = new Vector2(0, 0);
            oRt.anchorMax = new Vector2(1, 0);
            oRt.pivot = new Vector2(0.5f, 0);
            oRt.anchoredPosition = Vector2.zero;
            oRt.sizeDelta = V2(0, 154);
            var outerImg = outer.GetComponent<Image>();
            outerImg.sprite = LoadFrame("equipment_bg.png");
            outerImg.type = Image.Type.Simple;

            var navyRt = oRt;

            var title = NewText("EquipmentTitle", navyRt, "Equipment", cinzelDecor, Sc(29), ColTextGold,
                TextAlignmentOptions.TopLeft, FontStyles.Bold);
            title.characterSpacing = 4f;
            var ttRt = title.rectTransform;
            ttRt.anchorMin = new Vector2(0, 1);
            ttRt.anchorMax = new Vector2(0, 1);
            ttRt.pivot = new Vector2(0, 1);
            ttRt.sizeDelta = V2(440, 38);
            ttRt.anchoredPosition = V2(24, -14);

            AddEqStat(navyRt, "StatRng",  "Rng", "1",   V2(-140, -17));
            AddEqStat(navyRt, "StatAtk",  "Atk", "31",  V2(24,   -58), anchorLeft: true);
            AddEqStat(navyRt, "StatCrit", "Crit","15",  V2(-140, -58));
            AddEqStat(navyRt, "StatHit",  "Hit", "140", V2(24,   -98), anchorLeft: true);
            AddEqStat(navyRt, "StatAvo",  "Avo", "78",  V2(-140, -98));
        }

        static void AddEqStat(RectTransform parent, string name, string label, string value, Vector2 pos, bool anchorLeft = false)
        {
            var row = NewRect(name, parent);
            if (anchorLeft)
            {
                row.anchorMin = new Vector2(0, 1);
                row.anchorMax = new Vector2(0, 1);
                row.pivot = new Vector2(0, 1);
            }
            else
            {
                row.anchorMin = new Vector2(1, 1);
                row.anchorMax = new Vector2(1, 1);
                row.pivot = new Vector2(0, 1);
            }
            row.anchoredPosition = pos;
            row.sizeDelta = V2(130, 34);

            var layout = row.gameObject.AddComponent<HorizontalLayoutGroup>();
            layout.childAlignment = TextAnchor.MiddleLeft;
            layout.spacing = Sc(14);
            layout.childForceExpandWidth = false;
            layout.childForceExpandHeight = false;
            layout.childControlWidth = true;
            layout.childControlHeight = true;

            var lbl = NewText("Label", row, label, cinzel, Sc(25), ColTextLabel,
                TextAlignmentOptions.Midline, FontStyles.Bold);
            lbl.characterSpacing = 8f;
            var lblFit = lbl.gameObject.AddComponent<ContentSizeFitter>();
            lblFit.horizontalFit = ContentSizeFitter.FitMode.PreferredSize;
            lblFit.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

            var val = NewText("Value", row, value, cormorant, Sc(25), ColTextGold,
                TextAlignmentOptions.Midline, FontStyles.Bold);
            var valFit = val.gameObject.AddComponent<ContentSizeFitter>();
            valFit.horizontalFit = ContentSizeFitter.FitMode.PreferredSize;
            valFit.verticalFit = ContentSizeFitter.FitMode.PreferredSize;
        }

        // ==================================================================
        // scale helpers
        // ==================================================================

        // Scales a single length value (font size, spacing, inset, etc).
        static float Sc(float v) => v * Scale;

        // Builds a Vector2 whose components are scaled. Used for sizeDelta,
        // anchoredPosition, offsetMin, offsetMax — anywhere that holds a size
        // or a pixel-space offset. Zero components stay zero (important:
        // zero means "stretch with anchors" in sizeDelta).
        static Vector2 V2(float x, float y) => new Vector2(x * Scale, y * Scale);

        // ==================================================================
        // low-level element helpers
        // ==================================================================

        static Sprite LoadSprite(string filename) =>
            AssetDatabase.LoadAssetAtPath<Sprite>(SpriteDir + filename);

        static Sprite LoadFrame(string filename) =>
            AssetDatabase.LoadAssetAtPath<Sprite>(FrameDir + filename);

        static GameObject NewImage(string name, Transform parent, Color color)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
            go.transform.SetParent(parent, false);
            go.GetComponent<Image>().color = color;
            return go;
        }

        static GameObject NewImage(string name, RectTransform parent, Color color)
            => NewImage(name, (Transform)parent, color);

        static RectTransform NewRect(string name, Transform parent)
        {
            var go = new GameObject(name, typeof(RectTransform));
            go.transform.SetParent(parent, false);
            return go.GetComponent<RectTransform>();
        }

        static RectTransform NewRect(string name, RectTransform parent)
            => NewRect(name, (Transform)parent);

        static TextMeshProUGUI NewText(string name, Transform parent, string text,
            TMP_FontAsset font, float size, Color color,
            TextAlignmentOptions align, FontStyles style)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(TextMeshProUGUI));
            go.transform.SetParent(parent, false);
            var t = go.GetComponent<TextMeshProUGUI>();
            t.text = text;
            if (font != null) t.font = font;
            t.fontSize = size;
            t.color = color;
            t.alignment = align;
            t.fontStyle = style;
            t.enableWordWrapping = false;
            t.overflowMode = TextOverflowModes.Overflow;
            return t;
        }

        static TextMeshProUGUI NewText(string name, RectTransform parent, string text,
            TMP_FontAsset font, float size, Color color,
            TextAlignmentOptions align, FontStyles style)
            => NewText(name, (Transform)parent, text, font, size, color, align, style);

        static void SetCenter(GameObject go, float w, float h)
        {
            var rt = go.GetComponent<RectTransform>();
            rt.anchorMin = new Vector2(0.5f, 0.5f);
            rt.anchorMax = new Vector2(0.5f, 0.5f);
            rt.pivot = new Vector2(0.5f, 0.5f);
            rt.anchoredPosition = Vector2.zero;
            rt.sizeDelta = new Vector2(w, h);
        }

        static void SetStretch(GameObject go, float inset)
        {
            var rt = go.GetComponent<RectTransform>();
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.pivot = new Vector2(0.5f, 0.5f);
            rt.offsetMin = new Vector2(inset, inset);
            rt.offsetMax = new Vector2(-inset, -inset);
        }

        static void SetStretch(RectTransform rt, float inset)
        {
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.pivot = new Vector2(0.5f, 0.5f);
            rt.offsetMin = new Vector2(inset, inset);
            rt.offsetMax = new Vector2(-inset, -inset);
        }
    }
}
