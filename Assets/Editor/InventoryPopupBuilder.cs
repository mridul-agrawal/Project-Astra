using System.IO;
using ProjectAstra.Core.UI;
using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

namespace ProjectAstra.EditorTools
{
    // ==========================================================================================
    // InventoryPopup (Indigo Codex) — modal popup built as a prefab at
    //   Assets/UI/InventoryPopup/InventoryPopup.prefab
    //
    // Concept source: docs/mockups/Indigo Codex Inventory.unpacked/assets/*.js (unpacked Claude Design bundle).
    // Figma source:   vMS0l2pBV167WrJ8wRFz0X  (key in .secrets/figma_files.env as INVENTORY_POPUP_FIGMA).
    //
    // Layout — Figma frame 4:2 Canvas 1920×1080 → Modal 4:4 at (160,140) 1600×800 → three panels inside.
    // The prefab root covers the whole canvas (so DimBackdrop can sit over the map), with the
    // Modal child centered. InventoryPopupRefs is attached and wired so InventoryMenuUI can
    // drive live elements without Find() spelunking.
    // ==========================================================================================
    public static class InventoryPopupBuilder
    {
        const float CanvasWidth  = 1920f;
        const float CanvasHeight = 1080f;
        const bool  IsFullScreen = false;   // modal popup
        const float ModalWidth   = 1600f;
        const float ModalHeight  = 800f;
        const float Scale        = 1f;

        const string SpriteDir   = "Assets/UI/InventoryPopup/Sprites/";
        const string IconDir     = "Assets/UI/InventoryPopup/Icons/";
        const string FontDir     = "Assets/UI/TradeScreen/Fonts/";    // reused — no duplication
        const string MaterialDir = "Assets/UI/InventoryPopup/Materials/";
        const string PrefabPath  = "Assets/UI/InventoryPopup/InventoryPopup.prefab";

        // ---- Indigo Codex palette (from mockup JSX `CODEX`) ----
        static readonly Color ColParchment     = HexA("f2e6c4", 1.00f);
        static readonly Color ColParchmentSel  = HexA("fff5d8", 1.00f);
        static readonly Color ColParchDim      = HexA("c9b98a", 1.00f);
        static readonly Color ColInk           = HexA("1a140f", 1.00f);
        static readonly Color ColInkDeep       = HexA("3d2a1a", 1.00f);
        static readonly Color ColBrass         = HexA("c9993a", 1.00f);
        static readonly Color ColBrassLite     = HexA("e8c66a", 1.00f);
        static readonly Color ColBrassGlow     = HexA("fde49a", 1.00f);
        static readonly Color ColVermillion    = HexA("b0382a", 1.00f);
        static readonly Color ColIndigoHi      = HexA("1a1540", 1.00f);
        static readonly Color ColIndigo        = HexA("0f0b2e", 1.00f);
        static readonly Color ColIndigoLo      = HexA("08061c", 1.00f);
        static readonly Color ColDimBackdrop   = new Color(0f, 0f, 0f, 0.55f);

        // ---- loaded resources (set in Build()) ----
        static TMP_FontAsset cinzel, cormorant, cormorantItalic, ebGaramond, jetBrainsMono;
        static Sprite sprPortraitPanel, sprInventoryPanel, sprStatsPanel, sprPortraitArt,
                      sprStatPill, sprProvFooter,
                      sprRowDefault, sprRowSelected, sprRowEmpty, sprRowDepleted;
        static Sprite sprSigilSword, sprSigilLance, sprSigilAxe, sprSigilBow, sprSigilStaff, sprSigilConsumable;
        static Sprite sprKirtimukha, sprCornerFiligree, sprButiBand, sprSelectionCaret;
        static Material matStatValueGlow, matItemNameUnderlay, matBrassLabelGlow, matHeaderGlyphGlow;

        static InventoryPopupRefs refs;

        [MenuItem("Project Astra/Build Inventory Popup (prefab)")]
        public static void BuildPrefab()
        {
            if (IsFullScreen && (Mathf.Abs(ModalWidth - CanvasWidth) > 1f || Mathf.Abs(ModalHeight - CanvasHeight) > 1f))
                Debug.LogError("IsFullScreen=true but Modal dims != Canvas dims. Fix constants.");

            LoadResources();

            var root = BuildHierarchy();
            if (root == null) return;

            if (!Directory.Exists(Path.GetDirectoryName(PrefabPath)))
                Directory.CreateDirectory(Path.GetDirectoryName(PrefabPath));

            PrefabUtility.SaveAsPrefabAsset(root, PrefabPath, out bool ok);
            Object.DestroyImmediate(root);
            if (ok) Debug.Log($"InventoryPopup prefab saved to {PrefabPath}");
            else    Debug.LogError("InventoryPopup prefab save failed.");

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
        }

        // ==================================================================
        // resource loading
        // ==================================================================

        static void LoadResources()
        {
            cinzel          = LoadFont("Cinzel SDF.asset");
            cormorant       = LoadFont("CormorantGaramond SDF.asset");
            cormorantItalic = LoadFont("CormorantGaramondItalic SDF.asset");
            ebGaramond      = LoadFont("EBGaramond SDF.asset");
            jetBrainsMono   = LoadFont("JetBrainsMono SDF.asset");
            if (cinzel == null || cormorant == null || ebGaramond == null)
                Debug.LogWarning("TMP font assets missing — run 'Project Astra/Generate TradeScreen Fonts' first.");

            sprPortraitPanel   = LoadSprite("portrait_panel_bg.png");
            sprInventoryPanel  = LoadSprite("inventory_panel_bg.png");
            sprStatsPanel      = LoadSprite("stats_panel_bg.png");
            sprPortraitArt     = LoadSprite("portrait_art_bg.png");
            sprStatPill        = LoadSprite("stat_pill_bg.png");
            sprProvFooter      = LoadSprite("provenance_footer_bg.png");
            sprRowDefault      = LoadSprite("row_default.png");
            sprRowSelected     = LoadSprite("row_selected.png");
            sprRowEmpty        = LoadSprite("row_empty.png");
            sprRowDepleted     = LoadSprite("row_depleted.png");

            sprSigilSword       = LoadIcon("sigil_sword.png");
            sprSigilLance       = LoadIcon("sigil_lance.png");
            sprSigilAxe         = LoadIcon("sigil_axe.png");
            sprSigilBow         = LoadIcon("sigil_bow.png");
            sprSigilStaff       = LoadIcon("sigil_staff.png");
            sprSigilConsumable  = LoadIcon("sigil_consumable.png");
            sprKirtimukha       = LoadIcon("kirtimukha.png");
            sprCornerFiligree   = LoadIcon("corner_filigree.png");
            sprButiBand         = LoadIcon("butiband.png");
            sprSelectionCaret   = LoadIcon("selection_caret.png");

            matStatValueGlow    = AssetDatabase.LoadAssetAtPath<Material>(InventoryPopupMaterials.StatValueGlow);
            matItemNameUnderlay = AssetDatabase.LoadAssetAtPath<Material>(InventoryPopupMaterials.ItemNameUnderlay);
            matBrassLabelGlow   = AssetDatabase.LoadAssetAtPath<Material>(InventoryPopupMaterials.BrassLabelGlow);
            matHeaderGlyphGlow  = AssetDatabase.LoadAssetAtPath<Material>(InventoryPopupMaterials.HeaderGlyphGlow);
            if (matStatValueGlow == null)
                Debug.LogWarning("Glow materials missing — run 'Project Astra/Generate InventoryPopup Glow Materials' first.");
        }

        // ==================================================================
        // root hierarchy
        // ==================================================================

        static GameObject BuildHierarchy()
        {
            // Root — stretches full canvas so DimBackdrop can fill. Anchored stretch (0,0)→(1,1).
            var root = new GameObject("InventoryPopup", typeof(RectTransform));
            var rt = root.GetComponent<RectTransform>();
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.pivot = new Vector2(0.5f, 0.5f);
            rt.offsetMin = Vector2.zero;
            rt.offsetMax = Vector2.zero;
            rt.sizeDelta = Vector2.zero;

            refs = root.AddComponent<InventoryPopupRefs>();

            BuildDimBackdrop(rt);
            var modal = BuildModal(rt);
            BuildHeader(modal);
            var body = BuildBody(modal);
            BuildPortraitPanel(body);
            BuildInventoryPanel(body);
            BuildStatsPanel(body);

            // Populate ref-holder sigil lookup table
            refs.sigilSword      = sprSigilSword;
            refs.sigilLance      = sprSigilLance;
            refs.sigilAxe        = sprSigilAxe;
            refs.sigilBow        = sprSigilBow;
            refs.sigilStaff      = sprSigilStaff;
            refs.sigilConsumable = sprSigilConsumable;

            return root;
        }

        static void BuildDimBackdrop(RectTransform parent)
        {
            var go = NewImage("DimBackdrop", parent, ColDimBackdrop);
            SetStretch(go.GetComponent<RectTransform>(), 0);
            go.GetComponent<Image>().raycastTarget = true; // swallow clicks on map
        }

        static RectTransform BuildModal(RectTransform parent)
        {
            var modal = NewRect("Modal", parent);
            modal.anchorMin = modal.anchorMax = new Vector2(0.5f, 0.5f);
            modal.pivot = new Vector2(0.5f, 0.5f);
            modal.anchoredPosition = Vector2.zero;
            modal.sizeDelta = new Vector2(ModalWidth, ModalHeight);
            return modal;
        }

        // ==================================================================
        // header bar — cartouche title + kirtimukha + rules + butiband
        // ==================================================================

        static void BuildHeader(RectTransform modal)
        {
            // 4:5 HeaderBar 1600×60 at (0,0) of modal (top-left anchored)
            var header = NewRect("HeaderBar", modal);
            SetTopLeftBox(header, 0, 0, 1600, 60);

            // 4:7 RuleLeft 560×1 at x=80 y=30   (1px brass hairline)
            var ruleLeft = NewImage("RuleLeft", header, A(ColBrass, 0.6f));
            SetTopLeftBox(ruleLeft.GetComponent<RectTransform>(), 80, 30, 560, 1);
            ruleLeft.GetComponent<Image>().raycastTarget = false;

            // 4:8 RuleRight 560×1 at x=960 y=30
            var ruleRight = NewImage("RuleRight", header, A(ColBrass, 0.6f));
            SetTopLeftBox(ruleRight.GetComponent<RectTransform>(), 960, 30, 560, 1);
            ruleRight.GetComponent<Image>().raycastTarget = false;

            // 4:9 HeaderLabel — "Satchel · Śastrāgāra" centered between the kirtimukhas
            var headerLabel = NewText("HeaderLabel", header, "SATCHEL  ·  ŚASTRĀGĀRA",
                cinzel, 16, ColBrassGlow, TextAlignmentOptions.Center, FontStyles.Normal);
            headerLabel.characterSpacing = 12;
            if (matHeaderGlyphGlow != null) headerLabel.fontMaterial = matHeaderGlyphGlow;
            SetTopLeftBox(headerLabel.rectTransform, 540, 19, 520, 22);
            refs.headerLabel = headerLabel;

            // 12:22 KirtimukhaLeft 32×32 at x=750 y=14
            var kl = NewImage("KirtimukhaLeft", header, Color.white);
            SetTopLeftBox(kl.GetComponent<RectTransform>(), 750, 14, 32, 32);
            var klImg = kl.GetComponent<Image>();
            klImg.sprite = sprKirtimukha;
            klImg.preserveAspect = true;
            klImg.raycastTarget = false;

            // 12:33 KirtimukhaRight 32×32 at x=818 y=14 (mirrored)
            var kr = NewImage("KirtimukhaRight", header, Color.white);
            SetTopLeftBox(kr.GetComponent<RectTransform>(), 818, 14, 32, 32);
            var krImg = kr.GetComponent<Image>();
            krImg.sprite = sprKirtimukha;
            krImg.preserveAspect = true;
            krImg.raycastTarget = false;
            kr.GetComponent<RectTransform>().localScale = new Vector3(-1, 1, 1); // mirror

            // 12:44 ButiBand 240×14 at x=680 y=46
            var bb = NewImage("ButiBand", header, A(Color.white, 0.85f));
            SetTopLeftBox(bb.GetComponent<RectTransform>(), 680, 46, 240, 14);
            var bbImg = bb.GetComponent<Image>();
            bbImg.sprite = sprButiBand;
            bbImg.type = Image.Type.Tiled;
            bbImg.raycastTarget = false;
        }

        // ==================================================================
        // body container
        // ==================================================================

        static RectTransform BuildBody(RectTransform modal)
        {
            // 7:2 Body 1600×700 at y=80 (below the 60-tall header + 20px gap)
            var body = NewRect("Body", modal);
            SetTopLeftBox(body, 0, 80, 1600, 700);
            return body;
        }

        // ==================================================================
        // portrait panel — left column, 386×700
        // ==================================================================

        static void BuildPortraitPanel(RectTransform body)
        {
            var panel = NewRect("PortraitPanel", body);
            SetTopLeftBox(panel, 0, 0, 386, 700);

            var bg = NewImage("Background", panel, Color.white);
            SetStretch(bg.GetComponent<RectTransform>(), 0);
            bg.GetComponent<Image>().sprite = sprPortraitPanel;
            bg.GetComponent<Image>().raycastTarget = true;

            AddCornerFiligrees(panel);

            // 8:2 PortraitArt 322×322 at (32,32)
            var art = NewRect("PortraitArt", panel);
            SetTopLeftBox(art, 32, 32, 322, 322);
            var artBg = NewImage("Background", art, Color.white);
            SetStretch(artBg.GetComponent<RectTransform>(), 0);
            artBg.GetComponent<Image>().sprite = sprPortraitArt;

            var placeholder = NewText("Placeholder", art, "[ UNIT PORTRAIT ]",
                cinzel, 14, A(ColParchment, 0.35f), TextAlignmentOptions.Center, FontStyles.Normal);
            placeholder.characterSpacing = 4;
            SetTopLeftBox(placeholder.rectTransform, 0, 147, 322, 28);
            refs.portraitPlaceholder = placeholder;

            // 8:5 UnitName at y=376  (Cormorant 600 42)
            var unitName = NewText("UnitName", panel, "Arjuna",
                cormorant, 36, ColParchmentSel, TextAlignmentOptions.Center, FontStyles.Bold);
            SetTopLeftBox(unitName.rectTransform, 32, 376, 322, 42);
            refs.unitName = unitName;

            // 8:6 UnitClass at y=426  (Cinzel 14 brassLite, spaced)
            var unitClass = NewText("UnitClass", panel, "SENĀPATI · CELESTIAL ARCHER",
                cinzel, 12, ColBrassLite, TextAlignmentOptions.Center, FontStyles.Normal);
            unitClass.characterSpacing = 8;
            SetTopLeftBox(unitClass.rectTransform, 32, 426, 322, 16);
            refs.unitClass = unitClass;

            // 8:7 HpLabel at (32,462) 100×14 (static "HP")
            var hpLabel = NewText("HpLabel", panel, "HP",
                cinzel, 11, ColBrassLite, TextAlignmentOptions.Left, FontStyles.Normal);
            hpLabel.characterSpacing = 5;
            SetTopLeftBox(hpLabel.rectTransform, 32, 462, 100, 14);

            // 8:8 HpNumbers at (132,462) 222×14 (e.g. "28 / 34")
            var hpNumbers = NewText("HpNumbers", panel, "— / —",
                jetBrainsMono != null ? jetBrainsMono : cinzel, 13, ColParchment,
                TextAlignmentOptions.Right, FontStyles.Normal);
            SetTopLeftBox(hpNumbers.rectTransform, 132, 462, 222, 14);
            refs.hpNumbers = hpNumbers;

            // 8:9 HpTrack 322×8 at (32,484)
            var track = NewImage("HpTrack", panel, A(Color.black, 0.5f));
            SetTopLeftBox(track.GetComponent<RectTransform>(), 32, 484, 322, 8);
            track.GetComponent<Image>().raycastTarget = false;

            // 8:10 HpFill — fills left-to-right; runtime sets width
            var fill = NewImage("HpFill", panel, ColBrassLite);
            var fillRt = fill.GetComponent<RectTransform>();
            fillRt.anchorMin = fillRt.anchorMax = new Vector2(0, 1);
            fillRt.pivot = new Vector2(0, 1);
            fillRt.anchoredPosition = new Vector2(33, -485);
            fillRt.sizeDelta = new Vector2(288, 6);
            fill.GetComponent<Image>().raycastTarget = false;
            refs.hpFill = fill.GetComponent<Image>();
        }

        // ==================================================================
        // inventory panel — middle column, 669×700, 5 rows
        // ==================================================================

        static void BuildInventoryPanel(RectTransform body)
        {
            var panel = NewRect("InventoryPanel", body);
            SetTopLeftBox(panel, 408, 0, 669, 700);

            var bg = NewImage("Background", panel, Color.white);
            SetStretch(bg.GetComponent<RectTransform>(), 0);
            bg.GetComponent<Image>().sprite = sprInventoryPanel;

            AddCornerFiligrees(panel);

            // 9:2 InventoryHeader at (32,32) 605×42
            var header = NewRect("InventoryHeader", panel);
            SetTopLeftBox(header, 32, 32, 605, 42);

            var title = NewText("InventoryTitle", header, "SATCHEL",
                cinzel, 13, ColBrassLite, TextAlignmentOptions.Left, FontStyles.Normal);
            title.characterSpacing = 10;
            if (matBrassLabelGlow != null) title.fontMaterial = matBrassLabelGlow;
            SetTopLeftBox(title.rectTransform, 0, 6, 320, 18);
            refs.inventoryTitle = title;

            var count = NewText("InventoryCount", header, "0 / 5",
                jetBrainsMono != null ? jetBrainsMono : cinzel, 14, ColParchDim,
                TextAlignmentOptions.Right, FontStyles.Normal);
            SetTopLeftBox(count.rectTransform, 485, 8, 120, 16);
            refs.inventoryCount = count;

            var divider = NewImage("HeaderDivider", header, A(ColBrass, 0.8f));
            SetTopLeftBox(divider.GetComponent<RectTransform>(), 0, 32, 605, 1);
            divider.GetComponent<Image>().raycastTarget = false;

            // 5 rows at y = 82, 144, 206, 268, 330 (each 605×58)
            refs.rows = new InventoryPopupRefs.RowRefs[5];
            for (int i = 0; i < 5; i++)
            {
                refs.rows[i] = BuildItemRow(panel, i);
            }

            // 9:68 ActionHintsFooter at (32,628) 605×40
            var footer = NewRect("ActionHintsFooter", panel);
            SetTopLeftBox(footer, 32, 628, 605, 40);

            var hintsDivider = NewImage("HintsDivider", footer, A(ColBrass, 0.4f));
            SetTopLeftBox(hintsDivider.GetComponent<RectTransform>(), 0, 0, 605, 1);
            hintsDivider.GetComponent<Image>().raycastTarget = false;

            var hintsText = NewText("HintsText", footer,
                "▲▼  CHOOSE      ⏎  SELECT      ⎋  BACK",
                cinzel, 10, ColParchDim, TextAlignmentOptions.Center, FontStyles.Normal);
            hintsText.characterSpacing = 6;
            SetTopLeftBox(hintsText.rectTransform, 0, 15, 605, 14);
            refs.hintsText = hintsText;
        }

        static InventoryPopupRefs.RowRefs BuildItemRow(RectTransform panel, int i)
        {
            // Figma rows sit 32px from panel left, stacked 58px tall starting at y=82.
            float y = 82 + i * 62f;   // slightly more generous than Figma's 62 step to match visuals

            // Row frame is 605 wide to match Figma's sample row (9:6 width=605).
            var row = NewRect($"Row_{i}", panel);
            SetTopLeftBox(row, 32, y, 605, 58);

            var bg = NewImage("Background", row, Color.white);
            SetStretch(bg.GetComponent<RectTransform>(), 0);
            var bgImg = bg.GetComponent<Image>();
            bgImg.sprite = sprRowDefault;
            bgImg.type = Image.Type.Simple;
            bgImg.raycastTarget = true;
            bgImg.preserveAspect = false;

            // SelectionCaret — anchored to left edge of row, protrudes outward by 16px
            var caret = NewImage("SelectionCaret", row, Color.white);
            var caretRt = caret.GetComponent<RectTransform>();
            caretRt.anchorMin = caretRt.anchorMax = new Vector2(0, 0.5f);
            caretRt.pivot = new Vector2(1f, 0.5f);
            caretRt.anchoredPosition = new Vector2(-4, 0);
            caretRt.sizeDelta = new Vector2(12, 14);
            caret.GetComponent<Image>().sprite = sprSelectionCaret;
            caret.GetComponent<Image>().preserveAspect = true;
            caret.GetComponent<Image>().raycastTarget = false;
            caret.SetActive(false);

            // Sigil 34×34 at (16,12)
            var sigil = NewImage("Sigil", row, ColBrassLite);
            SetTopLeftBox(sigil.GetComponent<RectTransform>(), 16, 12, 34, 34);
            sigil.GetComponent<Image>().preserveAspect = true;
            sigil.GetComponent<Image>().raycastTarget = false;

            // Item name — Cormorant 22 at (62,8) 240×28
            var nameT = NewText("ItemName", row, "",
                cormorant, 22, ColParchment, TextAlignmentOptions.MidlineLeft, FontStyles.Bold);
            SetTopLeftBox(nameT.rectTransform, 62, 8, 380, 28);

            // Kind / subtitle — Cormorant italic 14 at (62,34) 340×16
            var kindT = NewText("ItemKind", row, "",
                cormorantItalic != null ? cormorantItalic : cormorant, 14, ColParchDim,
                TextAlignmentOptions.MidlineLeft, FontStyles.Italic);
            SetTopLeftBox(kindT.rectTransform, 62, 34, 400, 16);

            // Uses — right side, monospace "35 / 45" at (497,12) 80×18
            var usesT = NewText("Uses", row, "",
                jetBrainsMono != null ? jetBrainsMono : cinzel, 14, ColParchment,
                TextAlignmentOptions.Center, FontStyles.Normal);
            SetTopLeftBox(usesT.rectTransform, 497, 12, 80, 18);

            // Durability track
            var track = NewImage("DurabilityTrack", row, A(Color.black, 0.5f));
            var trackRt = track.GetComponent<RectTransform>();
            SetTopLeftBox(trackRt, 497, 38, 80, 4);
            track.GetComponent<Image>().raycastTarget = false;

            // Durability fill (width is runtime-set; pivot left so Width controls fill fraction)
            var fill = NewImage("DurabilityFill", row, ColBrass);
            var fillRt = fill.GetComponent<RectTransform>();
            fillRt.anchorMin = fillRt.anchorMax = new Vector2(0, 1);
            fillRt.pivot = new Vector2(0, 1);
            fillRt.anchoredPosition = new Vector2(497, -38.5f);
            fillRt.sizeDelta = new Vector2(80, 3);
            fill.GetComponent<Image>().raycastTarget = false;

            return new InventoryPopupRefs.RowRefs {
                root = row.gameObject,
                background = bgImg,
                sigil = sigil.GetComponent<Image>(),
                nameText = nameT,
                kindText = kindT,
                usesText = usesT,
                durabilityTrack = trackRt,
                durabilityFill = fill.GetComponent<Image>(),
                selectionCaret = caret.GetComponent<Image>(),
                sprDefault  = sprRowDefault,
                sprSelected = sprRowSelected,
                sprEmpty    = sprRowEmpty,
                sprDepleted = sprRowDepleted,
            };
        }

        // ==================================================================
        // stats panel — right column, 501×700
        // ==================================================================

        static void BuildStatsPanel(RectTransform body)
        {
            var panel = NewRect("StatsPanel", body);
            SetTopLeftBox(panel, 1099, 0, 501, 700);

            var bg = NewImage("Background", panel, Color.white);
            SetStretch(bg.GetComponent<RectTransform>(), 0);
            bg.GetComponent<Image>().sprite = sprStatsPanel;

            AddCornerFiligrees(panel);

            var cg = panel.gameObject.AddComponent<CanvasGroup>();
            refs.statsGroup = cg;

            // 10:2 SelectedItemName at (32,32) 437×44 (Cormorant 600 40)
            var selName = NewText("SelectedItemName", panel, "— select an item —",
                cormorant, 34, ColParchmentSel, TextAlignmentOptions.Left, FontStyles.Bold);
            SetTopLeftBox(selName.rectTransform, 32, 32, 437, 44);
            refs.selectedItemName = selName;

            // 10:3 SelectedItemKind at (32,82) 437×16  (Cinzel 11 brassLite spaced)
            var selKind = NewText("SelectedItemKind", panel, "",
                cinzel, 11, ColBrassLite, TextAlignmentOptions.Left, FontStyles.Normal);
            selKind.characterSpacing = 5;
            if (matBrassLabelGlow != null) selKind.fontMaterial = matBrassLabelGlow;
            SetTopLeftBox(selKind.rectTransform, 32, 82, 437, 16);
            refs.selectedItemKind = selKind;

            // 10:4 TitleUnderline at (32,106) 437×8 — two hairlines + a rotated diamond
            var underline = NewRect("TitleUnderline", panel);
            SetTopLeftBox(underline, 32, 106, 437, 8);
            var ulLeft = NewImage("Rule", underline, A(ColBrass, 0.6f));
            SetTopLeftBox(ulLeft.GetComponent<RectTransform>(), 0, 4, 208, 1);
            var ulRight = NewImage("Rule", underline, A(ColBrass, 0.6f));
            SetTopLeftBox(ulRight.GetComponent<RectTransform>(), 228, 4, 209, 1);
            var diamond = NewImage("Diamond", underline, ColBrassLite);
            var dRt = diamond.GetComponent<RectTransform>();
            SetTopLeftBox(dRt, 214, 1, 8, 8);
            dRt.localRotation = Quaternion.Euler(0, 0, 45);

            // 10:8 StatPill_ATK at (32,132) 211.5×118
            refs.statAtk = BuildStatPill(panel, "StatPill_ATK", 32, 132, "ATK");
            refs.statHit = BuildStatPill(panel, "StatPill_HIT", 257.5f, 132, "HIT");
            refs.statRng = BuildStatPill(panel, "StatPill_RNG", 32, 264, "RNG");
            refs.statWt  = BuildStatPill(panel, "StatPill_WT",  257.5f, 264, "WT");

            // 10:40 ItemDescription at (32,400) 437×80
            var desc = NewText("ItemDescription", panel, "",
                ebGaramond != null ? ebGaramond : cormorant, 16, ColParchment,
                TextAlignmentOptions.TopLeft, FontStyles.Normal);
            desc.enableWordWrapping = true;
            SetTopLeftBox(desc.rectTransform, 32, 400, 437, 180);
            refs.itemDescription = desc;

            // 10:41 ProvenanceFooter at (32,626) 437×32
            var prov = NewRect("ProvenanceFooter", panel);
            SetTopLeftBox(prov, 32, 626, 437, 32);
            var provBg = NewImage("Background", prov, Color.white);
            SetStretch(provBg.GetComponent<RectTransform>(), 0);
            provBg.GetComponent<Image>().sprite = sprProvFooter;

            var bullet = NewImage("ProvBullet", prov, ColBrassLite);
            var bRt = bullet.GetComponent<RectTransform>();
            SetTopLeftBox(bRt, 14, 13, 8, 8);
            bRt.localRotation = Quaternion.Euler(0, 0, 45);

            var provText = NewText("ProvText", prov, "",
                cormorantItalic != null ? cormorantItalic : cormorant, 13, ColBrassLite,
                TextAlignmentOptions.MidlineLeft, FontStyles.Italic);
            SetTopLeftBox(provText.rectTransform, 30, 7, 397, 18);
            refs.provText = provText;
        }

        static TextMeshProUGUI BuildStatPill(RectTransform parent, string name, float x, float y, string label)
        {
            var pill = NewRect(name, parent);
            SetTopLeftBox(pill, x, y, 211.5f, 118);

            var bg = NewImage("Background", pill, Color.white);
            SetStretch(bg.GetComponent<RectTransform>(), 0);
            bg.GetComponent<Image>().sprite = sprStatPill;

            var lbl = NewText("Label", pill, label,
                cinzel, 11, ColBrassLite, TextAlignmentOptions.Center, FontStyles.Normal);
            lbl.characterSpacing = 5;
            if (matBrassLabelGlow != null) lbl.fontMaterial = matBrassLabelGlow;
            SetTopLeftBox(lbl.rectTransform, 0, 14, 211.5f, 16);

            var val = NewText("Value", pill, "—",
                cormorant, 56, ColParchmentSel, TextAlignmentOptions.Center, FontStyles.Bold);
            if (matStatValueGlow != null) val.fontMaterial = matStatValueGlow;
            SetTopLeftBox(val.rectTransform, 0, 38, 211.5f, 70);

            var underline = NewImage("PillUnderline", pill, ColBrassLite);
            SetTopLeftBox(underline.GetComponent<RectTransform>(), 75.75f, 104, 60, 2);
            underline.GetComponent<Image>().raycastTarget = false;

            return val;
        }

        // ==================================================================
        // corner filigrees — 4 copies of the same sprite, rotated per corner
        // ==================================================================

        static void AddCornerFiligrees(RectTransform panel)
        {
            AddFiligree(panel, "CornerFiligree_TL", 4,               4,                0);
            AddFiligree(panel, "CornerFiligree_TR", panel.rect.width - 52, 4,           90);
            AddFiligree(panel, "CornerFiligree_BR", panel.rect.width - 52, panel.rect.height - 52, 180);
            AddFiligree(panel, "CornerFiligree_BL", 4,               panel.rect.height - 52,     270);
        }

        static void AddFiligree(RectTransform panel, string name, float x, float y, float rotation)
        {
            var go = NewImage(name, panel, ColBrassLite);
            SetTopLeftBox(go.GetComponent<RectTransform>(), x, y, 48, 48);
            go.GetComponent<Image>().sprite = sprCornerFiligree;
            go.GetComponent<Image>().preserveAspect = true;
            go.GetComponent<Image>().raycastTarget = false;
            go.transform.localRotation = Quaternion.Euler(0, 0, rotation);
        }

        // ==================================================================
        // helpers
        // ==================================================================

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
            t.raycastTarget = false;
            return t;
        }

        static TextMeshProUGUI NewText(string name, RectTransform parent, string text,
            TMP_FontAsset font, float size, Color color,
            TextAlignmentOptions align, FontStyles style)
            => NewText(name, (Transform)parent, text, font, size, color, align, style);

        // Top-left origin placement — Figma coordinates are (x,y) from top-left, size (w,h).
        static void SetTopLeftBox(RectTransform rt, float x, float y, float w, float h)
        {
            rt.anchorMin = rt.anchorMax = new Vector2(0, 1);
            rt.pivot = new Vector2(0, 1);
            rt.anchoredPosition = new Vector2(x, -y);
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
