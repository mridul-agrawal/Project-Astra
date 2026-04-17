using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;

namespace ProjectAstra.EditorTools
{
    // ===========================================================================================
    // Unit Info Panel — full-screen 1920×1080 multi-page info screen.
    //
    // Three pages: Stats (Personal Data), Inventory (Items), Supports (Bonds & Oaths).
    // Shared left section (portrait, name, class, level, HP) with swappable right section per page.
    //
    // Color palette: Nila Dharma (indigo/sapphire/gold from mockup 06).
    // Layout/spacing: Rakta Mitti v2 structure (tight, large-font, stamp-style weapon ranks).
    //
    // See docs/mockups/unit_info_06_nila_dharma.html for the visual reference.
    // See docs/UI_WORKFLOW.md for the full Figma-to-Unity pipeline.
    // ===========================================================================================
    public static class UnitInfoPanelBuilder
    {
        const float CanvasWidth  = 1920f;
        const float CanvasHeight = 1080f;
        const bool  IsFullScreen = true;

        // No Figma rescale needed — designing directly at 1920×1080.
        const float Scale = 1f;

        // ---- paths ----
        const string SpriteDir = "Assets/UI/UnitInfoPanel/Icons/";
        const string FrameDir  = "Assets/UI/UnitInfoPanel/Sprites/";
        const string FontDir   = "Assets/UI/UnitInfoPanel/Fonts/";

        // ---- Nila Dharma palette ----
        static readonly Color ColIndigo      = new Color32(0x0a, 0x0e, 0x2a, 0xff);
        static readonly Color ColIndigoMid   = new Color32(0x14, 0x18, 0x40, 0xff);
        static readonly Color ColIndigoLight = new Color32(0x1e, 0x24, 0x58, 0xff);
        static readonly Color ColSapphire    = new Color32(0x2a, 0x4a, 0x8a, 0xff);
        static readonly Color ColGold        = new Color32(0xc8, 0xa0, 0x40, 0xff);
        static readonly Color ColGoldBright  = new Color32(0xe8, 0xc8, 0x60, 0xff);
        static readonly Color ColGoldDim     = new Color32(0xc8, 0xa0, 0x40, 0x40); // 25% alpha
        static readonly Color ColGoldFaint   = new Color32(0xc8, 0xa0, 0x40, 0x14); // 8% alpha
        static readonly Color ColIvory       = new Color32(0xe8, 0xe0, 0xd0, 0xff);
        static readonly Color ColSilver      = new Color32(0xa0, 0xa8, 0xb8, 0xff);
        static readonly Color ColGreen       = new Color32(0x60, 0xc8, 0x70, 0xff);
        static readonly Color ColRed         = new Color32(0xc8, 0x40, 0x40, 0xff);
        static readonly Color ColDimOverlay  = new Color(0, 0, 0, 0.55f);

        // Sapphire with varying alpha for borders/separators
        static readonly Color ColBorderSapphire = new Color32(0x2a, 0x4a, 0x8a, 0x59); // 35%
        static readonly Color ColSepFaint       = new Color32(0x2a, 0x4a, 0x8a, 0x26); // 15%

        // ---- fonts (assigned in Build()) ----
        static TMP_FontAsset cinzel, cinzelDecor, cormorant, cormorantItalic;

        // ---- chrome sprites (assigned in Build()) ----
        static Sprite sprPanelFrame, sprMandalaBg, sprPortraitFrame;
        static Sprite sprHpFillGreen, sprHpFillYellow, sprHpFillRed;
        static Sprite sprAffinityBadge, sprAffinityIconAgni;
        static Sprite sprBondPipLit, sprBondPipUnlit, sprBondPipEncounter;
        static Sprite sprPageDotActive, sprPageDotInactive, sprNotifBadge;
        static Sprite sprDiyaMemorial, sprShapathIcon, sprThresholdMark;
        static Sprite sprAffinityAgni, sprAffinityJal, sprAffinityVayu, sprAffinityPrithvi, sprAffinityAkasha;
        static Sprite sprIconWeapon, sprIconItem;

        // ---- TMP glow materials (assigned in Build()) ----
        static Material matCharNameGlow, matPageHeaderGlow;
        static Material matWeaponRankAccess, matWeaponRankDefault;

        // ---- UI material (desaturation for dead portraits) ----
        static Material matDesaturated;

        // ==================================================================
        // entry point
        // ==================================================================

        [MenuItem("Project Astra/Build Unit Info Panel")]
        public static void Build()
        {
            // §0.5 dim assertion — if CanvasWidth/Height/IsFullScreen are ever edited away
            // from 1920×1080 full-screen without updating the canvas, catch it here.
            if (IsFullScreen && (Mathf.Abs(CanvasWidth - 1920f) > 1f || Mathf.Abs(CanvasHeight - 1080f) > 1f))
                Debug.LogError("UnitInfoPanel dims don't match Unity canvas reference (1920×1080). Fix constants.");

            var activeScene = EditorSceneManager.GetActiveScene();
            if (activeScene.name != "BattleMap")
                Debug.LogWarning($"Building UnitInfoPanel into scene '{activeScene.name}' — expected BattleMap. Continuing.");

            cinzel          = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(FontDir + "Cinzel SDF.asset");
            cinzelDecor     = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(FontDir + "CinzelDecorative SDF.asset");
            cormorant       = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(FontDir + "CormorantGaramond SDF.asset");
            cormorantItalic = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(FontDir + "CormorantGaramondItalic SDF.asset");

            // Chrome sprites (Figma source — see docs/UI_WORKFLOW.md §4.5)
            sprPanelFrame       = LoadFrame("panel_frame.png");
            sprMandalaBg        = LoadFrame("mandala_bg.png");
            sprPortraitFrame    = LoadFrame("portrait_frame.png");
            sprHpFillGreen      = LoadFrame("hp_bar_fill_green.png");
            sprHpFillYellow     = LoadFrame("hp_bar_fill_yellow.png");
            sprHpFillRed        = LoadFrame("hp_bar_fill_red.png");
            sprAffinityBadge    = LoadFrame("affinity_badge.png");
            sprAffinityIconAgni = LoadFrame("affinity_icon_agni.png");
            sprBondPipLit       = LoadFrame("bond_pip_lit.png");
            sprBondPipUnlit     = LoadFrame("bond_pip_unlit.png");
            sprBondPipEncounter = LoadFrame("bond_pip_encounter.png");
            sprPageDotActive    = LoadFrame("page_dot_active.png");
            sprNotifBadge       = LoadFrame("notif_badge.png");
            sprDiyaMemorial     = LoadFrame("diya_memorial.png");
            sprShapathIcon      = LoadFrame("shapath_icon.png");
            sprThresholdMark    = LoadFrame("threshold_mark.png");
            sprAffinityAgni     = LoadFrame("affinity_agni.png");
            sprAffinityJal      = LoadFrame("affinity_jal.png");
            sprAffinityVayu     = LoadFrame("affinity_vayu.png");
            sprAffinityPrithvi  = LoadFrame("affinity_prithvi.png");
            sprAffinityAkasha   = LoadFrame("affinity_akasha.png");
            sprIconWeapon       = LoadSprite("icon_weapon_generic.png");
            sprIconItem         = LoadSprite("icon_item_generic.png");

            matDesaturated      = AssetDatabase.LoadAssetAtPath<Material>("Assets/UI/UnitInfoPanel/Materials/UIDesaturated.mat");

            if (sprPanelFrame == null)
                Debug.LogWarning("panel_frame.png missing — panel will render with flat-color fallback.");

            // TMP glow materials (see UnitInfoPanelMaterials.cs — run the menu item once to generate)
            matCharNameGlow      = AssetDatabase.LoadAssetAtPath<Material>(UnitInfoPanelMaterials.CharNameGlow);
            matPageHeaderGlow    = AssetDatabase.LoadAssetAtPath<Material>(UnitInfoPanelMaterials.PageHeaderGlow);
            matWeaponRankAccess  = AssetDatabase.LoadAssetAtPath<Material>(UnitInfoPanelMaterials.WeaponRankAccessGlow);
            matWeaponRankDefault = AssetDatabase.LoadAssetAtPath<Material>(UnitInfoPanelMaterials.WeaponRankDefaultUL);
            if (matCharNameGlow == null)
                Debug.LogWarning("Glow materials missing — run 'Project Astra/Generate UnitInfo Glow Materials' first.");

            var canvas = EnsureCanvas();

            var existingOverlay = canvas.transform.Find("UnitInfoDimOverlay");
            if (existingOverlay != null) Object.DestroyImmediate(existingOverlay.gameObject);
            var existing = canvas.transform.Find("UnitInfoPanel");
            if (existing != null) Object.DestroyImmediate(existing.gameObject);

            var overlay = BuildDimOverlay(canvas.transform);
            var panel = BuildPanel(canvas.transform);

            // Attach runtime controller
            var controller = panel.GetComponent<Core.UI.UnitInfoPanelUI>();
            if (controller == null)
                controller = panel.AddComponent<Core.UI.UnitInfoPanelUI>();

            // Sub-panels — item detail & support detail (UI-02 pages 2 & 3 selection)
            var itemDetail = BuildItemDetailSubpanel(panel.transform);
            var supportDetail = BuildSupportDetailSubpanel(panel.transform);

            // Wire sprite assets onto the controller's SerializeFields so runtime sprite-swap works.
            WireControllerSprites(controller);
            WireDetailReferences(controller, itemDetail, supportDetail);

            // Auto-wire GridCursor._unitInfoPanelUI so the cursor's Open-Info key hits this instance.
            WireGridCursor(controller);

            EditorSceneManager.MarkSceneDirty(activeScene);
            Selection.activeGameObject = panel;
            Debug.Log("UnitInfoPanel (Nila Dharma) built.");
        }

        static void WireControllerSprites(Core.UI.UnitInfoPanelUI controller)
        {
            var so = new SerializedObject(controller);
            AssignSprite(so, "_hpFillGreen",     sprHpFillGreen);
            AssignSprite(so, "_hpFillYellow",    sprHpFillYellow);
            AssignSprite(so, "_hpFillRed",       sprHpFillRed);
            AssignSprite(so, "_pageDotActive",   sprPageDotActive);
            AssignSprite(so, "_pageDotInactive", sprPageDotInactive);
            AssignSprite(so, "_bondPipLit",      sprBondPipLit);
            AssignSprite(so, "_bondPipUnlit",    sprBondPipUnlit);
            AssignSprite(so, "_bondPipEncounter",sprBondPipEncounter);
            AssignSprite(so, "_notifBadge",      sprNotifBadge);
            AssignSprite(so, "_diyaMemorial",    sprDiyaMemorial);
            AssignSprite(so, "_shapathIcon",     sprShapathIcon);
            AssignSprite(so, "_thresholdMark",   sprThresholdMark);

            // Affinity icons indexed by PanchaBhuta enum (1..5; 0 = None unused).
            var affProp = so.FindProperty("_affinityIcons");
            if (affProp != null)
            {
                affProp.arraySize = 5;
                affProp.GetArrayElementAtIndex(0).objectReferenceValue = sprAffinityAgni;
                affProp.GetArrayElementAtIndex(1).objectReferenceValue = sprAffinityJal;
                affProp.GetArrayElementAtIndex(2).objectReferenceValue = sprAffinityVayu;
                affProp.GetArrayElementAtIndex(3).objectReferenceValue = sprAffinityPrithvi;
                affProp.GetArrayElementAtIndex(4).objectReferenceValue = sprAffinityAkasha;
            }

            var matProp = so.FindProperty("_desaturatedMaterial");
            if (matProp != null) matProp.objectReferenceValue = matDesaturated;

            so.ApplyModifiedPropertiesWithoutUndo();
        }

        static void AssignSprite(SerializedObject so, string propName, Sprite value)
        {
            var prop = so.FindProperty(propName);
            if (prop != null) prop.objectReferenceValue = value;
        }

        static Core.UI.UnitInfoItemDetailUI BuildItemDetailSubpanel(Transform parent)
        {
            var root = NewImage("ItemDetailPanel", parent, ColIndigo);
            var rt = root.GetComponent<RectTransform>();
            rt.anchorMin = new Vector2(0.5f, 0.5f);
            rt.anchorMax = new Vector2(0.5f, 0.5f);
            rt.pivot = new Vector2(0.5f, 0.5f);
            rt.anchoredPosition = Vector2.zero;
            rt.sizeDelta = new Vector2(900, 600);

            // Sapphire border ring
            // Outer sapphire ring achieved by inset-covering the root with an inner indigo fill.
            root.GetComponent<Image>().color = ColSapphire;
            var innerFill = NewImage("InnerFill", rt, ColIndigo);
            var ifRt = innerFill.GetComponent<RectTransform>();
            ifRt.anchorMin = Vector2.zero; ifRt.anchorMax = Vector2.one;
            ifRt.offsetMin = new Vector2(2, 2); ifRt.offsetMax = new Vector2(-2, -2);
            innerFill.GetComponent<Image>().raycastTarget = false;

            // Title
            var title = NewText("Name", rt, "Item Name", cinzel, 40, ColIvory,
                TextAlignmentOptions.MidlineLeft, FontStyles.Bold);
            title.characterSpacing = 4;
            if (matCharNameGlow != null) title.fontMaterial = matCharNameGlow;
            var tRt = title.rectTransform;
            tRt.anchorMin = new Vector2(0, 1);
            tRt.anchorMax = new Vector2(1, 1);
            tRt.pivot = new Vector2(0.5f, 1);
            tRt.anchoredPosition = new Vector2(0, -24);
            tRt.sizeDelta = new Vector2(-60, 52);

            // Type subtitle
            var type = NewText("Type", rt, "WEAPON · SWORD", cinzel, 18, ColSilver,
                TextAlignmentOptions.MidlineLeft, FontStyles.Normal);
            type.characterSpacing = 6;
            var tyRt = type.rectTransform;
            tyRt.anchorMin = new Vector2(0, 1);
            tyRt.anchorMax = new Vector2(1, 1);
            tyRt.pivot = new Vector2(0.5f, 1);
            tyRt.anchoredPosition = new Vector2(0, -78);
            tyRt.sizeDelta = new Vector2(-60, 24);

            // Stat grid — 3 columns × 2 rows
            string[] labels = { "Might", "Hit", "Crit", "Weight", "Range", "Rank" };
            string[] fieldNames = { "MightVal", "HitVal", "CritVal", "WeightVal", "RangeVal", "RankVal" };
            var statTmps = new TextMeshProUGUI[6];
            for (int i = 0; i < 6; i++)
            {
                int col = i % 3;
                int row = i / 3;
                var cell = NewRect("Cell_" + labels[i], rt);
                cell.anchorMin = new Vector2(col / 3f, 1);
                cell.anchorMax = new Vector2((col + 1) / 3f, 1);
                cell.pivot = new Vector2(0.5f, 1);
                cell.anchoredPosition = new Vector2(0, -140 - row * 80);
                cell.sizeDelta = new Vector2(0, 70);
                var lab = NewText("Label", cell, labels[i].ToUpper(), cinzel, 16, ColSilver,
                    TextAlignmentOptions.Top, FontStyles.Normal);
                lab.characterSpacing = 4;
                var lRt = lab.rectTransform;
                lRt.anchorMin = new Vector2(0, 1); lRt.anchorMax = new Vector2(1, 1); lRt.pivot = new Vector2(0.5f, 1);
                lRt.anchoredPosition = Vector2.zero; lRt.sizeDelta = new Vector2(0, 22);
                var val = NewText(fieldNames[i], cell, "—", cormorant, 32, ColIvory,
                    TextAlignmentOptions.Bottom, FontStyles.Bold);
                var vRt = val.rectTransform;
                vRt.anchorMin = new Vector2(0, 0); vRt.anchorMax = new Vector2(1, 0); vRt.pivot = new Vector2(0.5f, 0);
                vRt.anchoredPosition = Vector2.zero; vRt.sizeDelta = new Vector2(0, 36);
                statTmps[i] = val;
            }

            // Effectiveness
            var eff = NewText("Effectiveness", rt, "", cormorantItalic, 22, ColGreen,
                TextAlignmentOptions.MidlineLeft, FontStyles.Italic);
            var eRt = eff.rectTransform;
            eRt.anchorMin = new Vector2(0, 0); eRt.anchorMax = new Vector2(1, 0); eRt.pivot = new Vector2(0.5f, 0);
            eRt.anchoredPosition = new Vector2(0, 150); eRt.sizeDelta = new Vector2(-60, 30);

            // Special
            var spec = NewText("Special", rt, "", cormorant, 22, ColIvory,
                TextAlignmentOptions.MidlineLeft, FontStyles.Normal);
            var sRt = spec.rectTransform;
            sRt.anchorMin = new Vector2(0, 0); sRt.anchorMax = new Vector2(1, 0); sRt.pivot = new Vector2(0.5f, 0);
            sRt.anchoredPosition = new Vector2(0, 110); sRt.sizeDelta = new Vector2(-60, 30);

            // Description (consumable)
            var desc = NewText("Description", rt, "", cormorant, 22, ColIvory,
                TextAlignmentOptions.MidlineLeft, FontStyles.Normal);
            var dRt = desc.rectTransform;
            dRt.anchorMin = new Vector2(0, 0); dRt.anchorMax = new Vector2(1, 0); dRt.pivot = new Vector2(0.5f, 0);
            dRt.anchoredPosition = new Vector2(0, 70); dRt.sizeDelta = new Vector2(-60, 30);

            var comp = root.AddComponent<Core.UI.UnitInfoItemDetailUI>();
            var so = new SerializedObject(comp);
            so.FindProperty("_nameText").objectReferenceValue = title;
            so.FindProperty("_typeText").objectReferenceValue = type;
            so.FindProperty("_mightText").objectReferenceValue  = statTmps[0];
            so.FindProperty("_hitText").objectReferenceValue    = statTmps[1];
            so.FindProperty("_critText").objectReferenceValue   = statTmps[2];
            so.FindProperty("_weightText").objectReferenceValue = statTmps[3];
            so.FindProperty("_rangeText").objectReferenceValue  = statTmps[4];
            so.FindProperty("_rankReqText").objectReferenceValue= statTmps[5];
            so.FindProperty("_effectivenessText").objectReferenceValue = eff;
            so.FindProperty("_specialText").objectReferenceValue = spec;
            so.FindProperty("_descriptionText").objectReferenceValue = desc;
            so.ApplyModifiedPropertiesWithoutUndo();

            root.SetActive(false);
            return comp;
        }

        static Core.UI.UnitInfoSupportDetailUI BuildSupportDetailSubpanel(Transform parent)
        {
            var root = NewImage("SupportDetailPanel", parent, ColIndigo);
            var rt = root.GetComponent<RectTransform>();
            rt.anchorMin = new Vector2(0.5f, 0.5f);
            rt.anchorMax = new Vector2(0.5f, 0.5f);
            rt.pivot = new Vector2(0.5f, 0.5f);
            rt.anchoredPosition = Vector2.zero;
            rt.sizeDelta = new Vector2(900, 640);

            // Outer sapphire ring achieved by inset-covering the root with an inner indigo fill.
            root.GetComponent<Image>().color = ColSapphire;
            var innerFill = NewImage("InnerFill", rt, ColIndigo);
            var ifRt = innerFill.GetComponent<RectTransform>();
            ifRt.anchorMin = Vector2.zero; ifRt.anchorMax = Vector2.one;
            ifRt.offsetMin = new Vector2(2, 2); ifRt.offsetMax = new Vector2(-2, -2);
            innerFill.GetComponent<Image>().raycastTarget = false;

            // Partner portrait
            var portrait = NewImage("PartnerPortrait", rt, new Color(1, 1, 1, 0));
            var pRt = portrait.GetComponent<RectTransform>();
            pRt.anchorMin = new Vector2(0, 1); pRt.anchorMax = new Vector2(0, 1); pRt.pivot = new Vector2(0, 1);
            pRt.anchoredPosition = new Vector2(40, -40);
            pRt.sizeDelta = new Vector2(120, 120);
            portrait.GetComponent<Image>().preserveAspect = true;

            // Name
            var name = NewText("PartnerName", rt, "Partner", cinzel, 36, ColIvory,
                TextAlignmentOptions.TopLeft, FontStyles.Bold);
            name.characterSpacing = 4;
            if (matCharNameGlow != null) name.fontMaterial = matCharNameGlow;
            var nRt = name.rectTransform;
            nRt.anchorMin = new Vector2(0, 1); nRt.anchorMax = new Vector2(1, 1); nRt.pivot = new Vector2(0, 1);
            nRt.anchoredPosition = new Vector2(180, -44);
            nRt.sizeDelta = new Vector2(-220, 50);

            // Bonus grid — 6 rows of "Label +N"
            string[] bonusLabels = { "Atk", "Def", "Hit", "Avo", "Crit", "CritAvo" };
            string[] bonusFields = { "AtkVal", "DefVal", "HitVal", "AvoVal", "CritVal", "CritAvoVal" };
            var bonusTmps = new TextMeshProUGUI[6];
            for (int i = 0; i < 6; i++)
            {
                int col = i % 3;
                int row = i / 3;
                var tmp = NewText(bonusFields[i], rt, bonusLabels[i] + " +0", cormorant, 28, ColGreen,
                    TextAlignmentOptions.MidlineLeft, FontStyles.Bold);
                var bRt = tmp.rectTransform;
                bRt.anchorMin = new Vector2(col / 3f, 1); bRt.anchorMax = new Vector2((col + 1) / 3f, 1); bRt.pivot = new Vector2(0, 1);
                bRt.anchoredPosition = new Vector2(32, -200 - row * 56);
                bRt.sizeDelta = new Vector2(-44, 40);
                bonusTmps[i] = tmp;
            }

            // Promise container
            var promiseContainer = NewImage("PromiseContainer", rt, new Color32(0xc8, 0xa0, 0x40, 0x20));
            var pcRt = promiseContainer.GetComponent<RectTransform>();
            pcRt.anchorMin = new Vector2(0, 0); pcRt.anchorMax = new Vector2(1, 0); pcRt.pivot = new Vector2(0.5f, 0);
            pcRt.anchoredPosition = new Vector2(0, 100); pcRt.sizeDelta = new Vector2(-60, 100);
            var promise = NewText("PromiseText", pcRt, "", cormorantItalic, 22, ColGold,
                TextAlignmentOptions.Midline, FontStyles.Italic);
            var prRt = promise.rectTransform;
            prRt.anchorMin = Vector2.zero; prRt.anchorMax = Vector2.one;
            prRt.offsetMin = new Vector2(16, 8); prRt.offsetMax = new Vector2(-16, -8);
            promise.enableWordWrapping = true;
            promiseContainer.SetActive(false);

            // Shapath icon
            var shapath = NewImage("ShapathIcon", rt, Color.white);
            var shRt = shapath.GetComponent<RectTransform>();
            shRt.anchorMin = new Vector2(1, 0); shRt.anchorMax = new Vector2(1, 0); shRt.pivot = new Vector2(1, 0);
            shRt.anchoredPosition = new Vector2(-30, 40); shRt.sizeDelta = new Vector2(36, 36);
            if (sprShapathIcon != null) shapath.GetComponent<Image>().sprite = sprShapathIcon;
            shapath.SetActive(false);

            var comp = root.AddComponent<Core.UI.UnitInfoSupportDetailUI>();
            var so = new SerializedObject(comp);
            so.FindProperty("_portraitImage").objectReferenceValue = portrait.GetComponent<Image>();
            so.FindProperty("_nameText").objectReferenceValue = name;
            so.FindProperty("_atkText").objectReferenceValue     = bonusTmps[0];
            so.FindProperty("_defText").objectReferenceValue     = bonusTmps[1];
            so.FindProperty("_hitText").objectReferenceValue     = bonusTmps[2];
            so.FindProperty("_avoText").objectReferenceValue     = bonusTmps[3];
            so.FindProperty("_critText").objectReferenceValue    = bonusTmps[4];
            so.FindProperty("_critAvoText").objectReferenceValue = bonusTmps[5];
            so.FindProperty("_promiseContainer").objectReferenceValue = promiseContainer;
            so.FindProperty("_promiseText").objectReferenceValue = promise;
            so.FindProperty("_shapathIcon").objectReferenceValue = shapath;
            so.ApplyModifiedPropertiesWithoutUndo();

            root.SetActive(false);
            return comp;
        }

        static void WireDetailReferences(Core.UI.UnitInfoPanelUI controller,
            Core.UI.UnitInfoItemDetailUI itemDetail, Core.UI.UnitInfoSupportDetailUI supportDetail)
        {
            var so = new SerializedObject(controller);
            var itemProp = so.FindProperty("_itemDetail");
            if (itemProp != null) itemProp.objectReferenceValue = itemDetail;
            var supProp = so.FindProperty("_supportDetail");
            if (supProp != null) supProp.objectReferenceValue = supportDetail;
            so.ApplyModifiedPropertiesWithoutUndo();
        }

        static void WireGridCursor(Core.UI.UnitInfoPanelUI controller)
        {
            var cursor = Object.FindObjectOfType<ProjectAstra.Core.GridCursor>();
            if (cursor == null) { Debug.LogWarning("No GridCursor in scene — UnitInfoPanel opened-by-cursor wiring skipped."); return; }
            var so = new SerializedObject(cursor);
            var prop = so.FindProperty("_unitInfoPanelUI");
            if (prop == null) { Debug.LogWarning("GridCursor._unitInfoPanelUI field not found."); return; }
            prop.objectReferenceValue = controller;
            so.ApplyModifiedPropertiesWithoutUndo();
            Debug.Log("Wired GridCursor._unitInfoPanelUI → new UnitInfoPanelUI.");
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

        // ==================================================================
        // dim overlay
        // ==================================================================

        static GameObject BuildDimOverlay(Transform parent)
        {
            var go = NewImage("UnitInfoDimOverlay", parent, ColDimOverlay);
            SetCenter(go, CanvasWidth, CanvasHeight);
            go.GetComponent<Image>().raycastTarget = true;
            go.SetActive(false);
            return go;
        }

        // ==================================================================
        // panel — outer frame
        // ==================================================================

        static GameObject BuildPanel(Transform parent)
        {
            // panel_frame.png (1920×1080) bakes: indigo fill + sapphire border at inset 12 + outer glow
            //                                    + inner glow + sapphire outer ring + gold corner marks.
            var outer = NewImage("UnitInfoPanel", parent, Color.white);
            SetCenter(outer, CanvasWidth, CanvasHeight);
            outer.SetActive(false);
            var outerImg = outer.GetComponent<Image>();
            if (sprPanelFrame != null) outerImg.sprite = sprPanelFrame;
            else                        outerImg.color  = ColIndigo;
            outerImg.type = Image.Type.Simple;

            // Mandala backdrop sits inside the border, behind content.
            if (sprMandalaBg != null)
            {
                var mandala = NewImage("MandalaBg", outer.transform, Color.white);
                var mRt = mandala.GetComponent<RectTransform>();
                mRt.anchorMin = Vector2.zero;
                mRt.anchorMax = Vector2.one;
                mRt.offsetMin = new Vector2(14, 14);
                mRt.offsetMax = new Vector2(-14, -14);
                mandala.GetComponent<Image>().sprite = sprMandalaBg;
            }

            // Content area — inset 32px from visible border (which is at inset 12).
            var content = NewRect("Content", outer.transform);
            SetStretch(content, 0);
            content.offsetMin = new Vector2(44, 44);
            content.offsetMax = new Vector2(-44, -44);

            BuildLeftSection(content);
            BuildPageContainer(content);
            BuildPageIndicator(content);

            return outer;
        }

        // ==================================================================
        // left section (shared across all pages)
        // ==================================================================

        static void BuildLeftSection(RectTransform parent)
        {
            var left = NewRect("LeftSection", parent);
            left.anchorMin = new Vector2(0, 0);
            left.anchorMax = new Vector2(0, 1);
            left.pivot = new Vector2(0, 1);
            left.anchoredPosition = Vector2.zero;
            left.sizeDelta = new Vector2(460, 0);

            // Vertical separator line on right edge
            var sep = NewImage("Separator", left, ColSapphire);
            var sepRt = sep.GetComponent<RectTransform>();
            sepRt.anchorMin = new Vector2(1, 0.05f);
            sepRt.anchorMax = new Vector2(1, 0.95f);
            sepRt.pivot = new Vector2(1, 0.5f);
            sepRt.sizeDelta = new Vector2(2, 0);
            sepRt.anchoredPosition = Vector2.zero;

            BuildPortraitFrame(left);
            BuildNameBlock(left);
            BuildClassBlock(left);

            // Personality label — CC-01, displayed only for allied NPCs. Hidden by default.
            var personality = NewText("PersonalityLabel", left, "PERSONALITY: VIRA",
                cinzel, 16, ColSilver, TextAlignmentOptions.Center, FontStyles.Normal);
            personality.characterSpacing = 4;
            var prt = personality.rectTransform;
            prt.anchorMin = new Vector2(0, 1);
            prt.anchorMax = new Vector2(1, 1);
            prt.pivot = new Vector2(0.5f, 1);
            prt.anchoredPosition = new Vector2(0, -654); // below ClassBlock (at -466 + 180 = -646 bottom, +8px margin)
            prt.sizeDelta = new Vector2(0, 22);
            personality.gameObject.SetActive(false);
        }

        static void BuildPortraitFrame(RectTransform parent)
        {
            // portrait_frame.png is 440×440 native: 380×380 visible + 30px halo padding.
            // Size the GameObject to the native texture so the outer glow renders.
            var frame = NewImage("PortraitFrame", parent, Color.white);
            var rt = frame.GetComponent<RectTransform>();
            rt.anchorMin = new Vector2(0.5f, 1);
            rt.anchorMax = new Vector2(0.5f, 1);
            rt.pivot = new Vector2(0.5f, 1);
            rt.anchoredPosition = new Vector2(0, 12); // shift up by pad so visible area starts near the top
            rt.sizeDelta = new Vector2(440, 440);

            var img = frame.GetComponent<Image>();
            if (sprPortraitFrame != null) img.sprite = sprPortraitFrame;
            else                           img.color  = ColIndigoMid;
            img.type = Image.Type.Simple;

            // Portrait placeholder — inside the visible 380×380 area (30px halo padding).
            var placeholder = NewImage("PortraitPlaceholder", rt, new Color(1, 1, 1, 0));
            var placeRt = placeholder.GetComponent<RectTransform>();
            placeRt.anchorMin = Vector2.zero;
            placeRt.anchorMax = Vector2.one;
            placeRt.offsetMin = new Vector2(34, 34);
            placeRt.offsetMax = new Vector2(-34, -34);
            placeholder.GetComponent<Image>().preserveAspect = true;

            // Stress overlay — stacked on top of the portrait; shown at StressTier >= 1.
            var stressOverlay = NewImage("StressOverlay", placeRt, new Color(1, 1, 1, 0));
            SetStretch(stressOverlay, 0);
            stressOverlay.GetComponent<Image>().raycastTarget = false;
            stressOverlay.SetActive(false);

            // Affinity badge — sits in the bottom-right of the visible area.
            var badge = NewImage("AffinityBadge", rt, Color.white);
            var badgeRt = badge.GetComponent<RectTransform>();
            badgeRt.anchorMin = new Vector2(1, 0);
            badgeRt.anchorMax = new Vector2(1, 0);
            badgeRt.pivot = new Vector2(1, 0);
            badgeRt.anchoredPosition = new Vector2(-34, 34); // pull in past the halo padding
            badgeRt.sizeDelta = new Vector2(112, 46); // sprite native size
            var badgeImg = badge.GetComponent<Image>();
            if (sprAffinityBadge != null) badgeImg.sprite = sprAffinityBadge;
            else                           badgeImg.color  = new Color32(0x0a, 0x0e, 0x2a, 0xe6);

            // Affinity icon — small element inside the badge
            var affinIcon = NewImage("AffinIcon", badge.transform, Color.white);
            var aiRt = affinIcon.GetComponent<RectTransform>();
            aiRt.anchorMin = new Vector2(0, 0.5f);
            aiRt.anchorMax = new Vector2(0, 0.5f);
            aiRt.pivot = new Vector2(0, 0.5f);
            aiRt.anchoredPosition = new Vector2(9, 0);
            aiRt.sizeDelta = new Vector2(40, 40);
            var aiImg = affinIcon.GetComponent<Image>();
            if (sprAffinityIconAgni != null) aiImg.sprite = sprAffinityIconAgni;
            else                              aiImg.color  = new Color32(0xe8, 0x60, 0x30, 0xff);

            // Affinity label — right of the icon, centered in the badge's visible interior.
            var affinLabel = NewText("AffinLabel", badge.transform, "Agni", cinzel, 14, ColIvory,
                TextAlignmentOptions.MidlineLeft, FontStyles.Normal);
            affinLabel.characterSpacing = 3;
            var alRt = affinLabel.rectTransform;
            alRt.anchorMin = new Vector2(0, 0);
            alRt.anchorMax = new Vector2(1, 1);
            alRt.offsetMin = new Vector2(52, 6);
            alRt.offsetMax = new Vector2(-10, -6);
        }

        static void BuildNameBlock(RectTransform parent)
        {
            var nameText = NewText("UnitName", parent, "Arjuna", cinzel, 44, ColIvory,
                TextAlignmentOptions.Center, FontStyles.Bold);
            nameText.characterSpacing = 6;
            if (matCharNameGlow != null) nameText.fontMaterial = matCharNameGlow;
            var rt = nameText.rectTransform;
            rt.anchorMin = new Vector2(0, 1);
            rt.anchorMax = new Vector2(1, 1);
            rt.pivot = new Vector2(0.5f, 1);
            rt.anchoredPosition = new Vector2(0, -404);
            rt.sizeDelta = new Vector2(0, 54);
        }

        static void BuildClassBlock(RectTransform parent)
        {
            var block = NewImage("ClassBlock", parent, new Color32(0x14, 0x18, 0x40, 0x80));
            var rt = block.GetComponent<RectTransform>();
            rt.anchorMin = new Vector2(0.05f, 1);
            rt.anchorMax = new Vector2(0.95f, 1);
            rt.pivot = new Vector2(0.5f, 1);
            rt.anchoredPosition = new Vector2(0, -466);
            rt.sizeDelta = new Vector2(0, 180);

            // Class name row
            var className = NewText("ClassName", rt, "Kshatriya", cinzel, 26, ColGold,
                TextAlignmentOptions.TopLeft, FontStyles.Bold);
            className.characterSpacing = 4;
            var cnRt = className.rectTransform;
            cnRt.anchorMin = new Vector2(0, 1);
            cnRt.anchorMax = new Vector2(1, 1);
            cnRt.pivot = new Vector2(0, 1);
            cnRt.anchoredPosition = new Vector2(14, -10);
            cnRt.sizeDelta = new Vector2(-28, 34);

            // Lv / EXP row
            var lvRow = NewRect("LvRow", rt);
            lvRow.anchorMin = new Vector2(0, 1);
            lvRow.anchorMax = new Vector2(1, 1);
            lvRow.pivot = new Vector2(0, 1);
            lvRow.anchoredPosition = new Vector2(14, -48);
            lvRow.sizeDelta = new Vector2(-28, 34);

            var lvLayout = lvRow.gameObject.AddComponent<HorizontalLayoutGroup>();
            lvLayout.childAlignment = TextAnchor.MiddleLeft;
            lvLayout.spacing = 8;
            lvLayout.childForceExpandWidth = false;
            lvLayout.childForceExpandHeight = false;
            lvLayout.childControlWidth = true;
            lvLayout.childControlHeight = true;

            AddFittedText(lvRow, "LvLabel", "LV", cinzel, 16, ColSilver);
            AddFittedText(lvRow, "LvValue", "12", cormorant, 28, ColIvory);
            AddFittedText(lvRow, "ExpLabel", "EXP", cinzel, 16, ColSilver);
            AddFittedText(lvRow, "ExpValue", "76 / 100", cormorant, 28, ColIvory);

            // HP row
            var hpRow = NewRect("HpRow", rt);
            hpRow.anchorMin = new Vector2(0, 1);
            hpRow.anchorMax = new Vector2(1, 1);
            hpRow.pivot = new Vector2(0, 1);
            hpRow.anchoredPosition = new Vector2(14, -86);
            hpRow.sizeDelta = new Vector2(-28, 30);

            var hpLayout = hpRow.gameObject.AddComponent<HorizontalLayoutGroup>();
            hpLayout.childAlignment = TextAnchor.MiddleLeft;
            hpLayout.spacing = 8;
            hpLayout.childForceExpandWidth = false;
            hpLayout.childForceExpandHeight = false;
            hpLayout.childControlWidth = true;
            hpLayout.childControlHeight = true;

            AddFittedText(hpRow, "HpLabel", "HP", cinzel, 16, ColSilver);
            AddFittedText(hpRow, "HpCurrent", "38", cormorant, 28, ColIvory);
            AddFittedText(hpRow, "HpSep", "/", cormorant, 28, ColSilver);
            AddFittedText(hpRow, "HpMax", "45", cormorant, 28, ColIvory);

            // HP bar
            var hpBarOuter = NewImage("HpBarOuter", rt, new Color(0, 0, 0, 0.5f));
            var hbRt = hpBarOuter.GetComponent<RectTransform>();
            hbRt.anchorMin = new Vector2(0, 1);
            hbRt.anchorMax = new Vector2(1, 1);
            hbRt.pivot = new Vector2(0, 1);
            hbRt.anchoredPosition = new Vector2(14, -120);
            hbRt.sizeDelta = new Vector2(-28, 18);

            var hpBarFill = NewImage("HpBarFill", hbRt, Color.white);
            var hfRt = hpBarFill.GetComponent<RectTransform>();
            hfRt.anchorMin = Vector2.zero;
            hfRt.anchorMax = Vector2.one;
            hfRt.offsetMin = Vector2.zero;
            hfRt.offsetMax = Vector2.zero;
            var hpFillImg = hpBarFill.GetComponent<Image>();
            if (sprHpFillGreen != null)
            {
                hpFillImg.sprite = sprHpFillGreen;
                hpFillImg.type = Image.Type.Filled;
                hpFillImg.fillMethod = Image.FillMethod.Horizontal;
                hpFillImg.fillOrigin = (int)Image.OriginHorizontal.Left;
                hpFillImg.fillAmount = 0.844f;
            }
            else
            {
                // Fallback: color tint, shrink via anchor
                hpFillImg.color = ColGreen;
                hfRt.anchorMax = new Vector2(0.844f, 1);
            }

            // Class icon placeholder
            var classIcon = NewImage("ClassIcon", rt, ColGoldFaint);
            var ciRt = classIcon.GetComponent<RectTransform>();
            ciRt.anchorMin = new Vector2(1, 1);
            ciRt.anchorMax = new Vector2(1, 1);
            ciRt.pivot = new Vector2(1, 1);
            ciRt.sizeDelta = new Vector2(60, 60);
            ciRt.anchoredPosition = new Vector2(-14, -10);
            if (sprIconWeapon != null)
            {
                classIcon.GetComponent<Image>().sprite = sprIconWeapon;
                classIcon.GetComponent<Image>().preserveAspect = true;
            }
        }

        // ==================================================================
        // page container (right section, holds 3 pages)
        // ==================================================================

        static void BuildPageContainer(RectTransform parent)
        {
            var container = NewRect("PageContainer", parent);
            container.anchorMin = new Vector2(0, 0);
            container.anchorMax = new Vector2(1, 1);
            container.pivot = new Vector2(0.5f, 0.5f);
            container.offsetMin = new Vector2(494, 0); // 460 left col + 34 padding
            container.offsetMax = Vector2.zero;

            BuildStatsPage(container);
            BuildInventoryPage(container);
            BuildSupportsPage(container);
        }

        // ==================================================================
        // page 1 — Stats (Personal Data)
        // ==================================================================

        static void BuildStatsPage(RectTransform parent)
        {
            var page = NewRect("StatsPage", parent);
            SetStretch(page, 0);
            page.gameObject.SetActive(true);

            // Header
            BuildPageHeader(page, "Personal Data", "1 / 3", 0);

            // Stat grid — 2 columns
            var gridY = -60;
            var grid = NewRect("StatGrid", page);
            grid.anchorMin = new Vector2(0, 1);
            grid.anchorMax = new Vector2(1, 1);
            grid.pivot = new Vector2(0, 1);
            grid.anchoredPosition = new Vector2(0, gridY);
            grid.sizeDelta = new Vector2(0, 310);

            // Left column: Str, Mag, Skl, Spd, Niyati
            string[] leftLabels = { "Str", "Mag", "Skl", "Spd", "Niyati" };
            string[] leftValues = { "18", "6", "22", "21", "14" };
            // Right column: Def, Res, Con, Mov
            string[] rightLabels = { "Def", "Res", "Con", "Mov" };
            string[] rightValues = { "11", "8", "9", "6" };

            float rowH = 54;
            for (int i = 0; i < leftLabels.Length; i++)
            {
                float y = -(i * rowH);
                BuildStatRow(grid, "StatL_" + leftLabels[i], leftLabels[i], leftValues[i],
                    new Vector2(0, y), 0f, 0.48f);
            }
            for (int i = 0; i < rightLabels.Length; i++)
            {
                float y = -(i * rowH);
                BuildStatRow(grid, "StatR_" + rightLabels[i], rightLabels[i], rightValues[i],
                    new Vector2(0, y), 0.52f, 1f);
            }

            // Derived stats block
            var derivedY = gridY - 286;
            var derived = NewImage("DerivedStats", page, new Color32(0x14, 0x18, 0x40, 0x66));
            var dRt = derived.GetComponent<RectTransform>();
            dRt.anchorMin = new Vector2(0, 1);
            dRt.anchorMax = new Vector2(1, 1);
            dRt.pivot = new Vector2(0, 1);
            dRt.anchoredPosition = new Vector2(0, derivedY);
            dRt.sizeDelta = new Vector2(0, 90);

            var derivedTitle = NewText("DerivedTitle", dRt, "COMBAT", cinzel, 16, ColSilver,
                TextAlignmentOptions.TopLeft, FontStyles.Normal);
            derivedTitle.characterSpacing = 8;
            var dtRt = derivedTitle.rectTransform;
            dtRt.anchorMin = new Vector2(0, 1);
            dtRt.anchorMax = new Vector2(1, 1);
            dtRt.pivot = new Vector2(0, 1);
            dtRt.anchoredPosition = new Vector2(14, -8);
            dtRt.sizeDelta = new Vector2(0, 22);

            string[] dLabels = { "Atk", "Hit", "Crit", "AS", "Avo" };
            string[] dValues = { "31", "140", "15", "17", "48" };
            float dColW = 1f / 5f;
            for (int i = 0; i < 5; i++)
            {
                float xMin = i * dColW;
                float xMax = xMin + dColW;
                BuildDerivedStat(dRt, dLabels[i], dValues[i], xMin, xMax);
            }

            // Weapon ranks section
            var wepY = derivedY - 100;
            var wepTitle = NewText("WeaponTitle", page, "WEAPON PROFICIENCY", cinzel, 16, ColSilver,
                TextAlignmentOptions.TopLeft, FontStyles.Normal);
            wepTitle.characterSpacing = 8;
            var wtRt = wepTitle.rectTransform;
            wtRt.anchorMin = new Vector2(0, 1);
            wtRt.anchorMax = new Vector2(1, 1);
            wtRt.pivot = new Vector2(0, 1);
            wtRt.anchoredPosition = new Vector2(0, wepY);
            wtRt.sizeDelta = new Vector2(0, 22);

            var wepGrid = NewRect("WeaponGrid", page);
            wepGrid.anchorMin = new Vector2(0, 1);
            wepGrid.anchorMax = new Vector2(1, 1);
            wepGrid.pivot = new Vector2(0, 1);
            wepGrid.anchoredPosition = new Vector2(0, wepY - 28);
            wepGrid.sizeDelta = new Vector2(0, 200);

            string[] wepNames = { "Sword", "Lance", "Bow", "Axe", "Anima", "Light", "Dark", "Staff" };
            string[] wepRanks = { "A", "C", "B", "--", "--", "--", "--", "--" };
            bool[] wepActive  = { true, true, true, false, false, false, false, false };

            for (int i = 0; i < 8; i++)
            {
                int col = i % 2;
                int row = i / 2;
                float xMin = col * 0.5f;
                float xMax = xMin + 0.5f;
                float y = -(row * 44);
                BuildWeaponRankRow(wepGrid, wepNames[i], wepRanks[i], wepActive[i], xMin, xMax, y);
            }

            // SS-12 Dharmic Threshold banner — hidden until all stat caps are reached.
            var banner = NewImage("DharmicThresholdBanner", page, new Color32(0xc8, 0xa0, 0x40, 0x33));
            var bRt = banner.GetComponent<RectTransform>();
            bRt.anchorMin = new Vector2(0, 0);
            bRt.anchorMax = new Vector2(1, 0);
            bRt.pivot = new Vector2(0.5f, 0);
            bRt.anchoredPosition = new Vector2(0, 42);
            bRt.sizeDelta = new Vector2(0, 36);
            var bannerText = NewText("BannerText", bRt, "DHARMIC THRESHOLD REACHED",
                cinzel, 18, ColGold, TextAlignmentOptions.Midline, FontStyles.Bold);
            bannerText.characterSpacing = 6;
            var btRt = bannerText.rectTransform;
            btRt.anchorMin = Vector2.zero;
            btRt.anchorMax = Vector2.one;
            btRt.offsetMin = Vector2.zero;
            btRt.offsetMax = Vector2.zero;
            if (matPageHeaderGlow != null) bannerText.fontMaterial = matPageHeaderGlow;
            banner.SetActive(false);
        }

        static void BuildStatRow(RectTransform parent, string name, string label, string value,
            Vector2 pos, float xMin, float xMax)
        {
            var row = NewRect(name, parent);
            row.anchorMin = new Vector2(xMin, 1);
            row.anchorMax = new Vector2(xMax, 1);
            row.pivot = new Vector2(0, 1);
            row.anchoredPosition = new Vector2(0, pos.y);
            row.sizeDelta = new Vector2(0, 50);

            var layout = row.gameObject.AddComponent<HorizontalLayoutGroup>();
            layout.childAlignment = TextAnchor.MiddleLeft;
            layout.spacing = 8;
            layout.padding = new RectOffset(8, 8, 0, 0);
            layout.childForceExpandWidth = false;
            layout.childForceExpandHeight = false;
            layout.childControlWidth = true;
            layout.childControlHeight = true;

            var lbl = NewText("Label", row, label.ToUpper(), cinzel, 22, ColSilver,
                TextAlignmentOptions.MidlineLeft, FontStyles.Bold);
            lbl.characterSpacing = 4;
            var lblEl = lbl.gameObject.AddComponent<LayoutElement>();
            lblEl.flexibleWidth = 1;

            var val = NewText("Value", row, value, cormorant, 38, ColIvory,
                TextAlignmentOptions.MidlineRight, FontStyles.Bold);
            var valFit = val.gameObject.AddComponent<ContentSizeFitter>();
            valFit.horizontalFit = ContentSizeFitter.FitMode.PreferredSize;
            valFit.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

            // Modifier badge (↑+2 / ↓-1) — hidden unless a temporary modifier is active.
            var mod = NewText("Mod", row, "", cinzel, 20, ColGreen,
                TextAlignmentOptions.MidlineRight, FontStyles.Bold);
            var modFit = mod.gameObject.AddComponent<ContentSizeFitter>();
            modFit.horizontalFit = ContentSizeFitter.FitMode.PreferredSize;
            modFit.verticalFit = ContentSizeFitter.FitMode.PreferredSize;
            mod.gameObject.SetActive(false);

            // Dharmic threshold marker — shown when this stat reaches its class cap (SS-12).
            var mark = NewImage("ThresholdMark", row, Color.white);
            var markRt = mark.GetComponent<RectTransform>();
            markRt.anchorMin = new Vector2(1, 0.5f);
            markRt.anchorMax = new Vector2(1, 0.5f);
            markRt.pivot = new Vector2(1, 0.5f);
            markRt.anchoredPosition = new Vector2(-4, 0);
            markRt.sizeDelta = new Vector2(28, 28);
            if (sprThresholdMark != null) mark.GetComponent<Image>().sprite = sprThresholdMark;
            mark.AddComponent<LayoutElement>().ignoreLayout = true;
            mark.SetActive(false);

            // Separator line at bottom
            var sep = NewImage("Sep", row, ColSepFaint);
            var sepRt = sep.GetComponent<RectTransform>();
            sepRt.anchorMin = new Vector2(0, 0);
            sepRt.anchorMax = new Vector2(1, 0);
            sepRt.pivot = new Vector2(0.5f, 0);
            sepRt.sizeDelta = new Vector2(0, 1);
            sepRt.anchoredPosition = Vector2.zero;
            sep.AddComponent<LayoutElement>().ignoreLayout = true;
        }

        static void BuildDerivedStat(RectTransform parent, string label, string value, float xMin, float xMax)
        {
            var col = NewRect("Derived_" + label, parent);
            col.anchorMin = new Vector2(xMin, 0);
            col.anchorMax = new Vector2(xMax, 1);
            col.pivot = new Vector2(0.5f, 0.5f);
            col.offsetMin = new Vector2(0, 8);
            col.offsetMax = new Vector2(0, -34);

            var lbl = NewText("Label", col, label.ToUpper(), cinzel, 17, ColSilver,
                TextAlignmentOptions.Top, FontStyles.Normal);
            lbl.characterSpacing = 3;
            var lblRt = lbl.rectTransform;
            lblRt.anchorMin = new Vector2(0, 1);
            lblRt.anchorMax = new Vector2(1, 1);
            lblRt.pivot = new Vector2(0.5f, 1);
            lblRt.anchoredPosition = Vector2.zero;
            lblRt.sizeDelta = new Vector2(0, 22);

            var val = NewText("Value", col, value, cormorant, 34, ColIvory,
                TextAlignmentOptions.Bottom, FontStyles.Bold);
            var valRt = val.rectTransform;
            valRt.anchorMin = new Vector2(0, 0);
            valRt.anchorMax = new Vector2(1, 0);
            valRt.pivot = new Vector2(0.5f, 0);
            valRt.anchoredPosition = Vector2.zero;
            valRt.sizeDelta = new Vector2(0, 38);
        }

        static void BuildWeaponRankRow(RectTransform parent, string weaponName, string rank,
            bool accessible, float xMin, float xMax, float y)
        {
            var row = NewImage("Wep_" + weaponName, parent,
                accessible ? new Color32(0xc8, 0xa0, 0x40, 0x0d) : new Color32(0x14, 0x18, 0x40, 0x4d));
            var rt = row.GetComponent<RectTransform>();
            rt.anchorMin = new Vector2(xMin, 1);
            rt.anchorMax = new Vector2(xMax, 1);
            rt.pivot = new Vector2(0, 1);
            float xPad = xMin > 0 ? 4 : 0;
            float xPadR = xMax < 1 ? -4 : 0;
            rt.anchoredPosition = new Vector2(xPad, y);
            rt.sizeDelta = new Vector2(xPadR - xPad, 40);

            var layout = row.AddComponent<HorizontalLayoutGroup>();
            layout.childAlignment = TextAnchor.MiddleLeft;
            layout.spacing = 8;
            layout.padding = new RectOffset(10, 10, 4, 4);
            layout.childForceExpandWidth = false;
            layout.childForceExpandHeight = false;
            layout.childControlWidth = true;
            layout.childControlHeight = true;

            Color nameCol = accessible ? ColIvory : ColSilver;
            Color rankCol = accessible ? ColGoldBright : new Color(ColSilver.r, ColSilver.g, ColSilver.b, 0.3f);

            var nameT = NewText("Name", row.transform, weaponName, cormorant, 21, nameCol,
                TextAlignmentOptions.MidlineLeft, FontStyles.Normal);
            var nameEl = nameT.gameObject.AddComponent<LayoutElement>();
            nameEl.flexibleWidth = 1;

            var rankT = NewText("Rank", row.transform, rank, cinzel, 30, rankCol,
                TextAlignmentOptions.Midline, FontStyles.Bold);
            var rankFit = rankT.gameObject.AddComponent<ContentSizeFitter>();
            rankFit.horizontalFit = ContentSizeFitter.FitMode.PreferredSize;
            rankFit.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

            var rankMat = accessible ? matWeaponRankAccess : matWeaponRankDefault;
            if (rankMat != null) rankT.fontMaterial = rankMat;

            if (accessible)
            {
                // WEXP bar
                var barBg = NewImage("WexpBarBg", row.transform, new Color(0, 0, 0, 0.4f));
                var barEl = barBg.AddComponent<LayoutElement>();
                barEl.preferredWidth = 60; barEl.preferredHeight = 8;

                var barFill = NewImage("WexpFill", barBg.transform, ColGold);
                var bfRt = barFill.GetComponent<RectTransform>();
                bfRt.anchorMin = Vector2.zero;
                bfRt.anchorMax = new Vector2(0.3f, 1);
                bfRt.offsetMin = Vector2.zero;
                bfRt.offsetMax = Vector2.zero;
            }
        }

        // ==================================================================
        // page 2 — Inventory (Items)
        // ==================================================================

        static void BuildInventoryPage(RectTransform parent)
        {
            var page = NewRect("InventoryPage", parent);
            SetStretch(page, 0);
            page.gameObject.SetActive(false);

            BuildPageHeader(page, "Items", "2 / 3", 1);

            // Items list
            var list = NewRect("ItemsList", page);
            list.anchorMin = new Vector2(0, 1);
            list.anchorMax = new Vector2(1, 1);
            list.pivot = new Vector2(0, 1);
            list.anchoredPosition = new Vector2(0, -60);
            list.sizeDelta = new Vector2(0, 400);

            var listLayout = list.gameObject.AddComponent<VerticalLayoutGroup>();
            listLayout.childAlignment = TextAnchor.UpperCenter;
            listLayout.spacing = 2;
            listLayout.padding = new RectOffset(0, 0, 0, 0);
            listLayout.childForceExpandWidth = true;
            listLayout.childForceExpandHeight = false;
            listLayout.childControlWidth = true;
            listLayout.childControlHeight = true;

            // 5 item slots
            BuildItemRow(list, "Loha Khadga", "42 / 46", true);
            BuildItemRow(list, "Gandiva", "28 / 30", false);
            BuildItemRow(list, "Sanjivani", "3 / 3", false);
            BuildEmptyItemRow(list);
            BuildEmptyItemRow(list);

            // Equipment summary
            var equip = NewImage("EquipmentBox", page, new Color32(0x14, 0x18, 0x40, 0x66));
            var eRt = equip.GetComponent<RectTransform>();
            eRt.anchorMin = new Vector2(0, 0);
            eRt.anchorMax = new Vector2(1, 0);
            eRt.pivot = new Vector2(0, 0);
            eRt.anchoredPosition = new Vector2(0, 0);
            eRt.sizeDelta = new Vector2(0, 110);

            var eTitle = NewText("EquipTitle", eRt, "EQUIPMENT", cinzel, 16, ColSilver,
                TextAlignmentOptions.TopLeft, FontStyles.Normal);
            eTitle.characterSpacing = 6;
            var etRt = eTitle.rectTransform;
            etRt.anchorMin = new Vector2(0, 1);
            etRt.anchorMax = new Vector2(1, 1);
            etRt.pivot = new Vector2(0, 1);
            etRt.anchoredPosition = new Vector2(14, -8);
            etRt.sizeDelta = new Vector2(0, 22);

            string[] eLabels = { "Rng", "Atk", "Hit", "Crit", "Avo" };
            string[] eValues = { "1", "31", "140", "15", "48" };
            for (int i = 0; i < 5; i++)
            {
                float xMin = i / 5f;
                float xMax = xMin + 0.2f;
                BuildDerivedStat(eRt, eLabels[i], eValues[i], xMin, xMax);
            }
        }

        static void BuildItemRow(RectTransform parent, string itemName, string uses, bool equipped)
        {
            var row = NewRect("Item_" + itemName.Replace(" ", ""), parent);
            row.sizeDelta = new Vector2(0, 60);

            var layout = row.gameObject.AddComponent<HorizontalLayoutGroup>();
            layout.childAlignment = TextAnchor.MiddleLeft;
            layout.spacing = 14;
            layout.padding = new RectOffset(14, 14, 6, 6);
            layout.childForceExpandWidth = false;
            layout.childForceExpandHeight = false;
            layout.childControlWidth = true;
            layout.childControlHeight = true;

            var le = row.gameObject.AddComponent<LayoutElement>();
            le.preferredHeight = 60;

            // Icon placeholder
            var icon = NewImage("Icon", row, sprIconItem != null ? Color.white : ColGoldFaint);
            if (sprIconItem != null) icon.GetComponent<Image>().sprite = sprIconItem;
            var iconEl = icon.AddComponent<LayoutElement>();
            iconEl.preferredWidth = 44; iconEl.preferredHeight = 44;

            // Name
            var nameT = NewText("Name", row, itemName, cormorant, 28, ColIvory,
                TextAlignmentOptions.MidlineLeft, FontStyles.Bold);
            var nameEl = nameT.gameObject.AddComponent<LayoutElement>();
            nameEl.flexibleWidth = 1;

            // Uses
            var usesT = NewText("Uses", row, uses, cormorant, 26, ColSilver,
                TextAlignmentOptions.MidlineRight, FontStyles.Normal);
            var usesFit = usesT.gameObject.AddComponent<ContentSizeFitter>();
            usesFit.horizontalFit = ContentSizeFitter.FitMode.PreferredSize;
            usesFit.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

            if (equipped)
            {
                var eqT = NewText("Equipped", row, "E", cinzel, 16, ColGreen,
                    TextAlignmentOptions.Midline, FontStyles.Bold);
                eqT.characterSpacing = 4;
                var eqFit = eqT.gameObject.AddComponent<ContentSizeFitter>();
                eqFit.horizontalFit = ContentSizeFitter.FitMode.PreferredSize;
                eqFit.verticalFit = ContentSizeFitter.FitMode.PreferredSize;
            }

            // Bottom separator
            var sep = NewImage("Sep", row, ColSepFaint);
            var sepRt = sep.GetComponent<RectTransform>();
            sepRt.anchorMin = new Vector2(0, 0);
            sepRt.anchorMax = new Vector2(1, 0);
            sepRt.pivot = new Vector2(0.5f, 0);
            sepRt.sizeDelta = new Vector2(0, 1);
            sep.AddComponent<LayoutElement>().ignoreLayout = true;
        }

        static void BuildEmptyItemRow(RectTransform parent)
        {
            var row = NewRect("Item_Empty", parent);
            row.sizeDelta = new Vector2(0, 60);

            var le = row.gameObject.AddComponent<LayoutElement>();
            le.preferredHeight = 60;

            var text = NewText("EmptyText", row, "\u2014 Empty \u2014", cormorant, 22,
                new Color(ColSilver.r, ColSilver.g, ColSilver.b, 0.25f),
                TextAlignmentOptions.Center, FontStyles.Italic);
            var tRt = text.rectTransform;
            tRt.anchorMin = Vector2.zero;
            tRt.anchorMax = Vector2.one;
            tRt.offsetMin = Vector2.zero;
            tRt.offsetMax = Vector2.zero;
        }

        // ==================================================================
        // page 3 — Supports (Bonds & Oaths)
        // ==================================================================

        static void BuildSupportsPage(RectTransform parent)
        {
            var page = NewRect("SupportsPage", parent);
            SetStretch(page, 0);
            page.gameObject.SetActive(false);

            BuildPageHeader(page, "Bonds & Oaths", "3 / 3", 2);

            // Support list
            var list = NewRect("SupportList", page);
            list.anchorMin = new Vector2(0, 1);
            list.anchorMax = new Vector2(1, 1);
            list.pivot = new Vector2(0, 1);
            list.anchoredPosition = new Vector2(0, -60);
            list.sizeDelta = new Vector2(0, 500);

            var listLayout = list.gameObject.AddComponent<VerticalLayoutGroup>();
            listLayout.childAlignment = TextAnchor.UpperCenter;
            listLayout.spacing = 2;
            listLayout.childForceExpandWidth = true;
            listLayout.childForceExpandHeight = false;
            listLayout.childControlWidth = true;
            listLayout.childControlHeight = true;

            // Hidden template — cloned at runtime per bond returned by ISupportProvider.
            BuildSupportRowTemplate(list);

            // Empty-state text — shown when no bonds exist.
            var empty = NewText("SupportsEmptyText", page,
                "No bonds yet.",
                cormorantItalic, 22, new Color(ColSilver.r, ColSilver.g, ColSilver.b, 0.4f),
                TextAlignmentOptions.Center, FontStyles.Italic);
            var eRt = empty.rectTransform;
            eRt.anchorMin = new Vector2(0, 1);
            eRt.anchorMax = new Vector2(1, 1);
            eRt.pivot = new Vector2(0.5f, 1);
            eRt.anchoredPosition = new Vector2(0, -200);
            eRt.sizeDelta = new Vector2(-40, 40);
            empty.gameObject.SetActive(false);

            // Camp-conversation hint
            var note = NewText("EmptyNote", page,
                "Further bonds may form through shared battles and camp conversations.",
                cormorantItalic, 22, new Color(ColSilver.r, ColSilver.g, ColSilver.b, 0.2f),
                TextAlignmentOptions.TopLeft, FontStyles.Italic);
            var nRt = note.rectTransform;
            nRt.anchorMin = new Vector2(0, 0);
            nRt.anchorMax = new Vector2(1, 0);
            nRt.pivot = new Vector2(0, 0);
            nRt.anchoredPosition = new Vector2(12, 16);
            nRt.sizeDelta = new Vector2(-24, 60);
        }

        static void BuildSupportRowTemplate(RectTransform parent)
        {
            var row = NewRect("SupportRowTemplate", parent);
            row.sizeDelta = new Vector2(0, 80);

            var layout = row.gameObject.AddComponent<HorizontalLayoutGroup>();
            layout.childAlignment = TextAnchor.MiddleLeft;
            layout.spacing = 14;
            layout.padding = new RectOffset(12, 12, 8, 8);
            layout.childForceExpandWidth = false;
            layout.childForceExpandHeight = false;
            layout.childControlWidth = true;
            layout.childControlHeight = true;

            var le = row.gameObject.AddComponent<LayoutElement>();
            le.preferredHeight = 80;

            // Portrait (with notif + diya children that controller toggles per bond state)
            var portrait = NewImage("Portrait", row, ColIndigoLight);
            var pEl = portrait.AddComponent<LayoutElement>();
            pEl.preferredWidth = 64; pEl.preferredHeight = 64;
            portrait.GetComponent<Image>().preserveAspect = true;

            var notif = NewImage("Notif", portrait.transform, Color.white);
            var nRt = notif.GetComponent<RectTransform>();
            nRt.anchorMin = new Vector2(1, 1);
            nRt.anchorMax = new Vector2(1, 1);
            nRt.pivot = new Vector2(1, 1);
            nRt.anchoredPosition = new Vector2(6, 6);
            nRt.sizeDelta = new Vector2(28, 28);
            if (sprNotifBadge != null) notif.GetComponent<Image>().sprite = sprNotifBadge;
            notif.SetActive(false);

            var diya = NewImage("DiyaMemorial", portrait.transform, Color.white);
            var dRt = diya.GetComponent<RectTransform>();
            dRt.anchorMin = new Vector2(1, 0);
            dRt.anchorMax = new Vector2(1, 0);
            dRt.pivot = new Vector2(1, 0);
            dRt.anchoredPosition = new Vector2(4, -4);
            dRt.sizeDelta = new Vector2(28, 28);
            if (sprDiyaMemorial != null) diya.GetComponent<Image>().sprite = sprDiyaMemorial;
            diya.SetActive(false);

            // Name
            var nameT = NewText("Name", row, "Partner", cormorant, 28, ColIvory,
                TextAlignmentOptions.MidlineLeft, FontStyles.Bold);
            var nameEl = nameT.gameObject.AddComponent<LayoutElement>();
            nameEl.flexibleWidth = 1;

            // Bond pips — three Image children, controller swaps sprites per bond.Stage.
            var pips = NewRect("BondPips", row);
            var pipsLayout = pips.gameObject.AddComponent<HorizontalLayoutGroup>();
            pipsLayout.childAlignment = TextAnchor.MiddleCenter;
            pipsLayout.spacing = 4;
            pipsLayout.childForceExpandWidth = false;
            pipsLayout.childForceExpandHeight = false;
            pipsLayout.childControlWidth = true;
            pipsLayout.childControlHeight = true;
            var pipsEl = pips.gameObject.AddComponent<LayoutElement>();
            pipsEl.preferredWidth = 100;

            for (int i = 0; i < 3; i++)
            {
                var pip = NewImage("Pip" + i, pips.transform, Color.white);
                var pipEl = pip.AddComponent<LayoutElement>();
                pipEl.preferredWidth = 32; pipEl.preferredHeight = 32;
                if (sprBondPipUnlit != null) pip.GetComponent<Image>().sprite = sprBondPipUnlit;
            }

            // Shapath icon (oath witnessed)
            var shapath = NewImage("ShapathIcon", row, Color.white);
            var shapathEl = shapath.AddComponent<LayoutElement>();
            shapathEl.preferredWidth = 24; shapathEl.preferredHeight = 24;
            if (sprShapathIcon != null) shapath.GetComponent<Image>().sprite = sprShapathIcon;
            shapath.SetActive(false);

            // Bonus teaser text
            var bonusT = NewText("Bonus", row, "", cormorant, 19, ColGreen,
                TextAlignmentOptions.MidlineRight, FontStyles.Italic);
            var bonusFit = bonusT.gameObject.AddComponent<ContentSizeFitter>();
            bonusFit.horizontalFit = ContentSizeFitter.FitMode.PreferredSize;
            bonusFit.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

            // Bottom separator
            var sep = NewImage("Sep", row, new Color32(0x2a, 0x4a, 0x8a, 0x0f));
            var sepRt = sep.GetComponent<RectTransform>();
            sepRt.anchorMin = new Vector2(0, 0);
            sepRt.anchorMax = new Vector2(1, 0);
            sepRt.pivot = new Vector2(0.5f, 0);
            sepRt.sizeDelta = new Vector2(0, 1);
            sep.AddComponent<LayoutElement>().ignoreLayout = true;

            row.gameObject.SetActive(false); // template stays inactive; clones become visible
        }

        // ==================================================================
        // page header + page indicator
        // ==================================================================

        static void BuildPageHeader(RectTransform page, string title, string indicator, int activeDot)
        {
            var header = NewRect("PageHeader", page);
            header.anchorMin = new Vector2(0, 1);
            header.anchorMax = new Vector2(1, 1);
            header.pivot = new Vector2(0, 1);
            header.anchoredPosition = Vector2.zero;
            header.sizeDelta = new Vector2(0, 50);

            var titleT = NewText("Title", header, title.ToUpper(), cinzel, 38, ColIvory,
                TextAlignmentOptions.MidlineLeft, FontStyles.Bold);
            titleT.characterSpacing = 4;
            if (matPageHeaderGlow != null) titleT.fontMaterial = matPageHeaderGlow;
            var ttRt = titleT.rectTransform;
            ttRt.anchorMin = new Vector2(0, 0);
            ttRt.anchorMax = new Vector2(0.7f, 1);
            ttRt.offsetMin = Vector2.zero;
            ttRt.offsetMax = Vector2.zero;

            var indT = NewText("Indicator", header, indicator, cinzel, 18, ColSilver,
                TextAlignmentOptions.MidlineRight, FontStyles.Normal);
            indT.characterSpacing = 4;
            var indRt = indT.rectTransform;
            indRt.anchorMin = new Vector2(0.7f, 0);
            indRt.anchorMax = new Vector2(1, 1);
            indRt.offsetMin = Vector2.zero;
            indRt.offsetMax = Vector2.zero;

            // Header rule
            var rule = NewImage("HeaderRule", page, ColSapphire);
            var ruleRt = rule.GetComponent<RectTransform>();
            ruleRt.anchorMin = new Vector2(0, 1);
            ruleRt.anchorMax = new Vector2(1, 1);
            ruleRt.pivot = new Vector2(0, 1);
            ruleRt.anchoredPosition = new Vector2(0, -50);
            ruleRt.sizeDelta = new Vector2(0, 2);
        }

        static void BuildPageIndicator(RectTransform parent)
        {
            var dots = NewRect("PageIndicator", parent);
            dots.anchorMin = new Vector2(0.5f, 0);
            dots.anchorMax = new Vector2(0.5f, 0);
            dots.pivot = new Vector2(0.5f, 0);
            dots.anchoredPosition = new Vector2(0, 16);
            dots.sizeDelta = new Vector2(60, 16);

            var layout = dots.gameObject.AddComponent<HorizontalLayoutGroup>();
            layout.childAlignment = TextAnchor.MiddleCenter;
            layout.spacing = 8;
            layout.childForceExpandWidth = false;
            layout.childForceExpandHeight = false;
            layout.childControlWidth = true;
            layout.childControlHeight = true;

            for (int i = 0; i < 3; i++)
            {
                bool isActive = i == 0;
                var dot = NewImage("Dot" + i, dots, Color.white);
                var el = dot.AddComponent<LayoutElement>();
                var dotImg = dot.GetComponent<Image>();

                if (sprPageDotActive != null)
                {
                    // Padded sprite: 24×24 (12×12 visible + glow halo)
                    el.preferredWidth = 24; el.preferredHeight = 24;
                    dotImg.sprite = isActive ? sprPageDotActive : sprPageDotInactive;
                    dotImg.color = isActive ? Color.white : new Color(1, 1, 1, 0.3f);
                }
                else
                {
                    el.preferredWidth = 12; el.preferredHeight = 12;
                    dotImg.color = isActive ? ColGold : new Color(ColGold.r, ColGold.g, ColGold.b, 0.3f);
                }
            }
        }

        // ==================================================================
        // helpers
        // ==================================================================

        static float Sc(float v) => v * Scale;

        static Sprite LoadSprite(string filename) =>
            AssetDatabase.LoadAssetAtPath<Sprite>(SpriteDir + filename);

        static Sprite LoadFrame(string filename) =>
            AssetDatabase.LoadAssetAtPath<Sprite>(FrameDir + filename);

        static void AddFittedText(RectTransform parent, string name, string text,
            TMP_FontAsset font, float size, Color color)
        {
            var t = NewText(name, parent, text, font, size, color,
                TextAlignmentOptions.Midline, FontStyles.Bold);
            var fitter = t.gameObject.AddComponent<ContentSizeFitter>();
            fitter.horizontalFit = ContentSizeFitter.FitMode.PreferredSize;
            fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;
        }

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
