using System.IO;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;

namespace ProjectAstra.EditorTools
{
    // ===========================================================================================
    // Battle Map HUD (temp) — three floating, semi-transparent panels that sit over the live
    // tactical grid. Source of truth: docs/mockups/battle_hud_01_temple_gold.html (approved).
    //
    // Screen kind: HUD OVERLAY (neither full-screen nor modal). Answers to §0 of UI_WORKFLOW.md:
    //   Q1: HUD overlay — three independent anchored panels, map visible behind.
    //   Q2: panels authored at native 1920×1080 coordinates (Scale = 1f).
    //   Q3: no dim backdrop — the battle map must remain readable behind the HUD.
    //   Q6: no interactive states — HUD panels are read-only displays.
    //   Q7: visual effects routes — Route A (panel border + gradient baked into panel_bg sprite,
    //       9-sliced) + Route B (TMP Glow+Underlay materials for text-shadow on unit name /
    //       objective text / tile name / stat values).
    //
    // Fonts are reused from Assets/UI/UnitInfoPanel/Fonts/ since the typography is shared
    // across Project Astra screens. See docs/UI_WORKFLOW.md §4.3.
    // ===========================================================================================
    public static class BattleMapHUDBuilder
    {
        // ---- target resolution & screen kind ----
        const float CanvasWidth  = 1920f;
        const float CanvasHeight = 1080f;
        const bool  IsFullScreen = false; // HUD overlay — see header
        const float Scale        = 1f;

        // ---- paths ----
        const string SpriteDir = "Assets/UI/BattleMapHUD/Sprites/";
        const string FontDir   = "Assets/UI/UnitInfoPanel/Fonts/"; // shared
        const string MatDir    = "Assets/UI/BattleMapHUD/Materials/";

        // ---- panel positions & sizes (native 1920×1080 CSS values) ----
        const float EdgePad    = 56f;
        const float UnitCardW  = 580f, UnitCardH  = 228f;
        const float ObjectiveW = 450f, ObjectiveH = 200f;
        const float TileInfoW  = 340f, TileInfoH  = 180f;

        // ---- colours (transcribed from HTML CSS) ----
        static readonly Color ColGold      = new Color32(0xd4, 0xa2, 0x4c, 0xff);
        static readonly Color ColGoldFaint = new Color32(0xd4, 0xa2, 0x4c, 0x38); // 0.22 alpha
        static readonly Color ColIvory     = new Color32(0xf5, 0xe9, 0xc8, 0xff);
        static readonly Color ColBronze    = new Color32(0xc0, 0xa0, 0x68, 0xff);
        static readonly Color ColCopper    = new Color32(0xc0, 0x88, 0x58, 0xff);
        static readonly Color ColMuted     = new Color32(0x8a, 0x7a, 0x5c, 0xff);
        static readonly Color ColGreenStat = new Color32(0x8d, 0xe0, 0x78, 0xff);
        static readonly Color ColHpDark    = new Color32(0x0a, 0x06, 0x04, 0xff);

        // ---- fonts & materials (assigned in Build) ----
        static TMP_FontAsset cinzel, cinzelDecor, cormorant, cormorantItalic;
        static Material matGoldGlow, matGreenGlow;

        // ==================================================================
        // entry point
        // ==================================================================

        [MenuItem("Project Astra/Build Battle Map HUD (temp)")]
        public static void Build()
        {
            // Sanity-check: overlay panels must fit within canvas bounds.
            if (UnitCardW + EdgePad > CanvasWidth / 2f ||
                ObjectiveW + EdgePad > CanvasWidth / 2f ||
                TileInfoW + EdgePad > CanvasWidth / 2f)
            {
                Debug.LogError("BattleMapHUDBuilder: panel sizes exceed canvas half-width. Aborting.");
                return;
            }

            cinzel          = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(FontDir + "Cinzel SDF.asset");
            cinzelDecor     = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(FontDir + "CinzelDecorative SDF.asset");
            cormorant       = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(FontDir + "CormorantGaramond SDF.asset");
            cormorantItalic = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(FontDir + "CormorantGaramondItalic SDF.asset");

            if (cinzel == null || cinzelDecor == null || cormorant == null || cormorantItalic == null)
            {
                Debug.LogError("BattleMapHUDBuilder: missing TMP font assets in " + FontDir);
                return;
            }

            EnsureTmpMaterials();

            var canvas = EnsureCanvas();
            var existing = canvas.transform.Find("BattleMapHUD");
            if (existing != null) Object.DestroyImmediate(existing.gameObject);

            var root = NewRect("BattleMapHUD", canvas.transform);
            SetStretch(root, 0);

            BuildUnitCard(root);
            BuildObjectivePanel(root);
            BuildTileInfoPanel(root);

            WireController(root);

            EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());
            Selection.activeGameObject = root.gameObject;
            Debug.Log("BattleMapHUD built.");
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
        // TMP glow materials (Route B per §4.5)
        // ==================================================================

        static void EnsureTmpMaterials()
        {
            if (!AssetDatabase.IsValidFolder("Assets/UI/BattleMapHUD/Materials"))
                AssetDatabase.CreateFolder("Assets/UI/BattleMapHUD", "Materials");

            matGoldGlow  = LoadOrCreateGlow(
                MatDir + "CinzelDecorGoldGlow.mat",
                cinzelDecor,
                glowColor:    new Color(0xd4 / 255f, 0xa2 / 255f, 0x4c / 255f, 0.28f),
                glowOuter:    0.6f,
                underlayColor:new Color(0f, 0f, 0f, 0.85f),
                underlayOffY: -0.06f,
                underlaySoft: 0.18f);

            matGreenGlow = LoadOrCreateGlow(
                MatDir + "CinzelDecorGreenGlow.mat",
                cinzelDecor,
                glowColor:    new Color(0x8d / 255f, 0xe0 / 255f, 0x78 / 255f, 0.45f),
                glowOuter:    0.44f,
                underlayColor:new Color(0f, 0f, 0f, 0f),
                underlayOffY: 0f,
                underlaySoft: 0f);
        }

        static Material LoadOrCreateGlow(string path, TMP_FontAsset font,
            Color glowColor, float glowOuter,
            Color underlayColor, float underlayOffY, float underlaySoft)
        {
            var existing = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (existing != null) return existing;

            var baseMat = font.material;
            var mat = new Material(baseMat);

            // Switch to the full Distance Field shader (Mobile variant has no Glow feature).
            var fullShader = Shader.Find("TextMeshPro/Distance Field");
            if (fullShader != null) mat.shader = fullShader;

            // Copy SDF-critical properties from the base material so glyphs remain visible.
            string[] floatProps = { "_GradientScale", "_TextureWidth", "_TextureHeight",
                                    "_WeightNormal", "_WeightBold",
                                    "_ScaleRatioA", "_ScaleRatioB", "_ScaleRatioC" };
            foreach (var p in floatProps)
                if (baseMat.HasProperty(p)) mat.SetFloat(p, baseMat.GetFloat(p));
            if (baseMat.HasProperty("_MainTex"))
                mat.SetTexture("_MainTex", baseMat.GetTexture("_MainTex"));

            // Glow
            if (glowColor.a > 0f)
            {
                mat.EnableKeyword("GLOW_ON");
                mat.SetColor("_GlowColor", glowColor);
                mat.SetFloat("_GlowOuter", glowOuter);
                mat.SetFloat("_GlowInner", 0f);
                mat.SetFloat("_GlowPower", 1f);
            }

            // Underlay (drop shadow)
            if (underlayColor.a > 0f)
            {
                mat.EnableKeyword("UNDERLAY_ON");
                mat.SetColor("_UnderlayColor", underlayColor);
                mat.SetFloat("_UnderlayOffsetX", 0f);
                mat.SetFloat("_UnderlayOffsetY", underlayOffY);
                mat.SetFloat("_UnderlaySoftness", underlaySoft);
            }

            AssetDatabase.CreateAsset(mat, path);
            AssetDatabase.SaveAssets();
            return mat;
        }

        // ==================================================================
        // Unit Card (top-left)
        // ==================================================================

        static void BuildUnitCard(RectTransform root)
        {
            var card = BuildPanel(root, "UnitCard", UnitCardW, UnitCardH,
                anchor: new Vector2(0, 1), pivot: new Vector2(0, 1),
                anchoredPos: new Vector2(EdgePad, -EdgePad));

            // Portrait (left column)
            var portrait = BuildPortraitFrame(card, 140f);
            portrait.anchorMin = portrait.anchorMax = new Vector2(0, 1);
            portrait.pivot = new Vector2(0, 1);
            portrait.anchoredPosition = new Vector2(28, -28);

            // Name / class / HP (right column)
            float rightX = 28f + 140f + 26f; // portrait + gap
            float rightW = UnitCardW - rightX - 32f;

            var name = NewText("UnitName", card, "Arjuna", cinzelDecor, 40,
                ColIvory, TextAlignmentOptions.Left, FontStyles.Normal);
            PlaceTopLeft(name, rightX, 36, rightW, 48);
            if (matGoldGlow != null) name.fontMaterial = matGoldGlow;

            var cls = NewText("UnitClass", card, "KSHATRIYA · LORD", cormorantItalic, 19,
                ColCopper, TextAlignmentOptions.Left, FontStyles.Italic);
            cls.characterSpacing = 10f;
            PlaceTopLeft(cls, rightX, 86, rightW, 24);

            // HP bar + labels
            var hpLabel = NewText("HPLabel", card, "VITALITY", cinzel, 12,
                ColMuted, TextAlignmentOptions.Left, FontStyles.Normal);
            hpLabel.characterSpacing = 16f;
            PlaceTopLeft(hpLabel, rightX, 136, rightW, 18);

            var hpValue = NewText("HPValue", card, "48 / 48", cinzelDecor, 26,
                ColIvory, TextAlignmentOptions.Right, FontStyles.Bold);
            PlaceTopLeft(hpValue, rightX, 128, rightW, 32);
            if (matGoldGlow != null) hpValue.fontMaterial = matGoldGlow;

            BuildHpBar(card, rightX, 162, rightW);

            AddCornerBosses(card, UnitCardW, UnitCardH);
        }

        static RectTransform BuildPortraitFrame(RectTransform parent, float size)
        {
            // Root container so the circular mask crops the portrait to a circle.
            var frame = NewImage("PortraitFrame", parent, Color.white);
            var rt = frame.GetComponent<RectTransform>();
            rt.sizeDelta = new Vector2(size, size);

            var img = frame.GetComponent<Image>();
            img.sprite = LoadSprite("portrait_frame_bg.png");
            img.type = Image.Type.Simple;

            // Mask: child portrait gets cropped to the circular frame sprite's alpha.
            var mask = frame.AddComponent<Mask>();
            mask.showMaskGraphic = true;

            // Arjuna portrait as a child of the masked frame.
            var arjuna = NewImage("Arjuna", rt, Color.white);
            SetStretch(arjuna, 2f);
            var aImg = arjuna.GetComponent<Image>();
            aImg.sprite = LoadSprite("portrait_arjuna.png");
            aImg.preserveAspect = true;

            return rt;
        }

        static void BuildHpBar(RectTransform parent, float x, float y, float w)
        {
            // 1px gold outer frame
            var outer = NewImage("HpBar", parent, ColGold);
            var oRt = outer.GetComponent<RectTransform>();
            PlaceTopLeft(oRt, x, y, w, 10);

            // Dark inset background
            var bg = NewImage("HpBarBg", oRt, ColHpDark);
            SetStretch(bg, 1f);

            // Green gradient fill (driven by Image.fillAmount at runtime)
            var fill = NewImage("HpFill", bg.transform, Color.white);
            SetStretch(fill, 0);
            var fImg = fill.GetComponent<Image>();
            fImg.sprite = LoadSprite("hp_fill_green.png");
            fImg.type = Image.Type.Filled;
            fImg.fillMethod = Image.FillMethod.Horizontal;
            fImg.fillAmount = 1f;
        }

        // ==================================================================
        // Objective Panel (top-right)
        // ==================================================================

        static void BuildObjectivePanel(RectTransform root)
        {
            var panel = BuildPanel(root, "ObjectivePanel", ObjectiveW, ObjectiveH,
                anchor: new Vector2(1, 1), pivot: new Vector2(1, 1),
                anchoredPos: new Vector2(-EdgePad, -EdgePad));

            float padX = 32f;
            float contentW = ObjectiveW - padX * 2f;

            var label = NewText("ObjLabel", panel, "PRIMARY DHARMA", cinzel, 12,
                ColMuted, TextAlignmentOptions.Center, FontStyles.Normal);
            label.characterSpacing = 18f;
            PlaceTopLeft(label, padX, 30, contentW, 18);

            var text = NewText("ObjText", panel, "Slay the Asura Lord", cinzelDecor, 28,
                ColIvory, TextAlignmentOptions.Center, FontStyles.Bold);
            PlaceTopLeft(text, padX, 54, contentW, 40);
            if (matGoldGlow != null) text.fontMaterial = matGoldGlow;

            var divider = NewImage("Divider", panel, Color.white);
            var dRt = divider.GetComponent<RectTransform>();
            PlaceTopLeft(dRt, padX, 118, contentW, 2);
            var dImg = divider.GetComponent<Image>();
            dImg.sprite = LoadSprite("divider_gold.png");
            dImg.type = Image.Type.Sliced;

            var turnLabel = NewText("TurnLabel", panel, "Turn", cormorantItalic, 18,
                ColBronze, TextAlignmentOptions.Left, FontStyles.Italic);
            PlaceTopLeft(turnLabel, padX, 140, 160, 28);

            var turnNum = NewText("TurnNum", panel, "04", cinzelDecor, 28,
                ColIvory, TextAlignmentOptions.Right, FontStyles.Bold);
            PlaceTopLeft(turnNum, ObjectiveW - padX - 120, 134, 120, 34);

            AddCornerBosses(panel, ObjectiveW, ObjectiveH);
        }

        // ==================================================================
        // Tile Info Panel (bottom-right)
        // ==================================================================

        static void BuildTileInfoPanel(RectTransform root)
        {
            var panel = BuildPanel(root, "TileInfoPanel", TileInfoW, TileInfoH,
                anchor: new Vector2(1, 0), pivot: new Vector2(1, 0),
                anchoredPos: new Vector2(-EdgePad, EdgePad));

            var name = NewText("TileName", panel, "Temple Floor", cinzelDecor, 28,
                ColIvory, TextAlignmentOptions.Center, FontStyles.Bold);
            PlaceTopLeft(name, 0, 30, TileInfoW, 40);
            if (matGoldGlow != null) name.fontMaterial = matGoldGlow;

            // Two stat columns
            float colW = TileInfoW / 2f;

            var lblDef = NewText("StatLabelDef", panel, "DEF", cinzel, 12,
                ColMuted, TextAlignmentOptions.Center, FontStyles.Normal);
            lblDef.characterSpacing = 20f;
            PlaceTopLeft(lblDef, 0, 92, colW, 18);

            var valDef = NewText("StatValueDef", panel, "+1", cinzelDecor, 32,
                ColGreenStat, TextAlignmentOptions.Center, FontStyles.Bold);
            PlaceTopLeft(valDef, 0, 112, colW, 44);
            if (matGreenGlow != null) valDef.fontMaterial = matGreenGlow;

            var lblAvo = NewText("StatLabelAvo", panel, "AVO", cinzel, 12,
                ColMuted, TextAlignmentOptions.Center, FontStyles.Normal);
            lblAvo.characterSpacing = 20f;
            PlaceTopLeft(lblAvo, colW, 92, colW, 18);

            var valAvo = NewText("StatValueAvo", panel, "+10", cinzelDecor, 32,
                ColGreenStat, TextAlignmentOptions.Center, FontStyles.Bold);
            PlaceTopLeft(valAvo, colW, 112, colW, 44);
            if (matGreenGlow != null) valAvo.fontMaterial = matGreenGlow;

            // Thin vertical divider between the two stats
            var vDiv = NewImage("StatDivider", panel, ColGoldFaint);
            var vRt = vDiv.GetComponent<RectTransform>();
            PlaceTopLeft(vRt, colW - 0.5f, 90, 1, 56);

            AddCornerBosses(panel, TileInfoW, TileInfoH);
        }

        // ==================================================================
        // runtime controller wiring
        // ==================================================================

        static void WireController(RectTransform root)
        {
            var controller = root.gameObject.AddComponent<ProjectAstra.UI.BattleMapHUDController>();

            // Unit card refs
            controller.UnitCardRoot  = root.Find("UnitCard").gameObject;
            controller.UnitName      = root.Find("UnitCard/UnitName").GetComponent<TextMeshProUGUI>();
            controller.UnitClass     = root.Find("UnitCard/UnitClass").GetComponent<TextMeshProUGUI>();
            controller.HpValue       = root.Find("UnitCard/HPValue").GetComponent<TextMeshProUGUI>();
            controller.HpFill        = root.Find("UnitCard/HpBar/HpBarBg/HpFill").GetComponent<Image>();
            controller.PortraitImage = root.Find("UnitCard/PortraitFrame/Arjuna").GetComponent<Image>();
            controller.DefaultPortrait = AssetDatabase.LoadAssetAtPath<Sprite>(SpriteDir + "portrait_arjuna.png");

            // Objective panel refs
            controller.ObjText       = root.Find("ObjectivePanel/ObjText").GetComponent<TextMeshProUGUI>();
            controller.TurnNum       = root.Find("ObjectivePanel/TurnNum").GetComponent<TextMeshProUGUI>();

            // Tile info panel refs
            controller.TileName      = root.Find("TileInfoPanel/TileName").GetComponent<TextMeshProUGUI>();
            controller.StatValueDef  = root.Find("TileInfoPanel/StatValueDef").GetComponent<TextMeshProUGUI>();
            controller.StatValueAvo  = root.Find("TileInfoPanel/StatValueAvo").GetComponent<TextMeshProUGUI>();

            // Data sources — ScriptableObjects wired by path
            controller.StatTable     = AssetDatabase.LoadAssetAtPath<ProjectAstra.Core.TerrainStatTable>(
                "Assets/ScriptableObjects/Map/TerrainStatTable.asset");
            controller.TurnChannel   = AssetDatabase.LoadAssetAtPath<ProjectAstra.Core.TurnEventChannel>(
                "Assets/ScriptableObjects/Core/TurnEventChannel.asset");

            if (controller.StatTable == null)
                Debug.LogWarning("BattleMapHUDBuilder: TerrainStatTable asset not found — tile bonuses will be 0.");
            if (controller.TurnChannel == null)
                Debug.LogWarning("BattleMapHUDBuilder: TurnEventChannel asset not found — turn number will not update.");
        }

        // ==================================================================
        // shared panel chrome
        // ==================================================================

        static RectTransform BuildPanel(RectTransform parent, string name,
            float w, float h, Vector2 anchor, Vector2 pivot, Vector2 anchoredPos)
        {
            var panel = NewImage(name, parent, Color.white);
            var rt = panel.GetComponent<RectTransform>();
            rt.anchorMin = rt.anchorMax = anchor;
            rt.pivot = pivot;
            rt.anchoredPosition = anchoredPos;
            rt.sizeDelta = new Vector2(w, h);

            var img = panel.GetComponent<Image>();
            img.sprite = LoadSprite("panel_bg.png");
            img.type = Image.Type.Sliced;
            img.pixelsPerUnitMultiplier = 1f;

            return rt;
        }

        static void AddCornerBosses(RectTransform panel, float w, float h)
        {
            // CSS: .corner { top/left/right/bottom: -6px; width/height: 22px; }
            AddBoss(panel, new Vector2(0, 1), new Vector2(0, 1), new Vector2(-6f, 6f));
            AddBoss(panel, new Vector2(1, 1), new Vector2(1, 1), new Vector2(6f, 6f));
            AddBoss(panel, new Vector2(0, 0), new Vector2(0, 0), new Vector2(-6f, -6f));
            AddBoss(panel, new Vector2(1, 0), new Vector2(1, 0), new Vector2(6f, -6f));
        }

        static void AddBoss(RectTransform panel, Vector2 anchor, Vector2 pivot, Vector2 offset)
        {
            var go = NewImage("CornerBoss", panel, Color.white);
            var rt = go.GetComponent<RectTransform>();
            rt.anchorMin = rt.anchorMax = anchor;
            rt.pivot = new Vector2(0.5f, 0.5f);
            rt.sizeDelta = new Vector2(22f, 22f);
            rt.anchoredPosition = offset;
            var img = go.GetComponent<Image>();
            img.sprite = LoadSprite("corner_boss.png");
            img.type = Image.Type.Simple;
            img.preserveAspect = true;
        }

        // ==================================================================
        // low-level element helpers
        // ==================================================================

        static Sprite LoadSprite(string filename) =>
            AssetDatabase.LoadAssetAtPath<Sprite>(SpriteDir + filename);

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

        // Place a rect by its top-left corner in canvas-local coordinates (y grows down in CSS,
        // but in uGUI with top anchor, anchoredPosition y is negative going down from top).
        static void PlaceTopLeft(RectTransform rt, float x, float y, float w, float h)
        {
            rt.anchorMin = new Vector2(0, 1);
            rt.anchorMax = new Vector2(0, 1);
            rt.pivot = new Vector2(0, 1);
            rt.anchoredPosition = new Vector2(x, -y);
            rt.sizeDelta = new Vector2(w, h);
        }

        static void PlaceTopLeft(TextMeshProUGUI t, float x, float y, float w, float h)
            => PlaceTopLeft(t.GetComponent<RectTransform>(), x, y, w, h);

        static void SetStretch(GameObject go, float inset)
            => SetStretch(go.GetComponent<RectTransform>(), inset);

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
