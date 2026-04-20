using System.IO;
using ProjectAstra.Core.UI;
using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

namespace ProjectAstra.EditorTools
{
    // ==========================================================================================
    // SupplyConvoy (Indigo Codex · Variant A) — full-screen 1920×1080
    // Concept source: docs/mockups/Supply Convoy Mockup.html
    // Figma source:   QWo6piYh7ArnjiEieXmNUO (SUPPLY_CONVOY_FIGMA in .secrets/figma_files.env)
    //
    // Layout (from the mockup CSS):
    //   Cartouche     980×72    centered top (y=20)
    //   LeftColumn    620×900   at (40, 120)   — PortraitPanel, Bubble, Submenu, LordInv
    //   RightColumn  1180×900   at (700, 120)  — ConvoyHead, Tabstrip, ConvoyList, Scrollrail
    //   FooterHints  1920×58    at y=1022
    //
    // The builder produces Assets/UI/SupplyConvoy/SupplyConvoy.prefab and wires every live
    // element into SupplyConvoyRefs so ConvoyUI doesn't need to walk the hierarchy by name.
    // ==========================================================================================
    public static class SupplyConvoyBuilder
    {
        const float CanvasWidth  = 1920f;
        const float CanvasHeight = 1080f;

        const string SpriteDir    = "Assets/UI/SupplyConvoy/Sprites/";
        const string MaterialDir  = "Assets/UI/SupplyConvoy/Materials/";
        const string FontDir      = "Assets/UI/TradeScreen/Fonts/";
        const string IconDir      = "Assets/UI/InventoryPopup/Icons/";  // reuse sigils
        const string PrefabPath   = "Assets/UI/SupplyConvoy/SupplyConvoy.prefab";

        // Indigo Codex palette (mockup CSS verbatim)
        static readonly Color ColParchment    = Hex("f2e6c4");
        static readonly Color ColParchmentSel = Hex("fff5d8");
        static readonly Color ColParchDim     = Hex("c9b98a");
        static readonly Color ColInk          = Hex("1a140f");
        static readonly Color ColInkDeep      = Hex("3d2a1a");
        static readonly Color ColBrass        = Hex("c9993a");
        static readonly Color ColBrassLite    = Hex("e8c66a");
        static readonly Color ColBrassGlow    = Hex("fde49a");
        static readonly Color ColVermillion   = Hex("b0382a");
        static readonly Color ColWine         = Hex("6b1e2e");
        static readonly Color ColIndigoHi     = Hex("1a1540");
        static readonly Color ColIndigo       = Hex("0f0b2e");
        static readonly Color ColDimBackdrop  = new Color(0, 0, 0, 0.55f);

        static TMP_FontAsset cinzel, cormorant, ebGaramond, jetBrainsMono;
        static SupplyConvoyRefs refs;

        // Chrome sprites
        static Sprite spCartouche, spPortraitPanel, spPortraitSlab, spBubble,
                      spGiveBtn, spTakeBtn, spLordInv, spConvoyHead, spTabstrip,
                      spConvoyList, spScrollrail, spScrollThumb, spScrollCapTop,
                      spScrollCapBottom, spFooterHints;

        // State sprites
        static Sprite spRowDefault, spRowHover, spRowFocused, spRowDepleted, spRowDisabled;
        static Sprite spTabDefault, spTabHover, spTabFocused, spTabActive;
        static Sprite spSubmenuDefault, spSubmenuHover, spSubmenuPressed, spSubmenuActive;
        static Sprite spSlotDefault, spSlotFocused, spSlotEquipped, spSlotDepleted, spSlotEmpty;

        // Reused sigils
        static Sprite spSigSword, spSigLance, spSigAxe, spSigBow, spSigStaff, spSigConsumable;

        // Glow materials
        static Material matCartoucheTitleGlow, matStockNumGlow, matFocusedNameGlow, matBrassLabelGlow;

        [MenuItem("Project Astra/Build Supply Convoy (prefab)")]
        public static void BuildPrefab()
        {
            LoadResources();

            var root = BuildHierarchy();
            if (root == null) return;

            Directory.CreateDirectory(Path.GetDirectoryName(PrefabPath));
            PrefabUtility.SaveAsPrefabAsset(root, PrefabPath, out bool ok);
            Object.DestroyImmediate(root);
            if (ok) Debug.Log($"SupplyConvoy prefab saved to {PrefabPath}");
            else    Debug.LogError("SupplyConvoy prefab save failed.");

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
        }

        // ==================================================================
        // resource loading
        // ==================================================================

        static void LoadResources()
        {
            cinzel        = LoadFont("Cinzel SDF.asset");
            cormorant     = LoadFont("CormorantGaramond SDF.asset");
            ebGaramond    = LoadFont("EBGaramond SDF.asset");
            jetBrainsMono = LoadFont("JetBrainsMono SDF.asset");

            spCartouche       = LoadSprite("cartouche_bg.png");
            spPortraitPanel   = LoadSprite("portrait_panel_bg.png");
            spPortraitSlab    = LoadSprite("portrait_slab_bg.png");
            spBubble          = LoadSprite("bubble_bg.png");
            spGiveBtn         = LoadSprite("give_btn_bg.png");
            spTakeBtn         = LoadSprite("take_btn_bg.png");
            spLordInv         = LoadSprite("lord_inv_bg.png");
            spConvoyHead      = LoadSprite("convoy_head_bg.png");
            spTabstrip        = LoadSprite("tabstrip_bg.png");
            spConvoyList      = LoadSprite("convoy_list_bg.png");
            spScrollrail      = LoadSprite("scrollrail_bg.png");
            spScrollThumb     = LoadSprite("scrollrail_thumb.png");
            spScrollCapTop    = LoadSprite("scrollrail_cap_top.png");
            spScrollCapBottom = LoadSprite("scrollrail_cap_bottom.png");
            spFooterHints     = LoadSprite("footer_hints_bg.png");

            spRowDefault      = LoadSprite("row_default.png");
            spRowHover        = LoadSprite("row_hover.png");
            spRowFocused      = LoadSprite("row_focused.png");
            spRowDepleted     = LoadSprite("row_depleted.png");
            spRowDisabled     = LoadSprite("row_disabled.png");

            spTabDefault      = LoadSprite("tab_default.png");
            spTabHover        = LoadSprite("tab_hover.png");
            spTabFocused      = LoadSprite("tab_focused.png");
            spTabActive       = LoadSprite("tab_active.png");

            spSubmenuDefault  = LoadSprite("submenu_default.png");
            spSubmenuHover    = LoadSprite("submenu_hover.png");
            spSubmenuPressed  = LoadSprite("submenu_pressed.png");
            spSubmenuActive   = LoadSprite("submenu_active.png");

            spSlotDefault     = LoadSprite("slot_default.png");
            spSlotFocused     = LoadSprite("slot_focused.png");
            spSlotEquipped    = LoadSprite("slot_equipped.png");
            spSlotDepleted    = LoadSprite("slot_depleted.png");
            spSlotEmpty       = LoadSprite("slot_empty.png");

            spSigSword        = LoadIcon("sigil_sword.png");
            spSigLance        = LoadIcon("sigil_lance.png");
            spSigAxe          = LoadIcon("sigil_axe.png");
            spSigBow          = LoadIcon("sigil_bow.png");
            spSigStaff        = LoadIcon("sigil_staff.png");
            spSigConsumable   = LoadIcon("sigil_consumable.png");

            matCartoucheTitleGlow = AssetDatabase.LoadAssetAtPath<Material>(SupplyConvoyMaterials.CartoucheTitleGlow);
            matStockNumGlow       = AssetDatabase.LoadAssetAtPath<Material>(SupplyConvoyMaterials.StockNumGlow);
            matFocusedNameGlow    = AssetDatabase.LoadAssetAtPath<Material>(SupplyConvoyMaterials.FocusedNameGlow);
            matBrassLabelGlow     = AssetDatabase.LoadAssetAtPath<Material>(SupplyConvoyMaterials.BrassLabelGlow);

            if (spCartouche == null)
                Debug.LogWarning("SupplyConvoy sprites missing — run download_supply_convoy_assets.sh first.");
            if (matCartoucheTitleGlow == null)
                Debug.LogWarning("SupplyConvoy glow materials missing — run 'Project Astra/Generate SupplyConvoy Glow Materials' first.");
        }

        // ==================================================================
        // hierarchy
        // ==================================================================

        static GameObject BuildHierarchy()
        {
            var root = new GameObject("SupplyConvoy", typeof(RectTransform));
            var rt = root.GetComponent<RectTransform>();
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.pivot = new Vector2(0.5f, 0.5f);
            rt.offsetMin = rt.offsetMax = Vector2.zero;

            refs = root.AddComponent<SupplyConvoyRefs>();

            BuildDimBackdrop(rt);
            BuildCartouche(rt);
            BuildLeftColumn(rt);
            BuildRightColumn(rt);
            BuildFooter(rt);

            // Populate sigil lookup
            refs.sigilSword      = spSigSword;
            refs.sigilLance      = spSigLance;
            refs.sigilAxe        = spSigAxe;
            refs.sigilBow        = spSigBow;
            refs.sigilStaff      = spSigStaff;
            refs.sigilConsumable = spSigConsumable;

            // Populate state sprite tables on refs (for tabs, slots, etc. that live there)
            refs.submenuDefault = spSubmenuDefault;
            refs.submenuHover   = spSubmenuHover;
            refs.submenuPressed = spSubmenuPressed;
            refs.submenuActive  = spSubmenuActive;

            return root;
        }

        static void BuildDimBackdrop(RectTransform parent)
        {
            var go = NewImage("DimBackdrop", parent, ColDimBackdrop);
            SetStretch(go.GetComponent<RectTransform>(), 0);
            go.GetComponent<Image>().raycastTarget = true;
        }

        // ==================================================================
        // Cartouche
        // ==================================================================

        static void BuildCartouche(RectTransform root)
        {
            var cart = NewRect("Cartouche", root);
            cart.anchorMin = cart.anchorMax = new Vector2(0.5f, 1f);
            cart.pivot = new Vector2(0.5f, 1f);
            cart.anchoredPosition = new Vector2(0, -20);
            cart.sizeDelta = new Vector2(980, 72);

            var bg = NewImage("Background", cart, Color.white);
            SetStretch(bg.GetComponent<RectTransform>(), 0);
            bg.GetComponent<Image>().sprite = spCartouche;

            var title = NewText("Title", cart, "QUARTERMASTER  ·  RASADĀRA",
                cinzel, 22, ColBrassGlow, TextAlignmentOptions.Center, FontStyles.Bold);
            title.characterSpacing = 42;
            if (matCartoucheTitleGlow != null) title.fontMaterial = matCartoucheTitleGlow;
            SetTopLeftBox(title.rectTransform, 0, 18, 980, 26);
            refs.cartoucheTitle = title;

            var sub = NewText("Subtitle", cart, "rasa-dāra · keeper of the stores",
                ebGaramond, 15, ColParchDim, TextAlignmentOptions.Center, FontStyles.Italic);
            SetTopLeftBox(sub.rectTransform, 0, 46, 980, 20);
            refs.cartoucheSub = sub;
        }

        // ==================================================================
        // Left column
        // ==================================================================

        static void BuildLeftColumn(RectTransform root)
        {
            var col = NewRect("LeftColumn", root);
            col.anchorMin = col.anchorMax = new Vector2(0, 1);
            col.pivot = new Vector2(0, 1);
            col.anchoredPosition = new Vector2(40, -120);
            col.sizeDelta = new Vector2(620, 900);

            BuildPortraitPanel(col);
            BuildBubble(col);
            BuildSubmenu(col);
            BuildLordInv(col);
        }

        static void BuildPortraitPanel(RectTransform parent)
        {
            var pp = NewRect("PortraitPanel", parent);
            SetTopLeftBox(pp, 0, 0, 620, 440);
            var bg = NewImage("Background", pp, Color.white);
            SetStretch(bg.GetComponent<RectTransform>(), 0);
            bg.GetComponent<Image>().sprite = spPortraitPanel;

            var slab = NewRect("Slab", pp);
            SetTopLeftBox(slab, 18, 18, 584, 404);
            var slabBg = NewImage("Background", slab, Color.white);
            SetStretch(slabBg.GetComponent<RectTransform>(), 0);
            slabBg.GetComponent<Image>().sprite = spPortraitSlab;

            var label = NewText("PortraitLabel", pp, "PORTRAIT · BUST ART PENDING",
                cinzel, 11, ColBrassLite, TextAlignmentOptions.Left, FontStyles.Bold);
            label.characterSpacing = 32;
            SetTopLeftBox(label.rectTransform, 24, 400, 320, 16);
            refs.portraitLabel = label;

            var name = NewText("PortraitName", pp, "—",
                cormorant, 28, ColParchment, TextAlignmentOptions.Right, FontStyles.Bold);
            SetTopLeftBox(name.rectTransform, 620 - 24 - 280, 398, 280, 30);
            refs.portraitName = name;
        }

        static void BuildBubble(RectTransform parent)
        {
            var bubble = NewRect("Bubble", parent);
            SetTopLeftBox(bubble, 0, 458, 620, 110);
            var bg = NewImage("Background", bubble, Color.white);
            SetStretch(bg.GetComponent<RectTransform>(), 0);
            bg.GetComponent<Image>().sprite = spBubble;

            var speaker = NewText("Speaker", bubble, "ARJUN · LORD",
                cinzel, 10, ColVermillion, TextAlignmentOptions.Left, FontStyles.Bold);
            speaker.characterSpacing = 30;
            SetTopLeftBox(speaker.rectTransform, 22, 14, 400, 14);
            refs.bubbleSpeaker = speaker;

            var line = NewText("BubbleLine", bubble, "\u201CWhat\u2019ll you do?\u201D",
                ebGaramond, 22, ColInkDeep, TextAlignmentOptions.Left, FontStyles.Italic);
            SetTopLeftBox(line.rectTransform, 22, 34, 576, 60);
            refs.bubbleLine = line;
        }

        static void BuildSubmenu(RectTransform parent)
        {
            var sm = NewRect("Submenu", parent);
            SetTopLeftBox(sm, 0, 586, 620, 52);

            refs.giveButtonBg = BuildSubmenuButton(sm, "GiveButton", 0, "GIVE", out refs.giveLabel);
            refs.takeButtonBg = BuildSubmenuButton(sm, "TakeButton", 316, "TAKE", out refs.takeLabel);
        }

        static Image BuildSubmenuButton(RectTransform parent, string name, float x, string label,
            out TextMeshProUGUI outLabel)
        {
            var btn = NewRect(name, parent);
            SetTopLeftBox(btn, x, 0, 304, 52);
            var bg = NewImage("Background", btn, Color.white);
            SetStretch(bg.GetComponent<RectTransform>(), 0);
            var bgImg = bg.GetComponent<Image>();
            bgImg.sprite = spSubmenuDefault;
            bgImg.raycastTarget = true;

            // Gem diamond at x=70 (rotated 45)
            var gem = NewImage("Gem", btn, ColBrass);
            var gemRt = gem.GetComponent<RectTransform>();
            gemRt.anchorMin = gemRt.anchorMax = new Vector2(0, 1);
            gemRt.pivot = new Vector2(0.5f, 0.5f);
            gemRt.anchoredPosition = new Vector2(74, -26);
            gemRt.sizeDelta = new Vector2(8, 8);
            gem.transform.localRotation = Quaternion.Euler(0, 0, 45);

            var lbl = NewText("Label", btn, label,
                cinzel, 15, ColParchment, TextAlignmentOptions.Left, FontStyles.Bold);
            lbl.characterSpacing = 35;
            SetTopLeftBox(lbl.rectTransform, 86, 16, 200, 20);
            outLabel = lbl;
            return bgImg;
        }

        static void BuildLordInv(RectTransform parent)
        {
            var li = NewRect("LordInv", parent);
            SetTopLeftBox(li, 0, 656, 620, 244);
            var bg = NewImage("Background", li, Color.white);
            SetStretch(bg.GetComponent<RectTransform>(), 0);
            bg.GetComponent<Image>().sprite = spLordInv;

            var header = NewText("Header", li, "SATCHEL · LORD",
                cinzel, 13, ColBrassLite, TextAlignmentOptions.Left, FontStyles.Bold);
            header.characterSpacing = 34;
            if (matBrassLabelGlow != null) header.fontMaterial = matBrassLabelGlow;
            SetTopLeftBox(header.rectTransform, 20, 16, 400, 14);
            refs.lordInvHeader = header;

            var cap = NewText("Cap", li, "0 / 5",
                jetBrainsMono, 14, ColParchment, TextAlignmentOptions.Right, FontStyles.Normal);
            SetTopLeftBox(cap.rectTransform, 620 - 20 - 80, 16, 80, 14);
            refs.lordInvCap = cap;

            refs.slots = new SupplyConvoyRefs.SlotRefs[5];
            for (int i = 0; i < 5; i++)
                refs.slots[i] = BuildSlot(li, i);
        }

        static SupplyConvoyRefs.SlotRefs BuildSlot(RectTransform parent, int i)
        {
            float y = 42 + i * 40;
            var slot = NewRect($"Slot_{i}", parent);
            SetTopLeftBox(slot, 14, y, 592, 38);

            var bg = NewImage("Background", slot, Color.white);
            SetStretch(bg.GetComponent<RectTransform>(), 0);
            var bgImg = bg.GetComponent<Image>();
            bgImg.sprite = spSlotDefault;

            var sigil = NewImage("Sigil", slot, ColBrassLite);
            var sigilRt = sigil.GetComponent<RectTransform>();
            sigilRt.anchorMin = sigilRt.anchorMax = new Vector2(0, 1);
            sigilRt.pivot = new Vector2(0, 1);
            sigilRt.anchoredPosition = new Vector2(14, -10);
            sigilRt.sizeDelta = new Vector2(22, 22);
            sigil.GetComponent<Image>().preserveAspect = true;

            var name = NewText("Name", slot, "",
                cormorant, 21, ColParchment, TextAlignmentOptions.MidlineLeft, FontStyles.Bold);
            SetTopLeftBox(name.rectTransform, 52, 6, 280, 26);

            var rank = NewText("Rank", slot, "",
                cinzel, 9, ColParchDim, TextAlignmentOptions.MidlineRight, FontStyles.Normal);
            rank.characterSpacing = 24;
            SetTopLeftBox(rank.rectTransform, 360, 10, 120, 16);

            var uses = NewText("Uses", slot, "",
                jetBrainsMono, 15, ColParchmentSel, TextAlignmentOptions.MidlineRight, FontStyles.Normal);
            SetTopLeftBox(uses.rectTransform, 492, 8, 86, 18);

            return new SupplyConvoyRefs.SlotRefs {
                root = slot.gameObject, background = bgImg,
                sigil = sigil.GetComponent<Image>(),
                nameText = name, rankText = rank, usesText = uses,
                sprDefault = spSlotDefault, sprFocused = spSlotFocused,
                sprEquipped = spSlotEquipped, sprDepleted = spSlotDepleted,
                sprEmpty = spSlotEmpty,
            };
        }

        // ==================================================================
        // Right column
        // ==================================================================

        static void BuildRightColumn(RectTransform root)
        {
            var col = NewRect("RightColumn", root);
            col.anchorMin = col.anchorMax = new Vector2(0, 1);
            col.pivot = new Vector2(0, 1);
            col.anchoredPosition = new Vector2(700, -120);
            col.sizeDelta = new Vector2(1180, 900);

            BuildConvoyHead(col);
            BuildTabstrip(col);
            BuildConvoyList(col);
        }

        static void BuildConvoyHead(RectTransform parent)
        {
            var ch = NewRect("ConvoyHead", parent);
            SetTopLeftBox(ch, 0, 0, 1180, 72);
            var bg = NewImage("Background", ch, Color.white);
            SetStretch(bg.GetComponent<RectTransform>(), 0);
            bg.GetComponent<Image>().sprite = spConvoyHead;

            var title = NewText("Title", ch, "SATCHEL · RASADĀRA CONVOY",
                cinzel, 18, ColBrassGlow, TextAlignmentOptions.Left, FontStyles.Bold);
            title.characterSpacing = 40;
            if (matCartoucheTitleGlow != null) title.fontMaterial = matCartoucheTitleGlow;
            SetTopLeftBox(title.rectTransform, 26, 24, 700, 22);
            refs.convoyTitle = title;

            var lbl = NewText("StockLabel", ch, "STOCK",
                cinzel, 11, ColParchDim, TextAlignmentOptions.Right, FontStyles.Normal);
            lbl.characterSpacing = 30;
            SetTopLeftBox(lbl.rectTransform, 1180 - 26 - 260, 28, 70, 14);

            var num = NewText("StockNum", ch, "0 / 100",
                jetBrainsMono, 28, ColParchmentSel, TextAlignmentOptions.Right, FontStyles.Normal);
            if (matStockNumGlow != null) num.fontMaterial = matStockNumGlow;
            SetTopLeftBox(num.rectTransform, 1180 - 26 - 180, 20, 180, 32);
            refs.stockNum = num;
        }

        static void BuildTabstrip(RectTransform parent)
        {
            var ts = NewRect("Tabstrip", parent);
            SetTopLeftBox(ts, 0, 72, 1180, 62);
            var bg = NewImage("Background", ts, Color.white);
            SetStretch(bg.GetComponent<RectTransform>(), 0);
            bg.GetComponent<Image>().sprite = spTabstrip;

            string[] letters = { "A", "S", "L", "X", "B", "M", "I", "D", "T", "C" };
            string[] names   = { "ALL", "SWORD", "LANCE", "AXE", "BOW", "ANIMA", "LIGHT", "DARK", "STAFF", "CONS" };
            float tabW = 1180f / 10f;

            refs.tabs = new SupplyConvoyRefs.TabRefs[10];
            for (int i = 0; i < 10; i++)
            {
                var tab = NewRect($"Tab_{i}_{names[i]}", ts);
                SetTopLeftBox(tab, i * tabW, 0, tabW, 62);

                var tbg = NewImage("Background", tab, Color.white);
                SetStretch(tbg.GetComponent<RectTransform>(), 0);
                var tbgImg = tbg.GetComponent<Image>();
                tbgImg.sprite = spTabDefault;
                tbgImg.raycastTarget = true;

                // Letter glyph (placeholder for a real icon)
                var glyph = NewText("Glyph", tab, letters[i],
                    cinzel, 18, ColParchDim, TextAlignmentOptions.Center, FontStyles.Bold);
                glyph.characterSpacing = 0;
                SetTopLeftBox(glyph.rectTransform, 0, 14, tabW, 24);

                var count = NewText("Count", tab, "0",
                    jetBrainsMono, 10, ColParchDim, TextAlignmentOptions.Right, FontStyles.Normal);
                SetTopLeftBox(count.rectTransform, tabW - 28, 6, 24, 14);

                refs.tabs[i] = new SupplyConvoyRefs.TabRefs {
                    root = tab.gameObject, background = tbgImg,
                    label = glyph, countText = count,
                    sprDefault = spTabDefault, sprHover = spTabHover,
                    sprFocused = spTabFocused, sprActive = spTabActive,
                };
            }
        }

        static void BuildConvoyList(RectTransform parent)
        {
            var cl = NewRect("ConvoyList", parent);
            SetTopLeftBox(cl, 0, 134, 1180, 766);
            var bg = NewImage("Background", cl, Color.white);
            SetStretch(bg.GetComponent<RectTransform>(), 0);
            bg.GetComponent<Image>().sprite = spConvoyList;

            // Rows container
            var rowsC = NewRect("Rows", cl);
            SetTopLeftBox(rowsC, 18, 6, 1124, 754);
            refs.rowsContainer = rowsC;

            refs.rows = new SupplyConvoyRefs.RowRefs[10];
            for (int i = 0; i < 10; i++)
                refs.rows[i] = BuildConvoyRow(rowsC, i);

            // Scrollrail
            var sr = NewRect("Scrollrail", cl);
            SetTopLeftBox(sr, 1180 - 14 - 12, 12, 14, 766 - 24);
            var srBg = NewImage("Background", sr, Color.white);
            SetStretch(srBg.GetComponent<RectTransform>(), 0);
            srBg.GetComponent<Image>().sprite = spScrollrail;

            var thumb = NewImage("Thumb", sr, Color.white);
            var thumbRt = thumb.GetComponent<RectTransform>();
            thumbRt.anchorMin = new Vector2(0, 1);
            thumbRt.anchorMax = new Vector2(1, 1);
            thumbRt.pivot = new Vector2(0.5f, 1);
            thumbRt.anchoredPosition = new Vector2(0, -(766 - 24) * 0.18f);
            thumbRt.offsetMin = new Vector2(2, 0);
            thumbRt.offsetMax = new Vector2(-2, 0);
            thumbRt.sizeDelta = new Vector2(-4, (766 - 24) * 0.34f);
            thumb.GetComponent<Image>().sprite = spScrollThumb;
            refs.scrollThumb = thumbRt;

            var capTop = NewImage("CapTop", sr, Color.white);
            var capTopRt = capTop.GetComponent<RectTransform>();
            capTopRt.anchorMin = capTopRt.anchorMax = new Vector2(0.5f, 1);
            capTopRt.pivot = new Vector2(0.5f, 0.5f);
            capTopRt.anchoredPosition = new Vector2(0, 0);
            capTopRt.sizeDelta = new Vector2(22, 12);
            capTop.GetComponent<Image>().sprite = spScrollCapTop;

            var capBot = NewImage("CapBottom", sr, Color.white);
            var capBotRt = capBot.GetComponent<RectTransform>();
            capBotRt.anchorMin = capBotRt.anchorMax = new Vector2(0.5f, 0);
            capBotRt.pivot = new Vector2(0.5f, 0.5f);
            capBotRt.anchoredPosition = new Vector2(0, 0);
            capBotRt.sizeDelta = new Vector2(22, 12);
            capBot.GetComponent<Image>().sprite = spScrollCapBottom;
        }

        static SupplyConvoyRefs.RowRefs BuildConvoyRow(RectTransform parent, int i)
        {
            float y = i * 64;
            var row = NewRect($"Row_{i}", parent);
            SetTopLeftBox(row, 0, y, 1124, 64);

            var bg = NewImage("Background", row, Color.white);
            SetStretch(bg.GetComponent<RectTransform>(), 0);
            var bgImg = bg.GetComponent<Image>();
            bgImg.sprite = spRowDefault;
            bgImg.raycastTarget = true;

            var sigil = NewImage("Sigil", row, ColBrassLite);
            var sigilRt = sigil.GetComponent<RectTransform>();
            sigilRt.anchorMin = sigilRt.anchorMax = new Vector2(0, 1);
            sigilRt.pivot = new Vector2(0, 1);
            sigilRt.anchoredPosition = new Vector2(14, -16);
            sigilRt.sizeDelta = new Vector2(32, 32);
            sigil.GetComponent<Image>().preserveAspect = true;

            var name = NewText("Name", row, "",
                cormorant, 24, ColParchment, TextAlignmentOptions.TopLeft, FontStyles.Bold);
            SetTopLeftBox(name.rectTransform, 60, 10, 520, 28);

            var sub = NewText("Sub", row, "",
                ebGaramond, 13, ColParchDim, TextAlignmentOptions.TopLeft, FontStyles.Italic);
            SetTopLeftBox(sub.rectTransform, 60, 38, 560, 18);

            // Rank chip (background rect + label)
            var chip = NewRect("RankChip", row);
            SetTopLeftBox(chip, 1124 - 360, 20, 96, 24);
            var chipBg = NewImage("Background", chip, new Color(ColIndigoHi.r, ColIndigoHi.g, ColIndigoHi.b, 0.6f));
            SetStretch(chipBg.GetComponent<RectTransform>(), 0);
            chipBg.GetComponent<Image>().raycastTarget = false;
            // simple 1px brass stroke via Outline isn't supported on Image — fake with a border child later if needed
            var rankTxt = NewText("Label", chip, "",
                cinzel, 10, ColBrassLite, TextAlignmentOptions.Center, FontStyles.Bold);
            rankTxt.characterSpacing = 22;
            SetTopLeftBox(rankTxt.rectTransform, 0, 4, 96, 16);

            var uses = NewText("Uses", row, "",
                jetBrainsMono, 18, ColParchmentSel, TextAlignmentOptions.MidlineRight, FontStyles.Normal);
            SetTopLeftBox(uses.rectTransform, 1124 - 240, 20, 100, 22);

            // Durability bar
            var track = NewImage("DurabilityTrack", row, new Color(ColBrass.r, ColBrass.g, ColBrass.b, 0.15f));
            SetTopLeftBox(track.GetComponent<RectTransform>(), 1124 - 130, 32, 120, 4);
            track.GetComponent<Image>().raycastTarget = false;

            var fill = NewImage("DurabilityFill", row, ColBrass);
            var fillRt = fill.GetComponent<RectTransform>();
            fillRt.anchorMin = fillRt.anchorMax = new Vector2(0, 1);
            fillRt.pivot = new Vector2(0, 1);
            fillRt.anchoredPosition = new Vector2(1124 - 130, -32);
            fillRt.sizeDelta = new Vector2(120, 4);
            fill.GetComponent<Image>().raycastTarget = false;

            return new SupplyConvoyRefs.RowRefs {
                root = row.gameObject, background = bgImg,
                sigil = sigil.GetComponent<Image>(),
                nameText = name, subText = sub,
                rankText = rankTxt, rankChipBg = chipBg.GetComponent<Image>(),
                usesText = uses,
                durabilityTrack = track.GetComponent<Image>(),
                durabilityFill = fill.GetComponent<Image>(),
                sprDefault = spRowDefault, sprFocused = spRowFocused,
                sprDepleted = spRowDepleted, sprDisabled = spRowDisabled,
            };
        }

        // ==================================================================
        // Footer
        // ==================================================================

        static void BuildFooter(RectTransform root)
        {
            var f = NewRect("FooterHints", root);
            f.anchorMin = new Vector2(0, 0);
            f.anchorMax = new Vector2(1, 0);
            f.pivot = new Vector2(0.5f, 0);
            f.anchoredPosition = new Vector2(0, 0);
            f.sizeDelta = new Vector2(0, 58);

            var bg = NewImage("Background", f, Color.white);
            SetStretch(bg.GetComponent<RectTransform>(), 0);
            bg.GetComponent<Image>().sprite = spFooterHints;

            var hints = NewText("HintsText", f,
                "\u25B2 \u25BC  CHOOSE      \u25C0 \u25B6  CATEGORY      \u23CE  CONFIRM      TAB  SWITCH PANEL      \u238B  BACK · CONSUMES TURN",
                cinzel, 11, ColParchDim, TextAlignmentOptions.Center, FontStyles.Normal);
            hints.characterSpacing = 22;
            SetTopLeftBox(hints.rectTransform, 0, 21, CanvasWidth, 16);
            refs.hintsText = hints;
        }

        // ==================================================================
        // helpers
        // ==================================================================

        static TMP_FontAsset LoadFont(string file) => AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(FontDir + file);
        static Sprite LoadSprite(string file) => AssetDatabase.LoadAssetAtPath<Sprite>(SpriteDir + file);
        static Sprite LoadIcon(string file)   => AssetDatabase.LoadAssetAtPath<Sprite>(IconDir + file);

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

        static RectTransform NewRect(string name, RectTransform parent) => NewRect(name, (Transform)parent);

        static TextMeshProUGUI NewText(string name, Transform parent, string str,
            TMP_FontAsset font, float size, Color color,
            TextAlignmentOptions align, FontStyles style)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(TextMeshProUGUI));
            go.transform.SetParent(parent, false);
            var t = go.GetComponent<TextMeshProUGUI>();
            t.text = str;
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

        static TextMeshProUGUI NewText(string name, RectTransform parent, string str,
            TMP_FontAsset font, float size, Color color,
            TextAlignmentOptions align, FontStyles style)
            => NewText(name, (Transform)parent, str, font, size, color, align, style);

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

        static Color Hex(string hex)
        {
            byte r = byte.Parse(hex.Substring(0,2), System.Globalization.NumberStyles.HexNumber);
            byte g = byte.Parse(hex.Substring(2,2), System.Globalization.NumberStyles.HexNumber);
            byte b = byte.Parse(hex.Substring(4,2), System.Globalization.NumberStyles.HexNumber);
            return new Color(r/255f, g/255f, b/255f, 1f);
        }
    }
}
