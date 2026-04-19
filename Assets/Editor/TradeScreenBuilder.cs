using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;

namespace ProjectAstra.EditorTools
{
    // ===========================================================================================
    // Trade Screen — Indigo Codex variant (full-screen 1920×1080)
    //
    // Concept source: Claude Design export, "Variant 1 — INDIGO CODEX" unpacked at
    //   docs/mockups/Project Astra Trade Screen.unpacked/assets/e7379e7e-*.js
    // Figma source: Project Astra — Trade Screen (Indigo Codex) — file key in .secrets/figma_files.env
    // CSS source of truth for effect values: same .js file (inline JSX styles).
    //
    // Layout: two facing units with inventories, a central brass spine, top banner, bottom
    // action bar. All interactive states visible within the mockup per UI_WORKFLOW §4.6.
    // ===========================================================================================
    public static class TradeScreenBuilder
    {
        const float CanvasWidth  = 1920f;
        const float CanvasHeight = 1080f;
        const bool  IsFullScreen = true;
        const float Scale        = 1f; // no rescale — Figma authored at 1920×1080

        // ---- paths ----
        const string SpriteDir   = "Assets/UI/TradeScreen/Sprites/";
        const string IconDir     = "Assets/UI/TradeScreen/Icons/";
        const string FontDir     = "Assets/UI/TradeScreen/Fonts/";
        const string MaterialDir = "Assets/UI/TradeScreen/Materials/";

        // ---- Indigo Codex palette (from JSX `const CODEX = { ... }`) ----
        static readonly Color ColParchment   = HexA("f0e5c8", 1.00f);
        static readonly Color ColParchmentHi = HexA("fff5d8", 1.00f);
        static readonly Color ColInk         = HexA("1a1540", 1.00f);
        static readonly Color ColInkDeep     = HexA("0f0b2e", 1.00f);
        static readonly Color ColInkMid      = HexA("1f1a4a", 1.00f);
        static readonly Color ColBrass       = HexA("c9993a", 1.00f);
        static readonly Color ColBrassLite   = HexA("e8c66a", 1.00f);
        static readonly Color ColBrassGlow   = HexA("f5e0a0", 1.00f);
        static readonly Color ColVermillion  = HexA("b0382a", 1.00f);
        static readonly Color ColWine        = HexA("6b1e2e", 1.00f);
        static readonly Color ColDimOverlay  = new Color(0, 0, 0, 0.55f);

        // ---- fonts (loaded in Build()) ----
        static TMP_FontAsset cinzel, cormorant, cormorantItalic, ebGaramond, jetBrainsMono;

        // ---- sprites (loaded in Build()) ----
        static Sprite sprPanelLeft, sprPanelRight, sprSpine, sprActionBar;
        static Sprite sprSigilChakra, sprSigilTrishul, sprSigilLotus, sprSigilConch,
                      sprSigilFlame, sprSigilArrow, sprSigilGem, sprSigilScroll, sprSigilShield;
        static Sprite sprFiligreeCorner, sprLotusMedallion, sprPaisley,
                      sprSelectionDiamond, sprFocusCaret, sprPortraitPlaceholder;

        // ---- TMP glow materials (assigned in Build()) ----
        static Material matUnitNameGlow, matBannerGlow, matButtonHoverGlow, matHoldingGlow;

        // ==================================================================
        // entry point
        // ==================================================================

        [MenuItem("Project Astra/Build Trade Screen (temp)")]
        public static void Build()
        {
            if (IsFullScreen && (Mathf.Abs(CanvasWidth - 1920f) > 1f || Mathf.Abs(CanvasHeight - 1080f) > 1f))
                Debug.LogError("TradeScreen dims don't match Unity canvas reference (1920×1080). Fix constants.");

            var activeScene = EditorSceneManager.GetActiveScene();
            if (activeScene.name != "BattleMap")
                Debug.LogWarning($"Building TradeScreen into scene '{activeScene.name}' — expected BattleMap. Continuing.");

            cinzel          = LoadFont("Cinzel SDF.asset");
            cormorant       = LoadFont("CormorantGaramond SDF.asset");
            cormorantItalic = LoadFont("CormorantGaramondItalic SDF.asset");
            ebGaramond      = LoadFont("EBGaramond SDF.asset");
            jetBrainsMono   = LoadFont("JetBrainsMono SDF.asset");
            if (cinzel == null || cormorant == null)
                Debug.LogWarning("TMP font assets missing — run 'Project Astra/Generate TradeScreen Fonts' first.");

            sprPanelLeft        = LoadSprite("panel_chrome_left.png");
            sprPanelRight       = LoadSprite("panel_chrome_right.png");
            sprSpine            = LoadSprite("center_spine.png");
            sprActionBar        = LoadSprite("action_bar.png");

            sprSigilChakra      = LoadIcon("sigil_chakra.png");
            sprSigilTrishul     = LoadIcon("sigil_trishul.png");
            sprSigilLotus       = LoadIcon("sigil_lotus.png");
            sprSigilConch       = LoadIcon("sigil_conch.png");
            sprSigilFlame       = LoadIcon("sigil_flame.png");
            sprSigilArrow       = LoadIcon("sigil_arrow.png");
            sprSigilGem         = LoadIcon("sigil_gem.png");
            sprSigilScroll      = LoadIcon("sigil_scroll.png");
            sprSigilShield      = LoadIcon("sigil_shield.png");
            sprFiligreeCorner   = LoadIcon("filigree_corner.png");
            sprLotusMedallion   = LoadIcon("lotus_medallion.png");
            sprPaisley          = LoadIcon("paisley_motif.png");
            sprSelectionDiamond = LoadIcon("selection_diamond.png");
            sprFocusCaret       = LoadIcon("focus_caret.png");
            sprPortraitPlaceholder = LoadIcon("portrait_placeholder.png");

            matUnitNameGlow    = AssetDatabase.LoadAssetAtPath<Material>(TradeScreenMaterials.UnitNameGlow);
            matBannerGlow      = AssetDatabase.LoadAssetAtPath<Material>(TradeScreenMaterials.BannerGlow);
            matButtonHoverGlow = AssetDatabase.LoadAssetAtPath<Material>(TradeScreenMaterials.ButtonHoverGlow);
            matHoldingGlow     = AssetDatabase.LoadAssetAtPath<Material>(TradeScreenMaterials.HoldingGlow);

            var canvas = EnsureCanvas();

            var existingOverlay = canvas.transform.Find("TradeScreenDimOverlay");
            if (existingOverlay != null) Object.DestroyImmediate(existingOverlay.gameObject);
            var existing = canvas.transform.Find("TradeScreen");
            if (existing != null) Object.DestroyImmediate(existing.gameObject);

            BuildDimOverlay(canvas.transform);
            var screen = BuildScreen(canvas.transform);

            EditorSceneManager.MarkSceneDirty(activeScene);
            Selection.activeGameObject = screen;
            Debug.Log("TradeScreen (Indigo Codex) built.");
        }

        // ==================================================================
        // canvas + overlay
        // ==================================================================

        static Canvas EnsureCanvas()
        {
            var existing = Object.FindObjectOfType<Canvas>();
            if (existing != null && existing.renderMode == RenderMode.ScreenSpaceOverlay)
                return existing;

            var go = new GameObject("UICanvas",
                typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
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

        static GameObject BuildDimOverlay(Transform parent)
        {
            var go = NewImage("TradeScreenDimOverlay", parent, ColDimOverlay);
            SetCenter(go, CanvasWidth, CanvasHeight);
            go.GetComponent<Image>().raycastTarget = true;
            go.SetActive(false); // activated by the runtime controller when trade opens
            return go;
        }

        // ==================================================================
        // root screen frame
        // ==================================================================

        static GameObject BuildScreen(Transform parent)
        {
            // Root frame. No sprite on the frame itself; background is a child Image
            // so we can swap or dim it without touching siblings.
            var screen = NewRect("TradeScreen", parent).gameObject;
            var rt = screen.GetComponent<RectTransform>();
            rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 0.5f);
            rt.pivot = new Vector2(0.5f, 0.5f);
            rt.sizeDelta = new Vector2(CanvasWidth, CanvasHeight);
            rt.anchoredPosition = Vector2.zero;

            BuildBackground(rt);
            BuildTopBanner(rt);
            BuildPanel(rt, side: "left");
            BuildCenterSpine(rt);
            BuildPanel(rt, side: "right");
            BuildActionBar(rt);

            return screen;
        }

        // ==================================================================
        // background (dark indigo gradient + dot pattern)
        // ==================================================================

        static void BuildBackground(RectTransform parent)
        {
            // Base flat indigo — the gradient itself is subtle enough that a solid fill
            // reads correctly; richer gradient can be layered in later as a sprite if needed.
            var bg = NewImage("Background", parent, ColInkDeep);
            SetStretch(bg.GetComponent<RectTransform>(), 0);

            // Radial warm tint (top of screen). Approximated with a semi-transparent
            // indigo-mid Image anchored to top-center — the gradient fade is visual-only
            // and can be replaced by a proper radial sprite later.
            var warm = NewImage("WarmTopTint", parent, new Color(0x3c / 255f, 0x28 / 255f, 0x64 / 255f, 0.25f));
            var wrt = warm.GetComponent<RectTransform>();
            wrt.anchorMin = new Vector2(0.2f, 0.5f);
            wrt.anchorMax = new Vector2(0.8f, 1.0f);
            wrt.pivot = new Vector2(0.5f, 1.0f);
            wrt.offsetMin = Vector2.zero;
            wrt.offsetMax = Vector2.zero;
            warm.GetComponent<Image>().raycastTarget = false;
        }

        // ==================================================================
        // top banner — "PARLEY · EXCHANGE OF PROVISIONS" + paisley border
        // ==================================================================

        static void BuildTopBanner(RectTransform parent)
        {
            var banner = NewRect("TopBanner", parent);
            banner.anchorMin = new Vector2(0.5f, 1f);
            banner.anchorMax = new Vector2(0.5f, 1f);
            banner.pivot = new Vector2(0.5f, 1f);
            banner.anchoredPosition = new Vector2(0, -28);
            banner.sizeDelta = new Vector2(800, 56);

            var label = NewText("Label", banner, "PARLEY  ·  EXCHANGE OF PROVISIONS",
                cinzel, 13, ColBrassLite, TextAlignmentOptions.Center, FontStyles.Normal);
            label.characterSpacing = 10;
            if (matBannerGlow != null) label.fontMaterial = matBannerGlow;
            var lrt = label.rectTransform;
            lrt.anchorMin = new Vector2(0, 1);
            lrt.anchorMax = new Vector2(1, 1);
            lrt.pivot = new Vector2(0.5f, 1);
            lrt.anchoredPosition = new Vector2(0, 0);
            lrt.sizeDelta = new Vector2(0, 20);

            if (sprPaisley != null)
            {
                var paisley = NewImage("PaisleyBorder", banner, Color.white);
                var prt = paisley.GetComponent<RectTransform>();
                prt.anchorMin = new Vector2(0.5f, 0);
                prt.anchorMax = new Vector2(0.5f, 0);
                prt.pivot = new Vector2(0.5f, 0);
                prt.anchoredPosition = new Vector2(0, 8);
                prt.sizeDelta = new Vector2(520, 16);
                var img = paisley.GetComponent<Image>();
                img.sprite = sprPaisley;
                img.type = Image.Type.Tiled;
                img.color = new Color(1, 1, 1, 0.8f);
                img.raycastTarget = false;
            }
        }

        // ==================================================================
        // panel — left or right unit display
        // ==================================================================

        // Main content area: top:96, left:56, right:56, bottom:140 → 1808×844
        // Panel widths: (1808 - 80 spine) / 2 = 864
        const float ContentTop = 96f, ContentSide = 56f, ContentBottom = 140f;
        const float PanelW = 864f, PanelH = 844f, SpineW = 80f;

        static void BuildPanel(RectTransform parent, string side)
        {
            bool isLeft = side == "left";
            float xOffset = isLeft ? ContentSide : ContentSide + PanelW + SpineW;

            var panel = NewRect($"{side[0].ToString().ToUpper()}{side.Substring(1)}Panel", parent);
            panel.anchorMin = new Vector2(0, 1);
            panel.anchorMax = new Vector2(0, 1);
            panel.pivot = new Vector2(0, 1);
            panel.anchoredPosition = new Vector2(xOffset, -ContentTop);
            panel.sizeDelta = new Vector2(PanelW, PanelH);

            // Panel chrome
            var chrome = NewImage("Chrome", panel, Color.white);
            SetStretch(chrome.GetComponent<RectTransform>(), 0);
            var chromeImg = chrome.GetComponent<Image>();
            chromeImg.sprite = isLeft ? sprPanelLeft : sprPanelRight;
            chromeImg.type = Image.Type.Simple;
            chromeImg.raycastTarget = true;

            // Corner filigrees (4×) — rotated instances of the single filigree sprite
            if (sprFiligreeCorner != null)
            {
                AddCornerFiligree(panel, "TL",   0f,  10,  10, pivotTL: true);
                AddCornerFiligree(panel, "TR",  90f, -10,  10, pivotTL: false, flipH: true);
                AddCornerFiligree(panel, "BR", 180f, -10, -10, pivotTL: false, flipH: true, flipV: true);
                AddCornerFiligree(panel, "BL", 270f,  10, -10, pivotTL: true,  flipV: true);
            }

            // Content area inset 28px
            var content = NewRect("Content", panel);
            SetStretch(content, 28);

            // Header row: portrait + info block
            BuildPanelHeader(content, isLeft);

            // Inventory heading
            var invHeading = NewText("InventoryHeading", content,
                isLeft ? "◆  SATCHEL" : "SATCHEL  ◆",
                cinzel, 12, ColBrassLite,
                isLeft ? TextAlignmentOptions.Left : TextAlignmentOptions.Right,
                FontStyles.Normal);
            invHeading.characterSpacing = 5;
            var ihRt = invHeading.rectTransform;
            ihRt.anchorMin = new Vector2(0, 1);
            ihRt.anchorMax = new Vector2(1, 1);
            ihRt.pivot = new Vector2(0.5f, 1);
            ihRt.anchoredPosition = new Vector2(0, -(280 + 24));
            ihRt.sizeDelta = new Vector2(0, 20);

            // Bottom border line under heading
            var bar = NewImage("HeadingUnderline", content, new Color(ColBrass.r, ColBrass.g, ColBrass.b, 1f));
            var brt = bar.GetComponent<RectTransform>();
            brt.anchorMin = new Vector2(0, 1);
            brt.anchorMax = new Vector2(1, 1);
            brt.pivot = new Vector2(0.5f, 1);
            brt.anchoredPosition = new Vector2(0, -(280 + 24 + 26));
            brt.sizeDelta = new Vector2(0, 1);

            // Inventory rows (5 rows × 52 + 4 gap)
            BuildInventoryRows(content, isLeft);
        }

        static void AddCornerFiligree(RectTransform parent, string cornerId, float rotation,
            float x, float y, bool pivotTL, bool flipH = false, bool flipV = false)
        {
            var go = NewImage($"Filigree_{cornerId}", parent, new Color(1, 1, 1, 0.9f));
            var rt = go.GetComponent<RectTransform>();
            rt.anchorMin = new Vector2(pivotTL ? 0 : 1, y > 0 ? 1 : 0);
            rt.anchorMax = rt.anchorMin;
            rt.pivot = new Vector2(pivotTL ? 0 : 1, y > 0 ? 1 : 0);
            rt.anchoredPosition = new Vector2(x, y);
            rt.sizeDelta = new Vector2(48, 48);
            rt.localScale = new Vector3(flipH ? -1 : 1, flipV ? -1 : 1, 1);
            var img = go.GetComponent<Image>();
            img.sprite = sprFiligreeCorner;
            img.raycastTarget = false;
        }

        // ------------------------------------------------------------------
        // panel header (portrait + info)
        // ------------------------------------------------------------------

        static void BuildPanelHeader(RectTransform parent, bool isLeft)
        {
            float portraitW = 220, portraitH = 280, portraitGap = 24;

            // Portrait
            var portrait = NewImage("Portrait", parent, Color.white);
            var prt = portrait.GetComponent<RectTransform>();
            prt.anchorMin = new Vector2(isLeft ? 0 : 1, 1);
            prt.anchorMax = prt.anchorMin;
            prt.pivot = new Vector2(isLeft ? 0 : 1, 1);
            prt.anchoredPosition = new Vector2(isLeft ? 0 : 0, 0);
            prt.sizeDelta = new Vector2(portraitW, portraitH);
            var pimg = portrait.GetComponent<Image>();
            if (sprPortraitPlaceholder != null) pimg.sprite = sprPortraitPlaceholder;
            else pimg.color = ColWine;
            pimg.raycastTarget = false;

            // Portrait label (live — will be replaced with real bust art later)
            var pLabel = NewText("PortraitLabel", portrait.transform,
                isLeft ? "[ portrait: Arjuna ]" : "[ portrait: Bhima ]",
                jetBrainsMono, 10, new Color(ColParchment.r, ColParchment.g, ColParchment.b, 0.55f),
                TextAlignmentOptions.Left, FontStyles.Normal);
            var plRt = pLabel.rectTransform;
            plRt.anchorMin = new Vector2(0, 0);
            plRt.anchorMax = new Vector2(1, 0);
            plRt.pivot = new Vector2(0.5f, 0);
            plRt.anchoredPosition = new Vector2(0, 14);
            plRt.sizeDelta = new Vector2(-28, 14);

            // Info block — sits beside the portrait
            float infoW = PanelW - 28 * 2 - portraitW - portraitGap;
            var info = NewRect("InfoBlock", parent);
            info.anchorMin = new Vector2(isLeft ? 0 : 1, 1);
            info.anchorMax = info.anchorMin;
            info.pivot = new Vector2(isLeft ? 0 : 1, 1);
            info.anchoredPosition = new Vector2(isLeft ? portraitW + portraitGap : -(portraitW + portraitGap), 0);
            info.sizeDelta = new Vector2(infoW, portraitH);

            // Epithet (uppercase small caps)
            var epithet = NewText("Epithet", info,
                isLeft ? "PANDAVA PRINCE" : "WIND-BORN",
                cinzel, 14, ColBrassLite,
                isLeft ? TextAlignmentOptions.Left : TextAlignmentOptions.Right,
                FontStyles.Normal);
            epithet.characterSpacing = 4;
            var eRt = epithet.rectTransform;
            eRt.anchorMin = new Vector2(0, 1);
            eRt.anchorMax = new Vector2(1, 1);
            eRt.pivot = new Vector2(0.5f, 1);
            eRt.anchoredPosition = new Vector2(0, 0);
            eRt.sizeDelta = new Vector2(0, 20);

            // Unit name (italic, large)
            var nameT = NewText("UnitName", info, isLeft ? "Arjuna" : "Bhima",
                cormorantItalic, 56, ColParchment,
                isLeft ? TextAlignmentOptions.Left : TextAlignmentOptions.Right,
                FontStyles.Italic);
            if (matUnitNameGlow != null) nameT.fontMaterial = matUnitNameGlow;
            var nRt = nameT.rectTransform;
            nRt.anchorMin = new Vector2(0, 1);
            nRt.anchorMax = new Vector2(1, 1);
            nRt.pivot = new Vector2(0.5f, 1);
            nRt.anchoredPosition = new Vector2(0, -28);
            nRt.sizeDelta = new Vector2(0, 72);

            // Stats line — class / lv / carry
            var stats = NewText("StatsLine", info,
                isLeft ? "<color=#e8c66a>CLASS </color>Dhanurveda   <color=#e8c66a>LV </color>14   <color=#e8c66a>CARRY </color>4/5"
                       : "<color=#e8c66a>CLASS </color>Gada-dhara   <color=#e8c66a>LV </color>16   <color=#e8c66a>CARRY </color>5/5",
                cormorant, 16, new Color(ColParchment.r, ColParchment.g, ColParchment.b, 0.75f),
                isLeft ? TextAlignmentOptions.Left : TextAlignmentOptions.Right,
                FontStyles.Normal);
            stats.richText = true;
            var sRt = stats.rectTransform;
            sRt.anchorMin = new Vector2(0, 1);
            sRt.anchorMax = new Vector2(1, 1);
            sRt.pivot = new Vector2(0.5f, 1);
            sRt.anchoredPosition = new Vector2(0, -112);
            sRt.sizeDelta = new Vector2(0, 24);

            // Paisley divider
            if (sprPaisley != null)
            {
                var paisley = NewImage("PaisleyDivider", info, new Color(1, 1, 1, 0.7f));
                var prtD = paisley.GetComponent<RectTransform>();
                prtD.anchorMin = new Vector2(isLeft ? 0 : 1, 1);
                prtD.anchorMax = prtD.anchorMin;
                prtD.pivot = new Vector2(isLeft ? 0 : 1, 1);
                prtD.anchoredPosition = new Vector2(0, -148);
                prtD.sizeDelta = new Vector2(240, 14);
                var img = paisley.GetComponent<Image>();
                img.sprite = sprPaisley;
                img.type = Image.Type.Tiled;
                img.raycastTarget = false;
            }
        }

        // ------------------------------------------------------------------
        // inventory rows
        // ------------------------------------------------------------------

        // Data for each unit's visible state gallery — matches the Claude Design mockup.
        // Replace with live data feed once runtime controller is wired.
        static readonly ItemRow[] LeftItems =
        {
            new ItemRow("arrow",  "Celestial Arrow", 46, RowState.Default),
            new ItemRow("chakra", "Sudarshan Disk",  15, RowState.Hover),
            new ItemRow("scroll", "Vedic Scroll",     3, RowState.Selected),
            new ItemRow("gem",    "Kaustubha Gem",    1, RowState.Disabled),
            new ItemRow(null,     null,              -1, RowState.Empty),
        };
        static readonly ItemRow[] RightItems =
        {
            new ItemRow("trishul", "Trishul Haft", 30, RowState.Focused),
            new ItemRow("lotus",   "Lotus Balm",   20, RowState.Pressed),
            new ItemRow("flame",   "Agni Charm",    8, RowState.Default),
            new ItemRow("conch",   "Broken Conch",  2, RowState.Default),
            new ItemRow("shield",  "Iron Aegis",    1, RowState.Default),
        };

        static void BuildInventoryRows(RectTransform parent, bool isLeft)
        {
            var rowsRoot = NewRect("InventoryRows", parent);
            rowsRoot.anchorMin = new Vector2(0, 1);
            rowsRoot.anchorMax = new Vector2(1, 1);
            rowsRoot.pivot = new Vector2(0.5f, 1);
            rowsRoot.anchoredPosition = new Vector2(0, -(280 + 24 + 30));
            rowsRoot.sizeDelta = new Vector2(0, 52 * 5 + 4 * 4);

            var items = isLeft ? LeftItems : RightItems;
            for (int i = 0; i < items.Length; i++)
            {
                var row = BuildItemRow(rowsRoot, items[i], isLeft, index: i);
                var rrt = row.GetComponent<RectTransform>();
                rrt.anchorMin = new Vector2(0, 1);
                rrt.anchorMax = new Vector2(1, 1);
                rrt.pivot = new Vector2(0.5f, 1);
                rrt.anchoredPosition = new Vector2(0, -i * (52 + 4));
                rrt.sizeDelta = new Vector2(0, 52);
            }
        }

        static GameObject BuildItemRow(RectTransform parent, ItemRow item, bool isLeft, int index)
        {
            var row = NewRect($"Row_{index}_{item.State}", parent).gameObject;
            var rowRt = row.GetComponent<RectTransform>();
            rowRt.sizeDelta = new Vector2(0, 52);

            // Background — transparent by default, colored for hover/selected/focused/pressed
            var bg = NewImage("Background", row.transform, Color.clear);
            SetStretch(bg.GetComponent<RectTransform>(), 0);
            var bgImg = bg.GetComponent<Image>();
            bgImg.raycastTarget = true;

            ApplyRowStateVisuals(row.transform, bgImg, item.State, isLeft);

            // Empty row: just show the italic "— empty —" label
            if (item.State == RowState.Empty)
            {
                var emptyT = NewText("EmptyLabel", row.transform, "— empty —",
                    cormorantItalic, 18, new Color(ColParchment.r, ColParchment.g, ColParchment.b, 0.25f),
                    isLeft ? TextAlignmentOptions.Left : TextAlignmentOptions.Right,
                    FontStyles.Italic);
                var ert = emptyT.rectTransform;
                SetStretch(ert, 0);
                ert.offsetMin = new Vector2(14, 0);
                ert.offsetMax = new Vector2(-14, 0);
                // Dashed top border
                var dash = NewImage("DashedTop", row.transform, new Color(ColBrassLite.r, ColBrassLite.g, ColBrassLite.b, 0.2f));
                var drt = dash.GetComponent<RectTransform>();
                drt.anchorMin = new Vector2(0, 1); drt.anchorMax = new Vector2(1, 1);
                drt.pivot = new Vector2(0.5f, 1);
                drt.anchoredPosition = new Vector2(0, 0);
                drt.sizeDelta = new Vector2(0, 1);
                return row;
            }

            // Sigil icon
            var sigil = NewImage("Sigil", row.transform, Color.white);
            var srt = sigil.GetComponent<RectTransform>();
            srt.anchorMin = new Vector2(isLeft ? 0 : 1, 0.5f);
            srt.anchorMax = srt.anchorMin;
            srt.pivot = new Vector2(isLeft ? 0 : 1, 0.5f);
            srt.anchoredPosition = new Vector2(isLeft ? 14 : -14, 0);
            srt.sizeDelta = new Vector2(28, 28);
            var sigilImg = sigil.GetComponent<Image>();
            sigilImg.sprite = GetSigilSprite(item.Sigil);
            sigilImg.raycastTarget = false;
            sigilImg.color = item.State == RowState.Disabled
                ? new Color(ColBrass.r, ColBrass.g, ColBrass.b, 0.4f)
                : Color.white;

            // Item name
            Color textCol = item.State == RowState.Disabled
                ? new Color(ColParchment.r, ColParchment.g, ColParchment.b, 0.35f)
                : (item.State == RowState.Pressed || item.State == RowState.Selected
                    ? ColParchmentHi : ColParchment);

            var nameT = NewText("ItemName", row.transform, item.Name,
                cormorant, 22, textCol,
                isLeft ? TextAlignmentOptions.Left : TextAlignmentOptions.Right,
                FontStyles.Normal);
            if (item.State == RowState.Disabled) nameT.fontStyle |= FontStyles.Strikethrough;
            nameT.characterSpacing = 2;
            var nrt = nameT.rectTransform;
            nrt.anchorMin = new Vector2(isLeft ? 0 : 1, 0.5f);
            nrt.anchorMax = nrt.anchorMin;
            nrt.pivot = new Vector2(isLeft ? 0 : 1, 0.5f);
            nrt.anchoredPosition = new Vector2(isLeft ? 14 + 28 + 12 : -(14 + 56 + 12), 0);
            nrt.sizeDelta = new Vector2(400, 30);

            // Qty
            var qtyT = NewText("Qty", row.transform, item.Qty.ToString(),
                cinzel, 18, textCol,
                isLeft ? TextAlignmentOptions.Right : TextAlignmentOptions.Left,
                FontStyles.Bold);
            var qrt = qtyT.rectTransform;
            qrt.anchorMin = new Vector2(isLeft ? 1 : 0, 0.5f);
            qrt.anchorMax = qrt.anchorMin;
            qrt.pivot = new Vector2(isLeft ? 1 : 0, 0.5f);
            qrt.anchoredPosition = new Vector2(isLeft ? -14 : 14, 0);
            qrt.sizeDelta = new Vector2(56, 24);

            // Selected row gets a vermillion diamond marker
            if (item.State == RowState.Selected && sprSelectionDiamond != null)
            {
                var diamond = NewImage("SelectionDiamond", row.transform, Color.white);
                var drt = diamond.GetComponent<RectTransform>();
                drt.anchorMin = new Vector2(isLeft ? 0 : 1, 0.5f);
                drt.anchorMax = drt.anchorMin;
                drt.pivot = new Vector2(isLeft ? 1 : 0, 0.5f);
                drt.anchoredPosition = new Vector2(isLeft ? -8 : 8, 0);
                drt.sizeDelta = new Vector2(14, 14);
                diamond.GetComponent<Image>().sprite = sprSelectionDiamond;
            }

            // Focused row gets a glowing vertical caret
            if (item.State == RowState.Focused && sprFocusCaret != null)
            {
                var caret = NewImage("FocusCaret", row.transform, Color.white);
                var crt = caret.GetComponent<RectTransform>();
                crt.anchorMin = new Vector2(isLeft ? 0 : 1, 0);
                crt.anchorMax = new Vector2(isLeft ? 0 : 1, 1);
                crt.pivot = new Vector2(isLeft ? 0 : 1, 0.5f);
                crt.anchoredPosition = new Vector2(isLeft ? 2 : -4, 0);
                crt.offsetMin = new Vector2(crt.offsetMin.x, 8);
                crt.offsetMax = new Vector2(crt.offsetMax.x, -8);
                crt.sizeDelta = new Vector2(2, crt.sizeDelta.y);
                var img = caret.GetComponent<Image>();
                img.sprite = sprFocusCaret;
                img.preserveAspect = false;
            }

            return row;
        }

        static void ApplyRowStateVisuals(Transform row, Image bgImg, RowState state, bool isLeft)
        {
            switch (state)
            {
                case RowState.Hover:
                    bgImg.color = new Color(ColBrassLite.r, ColBrassLite.g, ColBrassLite.b, 0.10f);
                    AddRowBorder(row, new Color(ColBrassLite.r, ColBrassLite.g, ColBrassLite.b, 0.30f));
                    break;
                case RowState.Pressed:
                    bgImg.color = new Color(ColBrassLite.r, ColBrassLite.g, ColBrassLite.b, 0.22f);
                    AddRowBorder(row, ColBrassLite);
                    break;
                case RowState.Focused:
                    bgImg.color = new Color(ColBrassLite.r, ColBrassLite.g, ColBrassLite.b, 0.14f);
                    AddRowBorder(row, ColBrassLite);
                    break;
                case RowState.Selected:
                    bgImg.color = new Color(ColVermillion.r, ColVermillion.g, ColVermillion.b, 0.35f);
                    AddRowBorder(row, ColVermillion);
                    break;
                // Default, Disabled, Empty — transparent
            }
        }

        static void AddRowBorder(Transform row, Color color)
        {
            // Hollow 1px outline via 4 edge strips so the row's bg tint stays visible through the interior.
            AddBorderEdge(row, color, new Vector2(0, 1), new Vector2(1, 1), 0, -1, 0, 0); // top
            AddBorderEdge(row, color, new Vector2(0, 0), new Vector2(1, 0), 0, 0, 0, 1);  // bottom
            AddBorderEdge(row, color, new Vector2(0, 0), new Vector2(0, 1), 0, 0, 1, 0);  // left
            AddBorderEdge(row, color, new Vector2(1, 0), new Vector2(1, 1), -1, 0, 0, 0); // right
        }

        static void AddBorderEdge(Transform row, Color color, Vector2 anchorMin, Vector2 anchorMax,
            float offsetMinX, float offsetMinY, float offsetMaxX, float offsetMaxY)
        {
            var edge = NewImage("BorderEdge", row, color);
            var rt = edge.GetComponent<RectTransform>();
            rt.anchorMin = anchorMin;
            rt.anchorMax = anchorMax;
            rt.pivot = new Vector2(0.5f, 0.5f);
            rt.offsetMin = new Vector2(offsetMinX, offsetMinY);
            rt.offsetMax = new Vector2(offsetMaxX, offsetMaxY);
            edge.GetComponent<Image>().raycastTarget = false;
        }

        // ==================================================================
        // center spine — lotus medallions + diamond dividers
        // ==================================================================

        static void BuildCenterSpine(RectTransform parent)
        {
            var spine = NewRect("CenterSpine", parent);
            spine.anchorMin = new Vector2(0, 1);
            spine.anchorMax = new Vector2(0, 1);
            spine.pivot = new Vector2(0, 1);
            spine.anchoredPosition = new Vector2(ContentSide + PanelW, -ContentTop);
            spine.sizeDelta = new Vector2(SpineW, PanelH);

            // Chrome — baked gradient + brass stroke
            var chrome = NewImage("Chrome", spine, Color.white);
            SetStretch(chrome.GetComponent<RectTransform>(), 0);
            if (sprSpine != null) chrome.GetComponent<Image>().sprite = sprSpine;
            else chrome.GetComponent<Image>().color = ColInkDeep;

            // Top medallion
            if (sprLotusMedallion != null)
            {
                var topMed = NewImage("LotusTop", spine, Color.white);
                var tmRt = topMed.GetComponent<RectTransform>();
                tmRt.anchorMin = new Vector2(0.5f, 0.5f);
                tmRt.anchorMax = new Vector2(0.5f, 0.5f);
                tmRt.pivot = new Vector2(0.5f, 0.5f);
                tmRt.anchoredPosition = new Vector2(0, 84);
                tmRt.sizeDelta = new Vector2(72, 72);
                topMed.GetComponent<Image>().sprite = sprLotusMedallion;
                topMed.GetComponent<Image>().raycastTarget = false;
            }

            // Three diamond separators in the center
            for (int i = 0; i < 3; i++)
            {
                var d = NewImage($"Diamond_{i}", spine, ColBrass);
                var drt = d.GetComponent<RectTransform>();
                drt.anchorMin = new Vector2(0.5f, 0.5f);
                drt.anchorMax = new Vector2(0.5f, 0.5f);
                drt.pivot = new Vector2(0.5f, 0.5f);
                drt.anchoredPosition = new Vector2(0, (i - 1) * 14);
                drt.sizeDelta = new Vector2(6, 6);
                drt.localRotation = Quaternion.Euler(0, 0, 45);
                d.GetComponent<Image>().raycastTarget = false;
            }

            // Bottom medallion
            if (sprLotusMedallion != null)
            {
                var botMed = NewImage("LotusBottom", spine, Color.white);
                var bmRt = botMed.GetComponent<RectTransform>();
                bmRt.anchorMin = new Vector2(0.5f, 0.5f);
                bmRt.anchorMax = new Vector2(0.5f, 0.5f);
                bmRt.pivot = new Vector2(0.5f, 0.5f);
                bmRt.anchoredPosition = new Vector2(0, -84);
                bmRt.sizeDelta = new Vector2(72, 72);
                botMed.GetComponent<Image>().sprite = sprLotusMedallion;
                botMed.GetComponent<Image>().raycastTarget = false;
            }
        }

        // ==================================================================
        // bottom action bar — holding readout + button row
        // ==================================================================

        static void BuildActionBar(RectTransform parent)
        {
            var bar = NewRect("ActionBar", parent);
            bar.anchorMin = new Vector2(0, 0);
            bar.anchorMax = new Vector2(1, 0);
            bar.pivot = new Vector2(0.5f, 0);
            bar.anchoredPosition = new Vector2(0, 24);
            bar.offsetMin = new Vector2(56, 24);
            bar.offsetMax = new Vector2(-56, 24 + 72);
            bar.sizeDelta = new Vector2(bar.sizeDelta.x, 72);

            // Chrome
            var chrome = NewImage("Chrome", bar, Color.white);
            SetStretch(chrome.GetComponent<RectTransform>(), 0);
            if (sprActionBar != null) chrome.GetComponent<Image>().sprite = sprActionBar;
            else chrome.GetComponent<Image>().color = ColInk;

            // Left side — holding readout
            var holding = NewRect("HoldingReadout", bar);
            holding.anchorMin = new Vector2(0, 0.5f);
            holding.anchorMax = new Vector2(0, 0.5f);
            holding.pivot = new Vector2(0, 0.5f);
            holding.anchoredPosition = new Vector2(32, 0);
            holding.sizeDelta = new Vector2(520, 40);

            if (sprSelectionDiamond != null)
            {
                var diamond = NewImage("HoldDiamond", holding, Color.white);
                var drt = diamond.GetComponent<RectTransform>();
                drt.anchorMin = new Vector2(0, 0.5f);
                drt.anchorMax = new Vector2(0, 0.5f);
                drt.pivot = new Vector2(0, 0.5f);
                drt.anchoredPosition = new Vector2(0, 0);
                drt.sizeDelta = new Vector2(14, 14);
                diamond.GetComponent<Image>().sprite = sprSelectionDiamond;
            }

            var holdLabel = NewText("HoldingLabel", holding, "HOLDING",
                cinzel, 11, ColBrassLite, TextAlignmentOptions.Left, FontStyles.Normal);
            holdLabel.characterSpacing = 3;
            var hlRt = holdLabel.rectTransform;
            hlRt.anchorMin = new Vector2(0, 0.5f);
            hlRt.anchorMax = new Vector2(0, 0.5f);
            hlRt.pivot = new Vector2(0, 0.5f);
            hlRt.anchoredPosition = new Vector2(28, 0);
            hlRt.sizeDelta = new Vector2(70, 14);

            var holdValue = NewText("HoldingValue", holding, "Sudarshan Disk  ·  15 uses",
                cormorantItalic, 24, ColParchment, TextAlignmentOptions.Left, FontStyles.Italic);
            if (matHoldingGlow != null) holdValue.fontMaterial = matHoldingGlow;
            var hvRt = holdValue.rectTransform;
            hvRt.anchorMin = new Vector2(0, 0.5f);
            hvRt.anchorMax = new Vector2(0, 0.5f);
            hvRt.pivot = new Vector2(0, 0.5f);
            hvRt.anchoredPosition = new Vector2(110, 0);
            hvRt.sizeDelta = new Vector2(400, 30);

            // Right side — 6 buttons
            var buttons = new[]
            {
                ("Move",     "A", ButtonState.Default,  false),
                ("Swap",     "S", ButtonState.Hover,    false),
                ("Place",    "D", ButtonState.Pressed,  false),
                ("Inspect",  "I", ButtonState.Focused,  false),
                ("Gift",     "G", ButtonState.Disabled, false),
                ("Conclude", "B", ButtonState.Default,  true),
            };
            const float btnW = 140, btnH = 44, btnGap = 12;
            float totalW = btnW * buttons.Length + btnGap * (buttons.Length - 1);
            for (int i = 0; i < buttons.Length; i++)
            {
                var b = buttons[i];
                var btn = BuildButton(bar, b.Item1, b.Item2, b.Item3, b.Item4);
                var brt = btn.GetComponent<RectTransform>();
                brt.anchorMin = new Vector2(1, 0.5f);
                brt.anchorMax = new Vector2(1, 0.5f);
                brt.pivot = new Vector2(1, 0.5f);
                float rightEdgeOffset = -(32 + (buttons.Length - 1 - i) * (btnW + btnGap));
                brt.anchoredPosition = new Vector2(rightEdgeOffset, 0);
                brt.sizeDelta = new Vector2(btnW, btnH);
            }
        }

        // ==================================================================
        // buttons (shortcut badge + label) with state visuals
        // ==================================================================

        static GameObject BuildButton(RectTransform parent, string label, string shortcut,
            ButtonState state, bool vermillion)
        {
            var btn = NewRect($"Btn_{label}_{state}{(vermillion ? "_vermillion" : "")}", parent).gameObject;
            var brt = btn.GetComponent<RectTransform>();
            brt.sizeDelta = new Vector2(140, 44);

            var style = GetButtonStyle(state, vermillion);

            // Background
            var bg = NewImage("Background", btn.transform, style.Bg);
            SetStretch(bg.GetComponent<RectTransform>(), 0);

            // Border (1.5px simulated via outline image)
            var border = NewImage("Border", btn.transform, style.Border);
            SetStretch(border.GetComponent<RectTransform>(), 0);
            var inner = NewImage("InnerFill", border.transform, style.Bg);
            SetStretch(inner.GetComponent<RectTransform>(), 2);
            border.GetComponent<Image>().raycastTarget = false;
            inner.GetComponent<Image>().raycastTarget = false;

            // Shortcut circle badge
            var badge = NewImage("Badge", btn.transform, Color.clear);
            var bdRt = badge.GetComponent<RectTransform>();
            bdRt.anchorMin = new Vector2(0, 0.5f);
            bdRt.anchorMax = new Vector2(0, 0.5f);
            bdRt.pivot = new Vector2(0, 0.5f);
            bdRt.anchoredPosition = new Vector2(14, 0);
            bdRt.sizeDelta = new Vector2(18, 18);
            // Circle-ish: Unity's default UI Image has no circle, so we draw an outlined sprite
            // by stacking two solid circles — but for this pass we use a text-only badge.
            var bChar = NewText("BadgeChar", badge.transform, shortcut,
                cinzel, 10, style.Border, TextAlignmentOptions.Center, FontStyles.Bold);
            bChar.characterSpacing = 1;
            SetStretch(bChar.rectTransform, 0);

            // Label
            var lbl = NewText("Label", btn.transform, label,
                cormorant, 20, style.Text, TextAlignmentOptions.Left, FontStyles.Normal);
            lbl.characterSpacing = 1;
            if (state == ButtonState.Hover && matButtonHoverGlow != null) lbl.fontMaterial = matButtonHoverGlow;
            var lRt = lbl.rectTransform;
            lRt.anchorMin = new Vector2(0, 0);
            lRt.anchorMax = new Vector2(1, 1);
            lRt.pivot = new Vector2(0.5f, 0.5f);
            lRt.offsetMin = new Vector2(44, 0);
            lRt.offsetMax = new Vector2(-14, 0);

            // Focused glow — outer ring
            if (state == ButtonState.Focused)
            {
                var glow = NewImage("FocusGlow", btn.transform,
                    new Color(ColBrassGlow.r, ColBrassGlow.g, ColBrassGlow.b, 0.35f));
                var gRt = glow.GetComponent<RectTransform>();
                SetStretch(gRt, -3);
                glow.transform.SetAsFirstSibling();
                glow.GetComponent<Image>().raycastTarget = false;
            }

            // State label (dev-only annotation, top-right)
            var stateTag = NewText("StateTag", btn.transform, state.ToString().ToUpper(),
                jetBrainsMono, 8, new Color(ColBrass.r, ColBrass.g, ColBrass.b, 0.6f),
                TextAlignmentOptions.Right, FontStyles.Normal);
            stateTag.characterSpacing = 1;
            var stRt = stateTag.rectTransform;
            stRt.anchorMin = new Vector2(1, 1);
            stRt.anchorMax = new Vector2(1, 1);
            stRt.pivot = new Vector2(1, 1);
            stRt.anchoredPosition = new Vector2(-2, 4);
            stRt.sizeDelta = new Vector2(80, 10);

            return btn;
        }

        struct ButtonStyle { public Color Bg, Border, Text; }

        static ButtonStyle GetButtonStyle(ButtonState state, bool vermillion)
        {
            switch (state)
            {
                case ButtonState.Hover:
                    return new ButtonStyle
                    {
                        Bg = vermillion ? A(ColVermillion, 0.6f) : A(ColBrassLite, 0.15f),
                        Border = ColBrassLite,
                        Text = ColParchmentHi,
                    };
                case ButtonState.Pressed:
                    return new ButtonStyle
                    {
                        Bg = vermillion ? A(HexA("781e14", 1), 0.9f) : A(ColBrassLite, 0.30f),
                        Border = ColBrassLite,
                        Text = ColParchmentHi,
                    };
                case ButtonState.Focused:
                    return new ButtonStyle
                    {
                        Bg = vermillion ? A(ColVermillion, 0.45f) : A(ColInk, 0.7f),
                        Border = ColBrassGlow,
                        Text = ColParchment,
                    };
                case ButtonState.Disabled:
                    return new ButtonStyle
                    {
                        Bg = A(ColInk, 0.3f),
                        Border = A(ColBrass, 0.3f),
                        Text = A(ColParchment, 0.3f),
                    };
                default:
                    return new ButtonStyle
                    {
                        Bg = vermillion ? A(ColVermillion, 0.4f) : A(ColInk, 0.6f),
                        Border = ColBrass,
                        Text = ColParchment,
                    };
            }
        }

        // ==================================================================
        // types
        // ==================================================================

        enum RowState { Default, Hover, Pressed, Focused, Selected, Disabled, Empty }
        enum ButtonState { Default, Hover, Pressed, Focused, Disabled }

        readonly struct ItemRow
        {
            public readonly string Sigil;
            public readonly string Name;
            public readonly int Qty;
            public readonly RowState State;
            public ItemRow(string sigil, string name, int qty, RowState state)
            { Sigil = sigil; Name = name; Qty = qty; State = state; }
        }

        // ==================================================================
        // helpers
        // ==================================================================

        static Sprite GetSigilSprite(string kind)
        {
            switch (kind)
            {
                case "arrow":   return sprSigilArrow;
                case "chakra":  return sprSigilChakra;
                case "trishul": return sprSigilTrishul;
                case "lotus":   return sprSigilLotus;
                case "conch":   return sprSigilConch;
                case "flame":   return sprSigilFlame;
                case "gem":     return sprSigilGem;
                case "scroll":  return sprSigilScroll;
                case "shield":  return sprSigilShield;
                default:        return null;
            }
        }

        static TMP_FontAsset LoadFont(string file)
            => AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(FontDir + file);

        static Sprite LoadSprite(string file)
            => AssetDatabase.LoadAssetAtPath<Sprite>(SpriteDir + file);

        static Sprite LoadIcon(string file)
            => AssetDatabase.LoadAssetAtPath<Sprite>(IconDir + file);

        static GameObject NewImage(string name, Transform parent, Color color)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
            go.transform.SetParent(parent, false);
            go.GetComponent<Image>().color = color;
            return go;
        }

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
            rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 0.5f);
            rt.pivot = new Vector2(0.5f, 0.5f);
            rt.anchoredPosition = Vector2.zero;
            rt.sizeDelta = new Vector2(w, h);
        }

        static void SetStretch(RectTransform rt, float inset)
        {
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.pivot = new Vector2(0.5f, 0.5f);
            rt.offsetMin = new Vector2(inset, inset);
            rt.offsetMax = new Vector2(-inset, -inset);
        }

        static Color HexA(string hex, float a)
        {
            byte r = byte.Parse(hex.Substring(0, 2), System.Globalization.NumberStyles.HexNumber);
            byte g = byte.Parse(hex.Substring(2, 2), System.Globalization.NumberStyles.HexNumber);
            byte b = byte.Parse(hex.Substring(4, 2), System.Globalization.NumberStyles.HexNumber);
            return new Color(r / 255f, g / 255f, b / 255f, a);
        }

        static Color A(Color c, float a) => new Color(c.r, c.g, c.b, a);
    }
}
