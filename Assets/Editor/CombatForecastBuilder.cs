using System.IO;
using ProjectAstra.Core.UI;
using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

namespace ProjectAstra.EditorTools
{
    // ==========================================================================================
    // CombatForecast (Variant A · Indigo Codex) — 960×720 modal overlay, top-right anchor.
    //
    // Concept source: docs/mockups/Combat Forecast Mockups.html
    // Figma source:   aDzZh8DzaJ1eBtdy1h8MMv  (COMBAT_FORECAST_FIGMA in .secrets/figma_files.env)
    //
    // Strategy: the whole 960×720 panel chrome (gradient, border, spine column, clash medallion,
    // header cartouche, DMG-row hero background, stat dividers, footer band) is baked into a
    // single sprite `forecast_panel_composite.png`. This builder overlays live TMP labels and
    // state-swap sprites on top. Positions are in panel-local coordinates (top-left origin).
    //
    // Reusable sigils come from Assets/UI/InventoryPopup/Icons/. No sigil exports live under
    // Assets/UI/CombatForecast/Icons/.
    // ==========================================================================================
    public static class CombatForecastBuilder
    {
        const float CanvasWidth  = 1920f;
        const float CanvasHeight = 1080f;
        const float PanelW = 960f;
        const float PanelH = 720f;
        const float HeaderH = 64f;
        const float FooterH = 56f;
        const float StageH  = PanelH - HeaderH - FooterH;   // 600
        const float SpineW  = 200f;
        const float HalfW   = (PanelW - SpineW) / 2f;        // 380

        const string SpriteDir   = "Assets/UI/CombatForecast/Sprites/";
        const string MaterialDir = "Assets/UI/CombatForecast/Materials/";
        const string FontDir     = "Assets/UI/TradeScreen/Fonts/";
        const string IconDir     = "Assets/UI/InventoryPopup/Icons/";
        const string PrefabPath  = "Assets/UI/CombatForecast/CombatForecast.prefab";

        static readonly Color Parchment    = Hex("f2e6c4");
        static readonly Color ParchmentSel = Hex("fff5d8");
        static readonly Color ParchDim     = Hex("c9b98a");
        static readonly Color Brass        = Hex("c9993a");
        static readonly Color BrassLite    = Hex("e8c66a");
        static readonly Color BrassGlow    = Hex("fde49a");
        static readonly Color Vermillion   = Hex("b0382a");
        static readonly Color VermillionLt = Hex("d14a34");
        static readonly Color HpGreen      = Hex("60c870");

        static TMP_FontAsset cinzel, cormorant, ebGaramond, jetBrainsMono;
        static Sprite sprComposite, sprEffective, sprKo, sprVs,
                      sprDblAs, sprDblBrave, sprDblCombined,
                      sprTagAtk, sprTagDef, sprWeaponRow, sprNoCounter,
                      sprHpTrack, sprHpGreen, sprHpYellow, sprHpRed, sprHpPred,
                      sprStatus;
        static Sprite sigSword, sigLance, sigAxe, sigBow, sigStaff, sigConsumable;
        static Material matDmgHero, matKo, matEffective, matStatVal, matUnitName, matBrassLabel, matVsTag;
        static CombatForecastRefs refs;

        [MenuItem("Project Astra/Build Combat Forecast (prefab)")]
        public static void BuildPrefab()
        {
            LoadResources();

            var root = BuildHierarchy();
            if (root == null) return;

            Directory.CreateDirectory(Path.GetDirectoryName(PrefabPath));
            PrefabUtility.SaveAsPrefabAsset(root, PrefabPath, out bool ok);
            Object.DestroyImmediate(root);
            if (ok) Debug.Log($"CombatForecast prefab saved to {PrefabPath}");
            else    Debug.LogError("CombatForecast prefab save failed.");

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
        }

        static void LoadResources()
        {
            cinzel        = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(FontDir + "Cinzel SDF.asset");
            cormorant     = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(FontDir + "CormorantGaramond SDF.asset");
            ebGaramond    = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(FontDir + "EBGaramond SDF.asset");
            jetBrainsMono = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(FontDir + "JetBrainsMono SDF.asset");

            sprComposite    = LoadSprite("forecast_panel_composite.png");
            sprEffective    = LoadSprite("chip_effective.png");
            sprKo           = LoadSprite("badge_ko.png");
            sprVs           = LoadSprite("badge_vs.png");
            sprDblAs        = LoadSprite("chip_double_as.png");
            sprDblBrave     = LoadSprite("chip_double_brave.png");
            sprDblCombined  = LoadSprite("chip_double_combined.png");
            sprTagAtk       = LoadSprite("tag_atk.png");
            sprTagDef       = LoadSprite("tag_def.png");
            sprWeaponRow    = LoadSprite("weapon_row.png");
            sprNoCounter    = LoadSprite("ribbon_nocounter.png");
            sprHpTrack      = LoadSprite("hp_track.png");
            sprHpGreen      = LoadSprite("hp_fill_green.png");
            sprHpYellow     = LoadSprite("hp_fill_yellow.png");
            sprHpRed        = LoadSprite("hp_fill_red.png");
            sprHpPred       = LoadSprite("hp_pred.png");
            sprStatus       = LoadSprite("badge_status.png");

            sigSword      = LoadIcon("sigil_sword.png");
            sigLance      = LoadIcon("sigil_lance.png");
            sigAxe        = LoadIcon("sigil_axe.png");
            sigBow        = LoadIcon("sigil_bow.png");
            sigStaff      = LoadIcon("sigil_staff.png");
            sigConsumable = LoadIcon("sigil_consumable.png");

            matDmgHero    = AssetDatabase.LoadAssetAtPath<Material>(CombatForecastMaterials.DmgHeroGlow);
            matKo         = AssetDatabase.LoadAssetAtPath<Material>(CombatForecastMaterials.KoBadgeGlow);
            matEffective  = AssetDatabase.LoadAssetAtPath<Material>(CombatForecastMaterials.EffectiveGlow);
            matStatVal    = AssetDatabase.LoadAssetAtPath<Material>(CombatForecastMaterials.StatValueGlow);
            matUnitName   = AssetDatabase.LoadAssetAtPath<Material>(CombatForecastMaterials.UnitNameGlow);
            matBrassLabel = AssetDatabase.LoadAssetAtPath<Material>(CombatForecastMaterials.BrassLabelGlow);
            matVsTag      = AssetDatabase.LoadAssetAtPath<Material>(CombatForecastMaterials.VsTagGlow);

            if (sprComposite == null)
                Debug.LogWarning("CombatForecast sprites missing — run download_combat_forecast_assets.sh first.");
            if (matDmgHero == null)
                Debug.LogWarning("CombatForecast glow materials missing — run 'Project Astra/Generate CombatForecast Glow Materials' first.");
        }

        // ==================================================================

        static GameObject BuildHierarchy()
        {
            // Root stretches full canvas so the panel can anchor top-right / top-left.
            var root = new GameObject("CombatForecast", typeof(RectTransform));
            var rt = root.GetComponent<RectTransform>();
            rt.anchorMin = Vector2.zero; rt.anchorMax = Vector2.one;
            rt.pivot = new Vector2(0.5f, 0.5f);
            rt.offsetMin = rt.offsetMax = Vector2.zero;
            refs = root.AddComponent<CombatForecastRefs>();

            // Panel — anchored top-right, 40px inset
            var panel = NewRect("Panel", root.transform);
            panel.anchorMin = panel.anchorMax = new Vector2(1f, 1f);
            panel.pivot = new Vector2(1f, 1f);
            panel.anchoredPosition = new Vector2(-40f, -40f);
            panel.sizeDelta = new Vector2(PanelW, PanelH);

            // Composite chrome (the big baked 960×720 background)
            var composite = NewImage("PanelComposite", panel, Color.white);
            SetStretch(composite.GetComponent<RectTransform>(), 0);
            composite.GetComponent<Image>().sprite = sprComposite;
            composite.GetComponent<Image>().raycastTarget = true;
            refs.panelComposite = composite.GetComponent<Image>();

            // Header title + VS tag live ON TOP of the composite's baked header band
            var title = NewText("HeaderTitle", panel, "COMBAT  FORECAST",
                cinzel, 16, BrassGlow, TextAlignmentOptions.Center, FontStyles.Bold);
            title.characterSpacing = 50;
            if (matBrassLabel != null) title.fontMaterial = matBrassLabel;
            SetTopLeftBox(title.rectTransform, 0, 22, PanelW, 22);
            refs.headerTitle = title;

            var vs = NewImage("VsTag", panel, Color.white);
            var vsRt = vs.GetComponent<RectTransform>();
            vsRt.anchorMin = vsRt.anchorMax = new Vector2(0, 1);
            vsRt.pivot = new Vector2(0.5f, 0.5f);
            vsRt.anchoredPosition = new Vector2(PanelW / 2f, -HeaderH);
            vsRt.sizeDelta = new Vector2(70, 26);
            vs.GetComponent<Image>().sprite = sprVs;
            var vsLabel = NewText("VsLabel", vs.transform, "VS",
                cinzel, 14, BrassGlow, TextAlignmentOptions.Center, FontStyles.Bold);
            vsLabel.characterSpacing = 40;
            if (matVsTag != null) vsLabel.fontMaterial = matVsTag;
            SetStretch(vsLabel.rectTransform, 0);
            refs.vsTag = vsLabel;

            // DMG hero numbers on the spine
            BuildDmgHero(panel);
            BuildStatRows(panel);

            // Left + Right unit sides (live text overlaying the baked halves)
            BuildUnitSide(panel, side: 0); // left
            BuildUnitSide(panel, side: 1); // right

            // No-counter ribbon (absolute, covers defender column middle)
            BuildNoCounterRibbon(panel);

            // Footer hints text
            BuildFooter(panel);

            // Sprite lookup tables on refs
            refs.chipEffective      = sprEffective;
            refs.chipDoubleAs       = sprDblAs;
            refs.chipDoubleBrave    = sprDblBrave;
            refs.chipDoubleCombined = sprDblCombined;
            refs.tagAtk             = sprTagAtk;
            refs.tagDef             = sprTagDef;
            refs.hpTrack            = sprHpTrack;
            refs.hpFillGreen        = sprHpGreen;
            refs.hpFillYellow       = sprHpYellow;
            refs.hpFillRed          = sprHpRed;
            refs.hpPred             = sprHpPred;
            refs.badgeKo            = sprKo;
            refs.ribbonNoCounter    = sprNoCounter;
            refs.weaponRow          = sprWeaponRow;
            refs.sigilSword         = sigSword;
            refs.sigilLance         = sigLance;
            refs.sigilAxe           = sigAxe;
            refs.sigilBow           = sigBow;
            refs.sigilStaff         = sigStaff;
            // Anima/light/dark not shipped yet — fall back to sword sigil
            refs.sigilAnima         = sigSword;
            refs.sigilLight         = sigSword;
            refs.sigilDark          = sigSword;

            return root;
        }

        // ==================================================================
        // Spine — DMG hero + HIT/CRIT rows
        // ==================================================================

        static void BuildDmgHero(RectTransform panel)
        {
            // Spine at x = HalfW (380), width 200, starts at y = HeaderH (64).
            // DMG row is 150h, sits 96px below top of spine (under clash).
            // Two cells of width SpineW/2 = 100, each with label "DEALS" and a big number.
            float dmgTop = HeaderH + 96f;   // 160
            float dmgH = 150f;

            var aLbl = NewText("DmgLabel_A", panel, "DEALS",
                cinzel, 11, BrassLite, TextAlignmentOptions.Center, FontStyles.Bold);
            aLbl.characterSpacing = 42;
            if (matBrassLabel != null) aLbl.fontMaterial = matBrassLabel;
            SetTopLeftBox(aLbl.rectTransform, HalfW, dmgTop + 22, 100, 14);
            refs.attackerDmgLabel = aLbl;

            var aNum = NewText("DmgNum_A", panel, "0",
                cinzel, 72, BrassGlow, TextAlignmentOptions.Center, FontStyles.Bold);
            if (matDmgHero != null) aNum.fontMaterial = matDmgHero;
            SetTopLeftBox(aNum.rectTransform, HalfW, dmgTop + 40, 100, 80);
            refs.attackerDmgNum = aNum;

            var dLbl = NewText("DmgLabel_D", panel, "DEALS",
                cinzel, 11, BrassLite, TextAlignmentOptions.Center, FontStyles.Bold);
            dLbl.characterSpacing = 42;
            if (matBrassLabel != null) dLbl.fontMaterial = matBrassLabel;
            SetTopLeftBox(dLbl.rectTransform, HalfW + 100, dmgTop + 22, 100, 14);
            refs.defenderDmgLabel = dLbl;

            var dNum = NewText("DmgNum_D", panel, "0",
                cinzel, 72, BrassGlow, TextAlignmentOptions.Center, FontStyles.Bold);
            if (matDmgHero != null) dNum.fontMaterial = matDmgHero;
            SetTopLeftBox(dNum.rectTransform, HalfW + 100, dmgTop + 40, 100, 80);
            refs.defenderDmgNum = dNum;
        }

        static void BuildStatRows(RectTransform panel)
        {
            // Stats region below DMG row (from y = 64 + 96 + 150 = 310 to y = 64 + 600 = 664)
            float statsTop = HeaderH + 96f + 150f;  // 310
            float statsBottom = HeaderH + StageH;   // 664
            float statsH = statsBottom - statsTop;  // 354
            float rowH = statsH / 2f;               // 177 each for HIT and CRIT

            BuildStatRow(panel, "HIT",  statsTop,           rowH,
                out refs.hitLabel, out refs.hitAttackerVal, out refs.hitDefenderVal, critTint:false);
            BuildStatRow(panel, "CRIT", statsTop + rowH,    rowH,
                out refs.critLabel, out refs.critAttackerVal, out refs.critDefenderVal, critTint:true);
        }

        static void BuildStatRow(RectTransform panel, string label, float y, float h,
            out TextMeshProUGUI lblOut, out TextMeshProUGUI lOut, out TextMeshProUGUI rOut, bool critTint)
        {
            // Spine x: HalfW..HalfW+200. Center label in middle ~72px; values flank.
            Color lblColor = critTint ? VermillionLt : BrassLite;

            var lbl = NewText("Label_" + label, panel, label,
                cinzel, 12, lblColor, TextAlignmentOptions.Center, FontStyles.Bold);
            lbl.characterSpacing = 40;
            if (matBrassLabel != null) lbl.fontMaterial = matBrassLabel;
            SetTopLeftBox(lbl.rectTransform, HalfW + (SpineW - 72f) / 2f, y + h / 2f - 8, 72, 16);
            lblOut = lbl;

            Color valColor = critTint ? Hex("f3d272") : ParchmentSel;
            var lVal = NewText("Val_L_" + label, panel, "0%",
                jetBrainsMono, 28, valColor, TextAlignmentOptions.Right, FontStyles.Normal);
            if (matStatVal != null) lVal.fontMaterial = matStatVal;
            SetTopLeftBox(lVal.rectTransform, HalfW + 8, y + h / 2f - 16, (SpineW - 72f) / 2f - 14, 32);
            lOut = lVal;

            var rVal = NewText("Val_R_" + label, panel, "0%",
                jetBrainsMono, 28, valColor, TextAlignmentOptions.Left, FontStyles.Normal);
            if (matStatVal != null) rVal.fontMaterial = matStatVal;
            SetTopLeftBox(rVal.rectTransform, HalfW + (SpineW + 72f) / 2f + 6, y + h / 2f - 16, (SpineW - 72f) / 2f - 14, 32);
            rOut = rVal;
        }

        // ==================================================================
        // Unit side (left or right)
        // ==================================================================

        static void BuildUnitSide(RectTransform panel, int side)
        {
            bool isLeft = side == 0;
            float halfX = isLeft ? 0 : HalfW + SpineW;   // 0 or 580
            var UnitSide = isLeft ? refs.left : refs.right;

            // Side tag (ATK or DEF)
            var tagBg = NewImage("SideTagBg_" + (isLeft ? "L" : "R"), panel, Color.white);
            var tagRt = tagBg.GetComponent<RectTransform>();
            SetTopLeftBox(tagRt, halfX + (isLeft ? 28 : HalfW - 28 - 108), HeaderH + 24, 108, 20);
            tagBg.GetComponent<Image>().sprite = isLeft ? sprTagAtk : sprTagDef;
            UnitSide.sideTagBg = tagBg.GetComponent<Image>();

            var tagLbl = NewText("SideTagLabel_" + (isLeft ? "L" : "R"), panel,
                isLeft ? "ATTACKER" : "DEFENDER",
                cinzel, 10, isLeft ? BrassGlow : VermillionLt,
                TextAlignmentOptions.Center, FontStyles.Bold);
            tagLbl.characterSpacing = 42;
            SetTopLeftBox(tagLbl.rectTransform, halfX + (isLeft ? 28 : HalfW - 28 - 108), HeaderH + 26, 108, 16);
            UnitSide.sideTagLabel = tagLbl;

            // Unit name (Cormorant 700 42)
            var name = NewText("UnitName_" + (isLeft ? "L" : "R"), panel, "—",
                cormorant, 38, ParchmentSel,
                isLeft ? TextAlignmentOptions.Left : TextAlignmentOptions.Right, FontStyles.Bold);
            if (matUnitName != null) name.fontMaterial = matUnitName;
            float nameW = HalfW - 56;
            SetTopLeftBox(name.rectTransform, halfX + (isLeft ? 28 : 28), HeaderH + 54, nameW, 44);
            UnitSide.unitName = name;

            // Unit sub (EB Garamond italic 16)
            var sub = NewText("UnitSub_" + (isLeft ? "L" : "R"), panel, "—",
                ebGaramond, 16, ParchDim,
                isLeft ? TextAlignmentOptions.Left : TextAlignmentOptions.Right, FontStyles.Italic);
            SetTopLeftBox(sub.rectTransform, halfX + 28, HeaderH + 98, nameW, 20);
            UnitSide.unitSub = sub;

            // Meta grid: Level + Class in one row
            float metaY = HeaderH + 124;
            var lvlLbl = NewText("LvlLbl_" + (isLeft ? "L" : "R"), panel, "LEVEL",
                cinzel, 10, ParchDim,
                isLeft ? TextAlignmentOptions.Left : TextAlignmentOptions.Right, FontStyles.Normal);
            lvlLbl.characterSpacing = 28;
            SetTopLeftBox(lvlLbl.rectTransform, halfX + 28, metaY, 60, 14);

            var lvlVal = NewText("LvlVal_" + (isLeft ? "L" : "R"), panel, "—",
                jetBrainsMono, 13, Parchment,
                isLeft ? TextAlignmentOptions.Left : TextAlignmentOptions.Right, FontStyles.Normal);
            SetTopLeftBox(lvlVal.rectTransform, halfX + (isLeft ? 92 : HalfW - 28 - 60), metaY, 60, 14);
            UnitSide.levelValue = lvlVal;

            var clsLbl = NewText("ClsLbl_" + (isLeft ? "L" : "R"), panel, "CLASS",
                cinzel, 10, ParchDim,
                isLeft ? TextAlignmentOptions.Left : TextAlignmentOptions.Right, FontStyles.Normal);
            clsLbl.characterSpacing = 28;
            SetTopLeftBox(clsLbl.rectTransform, halfX + (isLeft ? 164 : HalfW - 28 - 164), metaY, 60, 14);

            var clsVal = NewText("ClsVal_" + (isLeft ? "L" : "R"), panel, "—",
                jetBrainsMono, 13, Parchment,
                isLeft ? TextAlignmentOptions.Left : TextAlignmentOptions.Right, FontStyles.Normal);
            SetTopLeftBox(clsVal.rectTransform, halfX + (isLeft ? 228 : HalfW - 28 - 120), metaY, 120, 14);
            UnitSide.classValue = clsVal;

            // Weapon row (340×40)
            float wrowY = HeaderH + 152;
            var wrow = NewImage("WeaponRow_" + (isLeft ? "L" : "R"), panel, Color.white);
            var wrowRt = wrow.GetComponent<RectTransform>();
            SetTopLeftBox(wrowRt, halfX + (isLeft ? 28 : HalfW - 28 - 340), wrowY, 340, 40);
            wrow.GetComponent<Image>().sprite = sprWeaponRow;
            UnitSide.weaponRowBg = wrow.GetComponent<Image>();

            var icon = NewImage("WeaponIcon_" + (isLeft ? "L" : "R"), wrow.transform, BrassGlow);
            var iconRt = icon.GetComponent<RectTransform>();
            iconRt.anchorMin = iconRt.anchorMax = new Vector2(0, 0.5f);
            iconRt.pivot = new Vector2(0, 0.5f);
            iconRt.anchoredPosition = new Vector2(12, 0);
            iconRt.sizeDelta = new Vector2(24, 24);
            icon.GetComponent<Image>().preserveAspect = true;
            UnitSide.weaponIcon = icon.GetComponent<Image>();

            var wname = NewText("WeaponName_" + (isLeft ? "L" : "R"), wrow.transform, "—",
                cormorant, 20, Parchment,
                isLeft ? TextAlignmentOptions.MidlineLeft : TextAlignmentOptions.MidlineRight, FontStyles.Bold);
            SetTopLeftBox(wname.rectTransform, 44, 8, 240, 24);
            UnitSide.weaponName = wname;

            var arrow = NewText("WeaponArrow_" + (isLeft ? "L" : "R"), wrow.transform, "",
                jetBrainsMono, 18, HpGreen,
                TextAlignmentOptions.Center, FontStyles.Bold);
            SetTopLeftBox(arrow.rectTransform, 300, 10, 32, 24);
            UnitSide.weaponArrow = arrow;

            // EFFECTIVE chip (below weapon row, inactive by default)
            var effParent = NewRect("EffectiveChip_" + (isLeft ? "L" : "R"), panel);
            SetTopLeftBox(effParent, halfX + (isLeft ? 28 : HalfW - 28 - 180), HeaderH + 200, 180, 32);
            var effBg = NewImage("Background", effParent, Color.white);
            SetStretch(effBg.GetComponent<RectTransform>(), 0);
            effBg.GetComponent<Image>().sprite = sprEffective;
            var effLbl = NewText("Label", effParent, "EFFECTIVE",
                cinzel, 13, BrassGlow, TextAlignmentOptions.Center, FontStyles.Bold);
            effLbl.characterSpacing = 40;
            if (matEffective != null) effLbl.fontMaterial = matEffective;
            SetStretch(effLbl.rectTransform, 0);
            effParent.gameObject.SetActive(false);
            UnitSide.effectiveChip = effParent.gameObject;

            // HP block — near bottom of the half, above footer
            float hpY = HeaderH + StageH - 90;
            BuildHpBlock(panel, isLeft, halfX, hpY, UnitSide);

            // Doubling chip (absolute, overhangs spine bottom) — anchored inside spine area
            var dblParent = NewRect("DoubleChip_" + (isLeft ? "L" : "R"), panel);
            float dblX = isLeft ? HalfW - 20 - 130 : HalfW + SpineW + 20;
            SetTopLeftBox(dblParent, dblX, HeaderH + StageH - 56 - 14, 130, 56);
            var dblBg = NewImage("Background", dblParent, Color.white);
            SetStretch(dblBg.GetComponent<RectTransform>(), 0);
            dblBg.GetComponent<Image>().sprite = sprDblAs;
            UnitSide.doubleChipBg = dblBg.GetComponent<Image>();

            var dblNum = NewText("Number", dblParent, "×2",
                cinzel, 24, BrassGlow, TextAlignmentOptions.Center, FontStyles.Bold);
            SetTopLeftBox(dblNum.rectTransform, 0, 8, 130, 28);
            UnitSide.doubleChipNumber = dblNum;

            var dblTag = NewText("Tag", dblParent, "AS DOUBLE",
                cinzel, 9, ParchDim, TextAlignmentOptions.Center, FontStyles.Bold);
            dblTag.characterSpacing = 34;
            SetTopLeftBox(dblTag.rectTransform, 0, 36, 130, 14);
            UnitSide.doubleChipTag = dblTag;
            dblParent.gameObject.SetActive(false);
            UnitSide.doubleChipRoot = dblParent.gameObject;
        }

        static void BuildHpBlock(RectTransform panel, bool isLeft, float halfX, float hpY, CombatForecastRefs.UnitSide UnitSide)
        {
            float barX = halfX + 28;
            float barW = HalfW - 56;
            float sideBarX = isLeft ? barX : halfX + 28;  // same as above — mirrored is handled by inner anchors

            // Label + HP numeric line above bar
            var label = NewText("HpLabel_" + (isLeft ? "L" : "R"), panel, "HP",
                cinzel, 11, ParchDim,
                isLeft ? TextAlignmentOptions.Left : TextAlignmentOptions.Right, FontStyles.Bold);
            label.characterSpacing = 36;
            SetTopLeftBox(label.rectTransform, sideBarX, hpY, 40, 14);

            var numeric = NewText("HpNumeric_" + (isLeft ? "L" : "R"), panel, "0 / 0",
                jetBrainsMono, 20, ParchmentSel,
                isLeft ? TextAlignmentOptions.Left : TextAlignmentOptions.Right, FontStyles.Normal);
            if (matStatVal != null) numeric.fontMaterial = matStatVal;
            SetTopLeftBox(numeric.rectTransform, sideBarX + (isLeft ? 44 : 0), hpY - 4, barW - 44 - 80, 22);
            UnitSide.hpNumeric = numeric;

            var delta = NewText("HpDelta_" + (isLeft ? "L" : "R"), panel, "",
                jetBrainsMono, 14, VermillionLt,
                isLeft ? TextAlignmentOptions.Right : TextAlignmentOptions.Left, FontStyles.Bold);
            SetTopLeftBox(delta.rectTransform, sideBarX + (isLeft ? barW - 80 : 0), hpY - 2, 80, 18);
            UnitSide.hpDelta = delta;

            // HP track
            var track = NewImage("HpTrack_" + (isLeft ? "L" : "R"), panel, Color.white);
            var trackRt = track.GetComponent<RectTransform>();
            SetTopLeftBox(trackRt, sideBarX, hpY + 28, barW, 20);
            track.GetComponent<Image>().sprite = sprHpTrack;
            UnitSide.hpTrackBg = track.GetComponent<Image>();

            // HP predicted overlay (rendered BELOW the current fill; stripe pattern)
            var pred = NewImage("HpPred_" + (isLeft ? "L" : "R"), track.transform, Color.white);
            var predRt = pred.GetComponent<RectTransform>();
            predRt.anchorMin = new Vector2(0, 0); predRt.anchorMax = new Vector2(0, 1);
            predRt.pivot = new Vector2(0, 0.5f);
            predRt.anchoredPosition = new Vector2(0, 0);
            predRt.sizeDelta = new Vector2(0, -2);
            pred.GetComponent<Image>().sprite = sprHpPred;
            pred.GetComponent<Image>().type = Image.Type.Sliced;
            UnitSide.hpPredOverlay = pred.GetComponent<Image>();

            // HP current fill (on top of predicted)
            var fill = NewImage("HpFill_" + (isLeft ? "L" : "R"), track.transform, Color.white);
            var fillRt = fill.GetComponent<RectTransform>();
            fillRt.anchorMin = new Vector2(0, 0); fillRt.anchorMax = new Vector2(0, 1);
            fillRt.pivot = new Vector2(0, 0.5f);
            fillRt.anchoredPosition = new Vector2(0, 0);
            fillRt.sizeDelta = new Vector2(barW, -2);
            fill.GetComponent<Image>().sprite = sprHpGreen;
            fill.GetComponent<Image>().type = Image.Type.Sliced;
            UnitSide.hpFill = fill.GetComponent<Image>();

            // KO badge (absolute, centered over HP bar end) — inactive by default
            var ko = NewRect("KoBadge_" + (isLeft ? "L" : "R"), track.transform);
            ko.anchorMin = ko.anchorMax = new Vector2(isLeft ? 0 : 1, 0.5f);
            ko.pivot = new Vector2(isLeft ? 1 : 0, 0.5f);
            ko.anchoredPosition = new Vector2(isLeft ? -8 : 8, 0);
            ko.sizeDelta = new Vector2(86, 44);
            var koBg = NewImage("Background", ko, Color.white);
            SetStretch(koBg.GetComponent<RectTransform>(), 0);
            koBg.GetComponent<Image>().sprite = sprKo;
            var koLbl = NewText("Label", ko, "KO",
                cinzel, 18, BrassGlow, TextAlignmentOptions.Center, FontStyles.Bold);
            koLbl.characterSpacing = 38;
            if (matKo != null) koLbl.fontMaterial = matKo;
            SetStretch(koLbl.rectTransform, 0);
            ko.gameObject.SetActive(false);
            UnitSide.koBadgeTransform = ko;
        }

        static void BuildNoCounterRibbon(RectTransform panel)
        {
            var ribbon = NewRect("NoCounterRibbon", panel);
            SetTopLeftBox(ribbon, HalfW + SpineW, HeaderH + StageH - 90, HalfW, 40);
            var bg = NewImage("Background", ribbon, Color.white);
            SetStretch(bg.GetComponent<RectTransform>(), 0);
            bg.GetComponent<Image>().sprite = sprNoCounter;
            var lbl = NewText("Label", ribbon, "CANNOT COUNTER",
                cinzel, 16, BrassGlow, TextAlignmentOptions.Center, FontStyles.Bold);
            lbl.characterSpacing = 42;
            if (matKo != null) lbl.fontMaterial = matKo;
            SetStretch(lbl.rectTransform, 0);
            ribbon.gameObject.SetActive(false);
            refs.noCounterRibbon = ribbon.gameObject;
            refs.noCounterLabel = lbl;
        }

        static void BuildFooter(RectTransform panel)
        {
            var hints = NewText("FooterHints", panel,
                "\u23CE  ATTACK       \u238B  BACK       \u25C0 \u25B6  CYCLE TARGET",
                cinzel, 11, ParchDim, TextAlignmentOptions.Center, FontStyles.Normal);
            hints.characterSpacing = 32;
            SetTopLeftBox(hints.rectTransform, 0, PanelH - FooterH + 20, PanelW, 16);
            refs.footerHints = hints;
        }

        // ==================================================================
        // helpers (same shape as other builders)
        // ==================================================================

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
            t.text = str; if (font != null) t.font = font;
            t.fontSize = size; t.color = color; t.alignment = align; t.fontStyle = style;
            t.enableWordWrapping = false; t.overflowMode = TextOverflowModes.Overflow;
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
            rt.anchorMin = Vector2.zero; rt.anchorMax = Vector2.one;
            rt.pivot = new Vector2(0.5f, 0.5f);
            rt.offsetMin = new Vector2(inset, inset); rt.offsetMax = new Vector2(-inset, -inset);
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
