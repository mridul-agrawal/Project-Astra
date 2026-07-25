using System.IO;
using ProjectAstra.Core.UI.ActionMenu;
using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

namespace ProjectAstra.EditorTools
{
    // ==========================================================================================
    // SelectionMenu — the shared vertical list-menu popup (unit action menu, trade-target picker,
    // inventory slot sub-menu) built as a prefab at Assets/UI/SelectionMenu/SelectionMenu.prefab,
    // plus an Indigo Codex parchment variant for the inventory sub-menu.
    //
    // Warrior's Command look: octagonal ember frame, trishul cursor, ember dividers. Variable
    // length — the disabled Row_Template child is cloned into the root's VerticalLayoutGroup per
    // option at runtime by SelectionMenuView; a ContentSizeFitter grows the panel to fit.
    // ==========================================================================================
    public static class SelectionMenuBuilder
    {
        const float PanelWidth    = 340f;
        const float OptionHeight  = 52f;
        const float DividerHeight = 8f;
        const float RowHeight     = OptionHeight + DividerHeight;   // 60 — option band + bottom divider
        const float ContentCenter = (DividerHeight + OptionHeight * 0.5f) / RowHeight;  // vert-centre of the option band
        const int   PadX          = 8;
        const int   PadY          = 20;

        const string SpriteDir   = "Assets/UI/UnitActionMenu/Sprites/";
        const string FontPath    = "Assets/UI/UnitInfoPanel/Fonts/Cinzel SDF.asset";
        const string GlowMatPath = "Assets/UI/BattleMapHUD/Materials/CinzelGoldGlow.mat";
        const string BasePath    = "Assets/UI/SelectionMenu/SelectionMenu.prefab";
        const string ParchPath   = "Assets/UI/SelectionMenu/SelectionMenu_Parchment.prefab";

        // Warrior's Command palette
        static readonly Color EmberText     = HexA("f5d8b8", 1f);
        static readonly Color EmberDisabled = HexA("4a2a1a", 1f);
        static readonly Color EmberBar      = HexA("d46a2c", 1f);

        // Indigo Codex parchment palette (variant)
        static readonly Color ParchBg    = HexA("f2e6c4", 1f);
        static readonly Color ParchInk    = HexA("3d2a1a", 1f);
        static readonly Color ParchSel    = HexA("b0382a", 1f);   // vermillion
        static readonly Color ParchBrass  = HexA("c9993a", 1f);

        static Sprite bgSprite, trishulSprite, dividerSprite;
        static TMP_FontAsset cinzel;
        static Material glowMat;

        [MenuItem("Project Astra/Build Selection Menu (prefab)")]
        public static void BuildPrefab()
        {
            LoadResources();

            var root = BuildBase();
            EnsureDir(BasePath);
            PrefabUtility.SaveAsPrefabAsset(root, BasePath, out bool ok);
            Object.DestroyImmediate(root);
            if (!ok) { Debug.LogError("SelectionMenu base prefab save failed."); return; }
            Debug.Log($"SelectionMenu base prefab saved to {BasePath}");

            BuildParchmentVariant();

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
        }

        static void LoadResources()
        {
            bgSprite      = LoadSprite("action_menu_bg.png");
            trishulSprite = LoadSprite("trishul_cursor.png");
            dividerSprite = LoadSprite("ember_divider.png");
            cinzel  = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(FontPath);
            glowMat = AssetDatabase.LoadAssetAtPath<Material>(GlowMatPath);
            if (cinzel == null)  Debug.LogWarning("Cinzel SDF font missing at " + FontPath);
            if (glowMat == null) Debug.LogWarning("CinzelGoldGlow material missing at " + GlowMatPath);
        }

        // ==================================================================
        // base prefab
        // ==================================================================

        static GameObject BuildBase()
        {
            var root = new GameObject("SelectionMenu",
                typeof(RectTransform), typeof(CanvasGroup), typeof(SelectionMenuView));
            var rt = root.GetComponent<RectTransform>();
            rt.anchorMin = rt.anchorMax = new Vector2(1f, 0.5f);   // right-centre of the canvas
            rt.pivot = new Vector2(1f, 0.5f);
            rt.anchoredPosition = new Vector2(-40f, 0f);
            rt.sizeDelta = new Vector2(PanelWidth, 0f);            // width fixed; height auto (ContentSizeFitter)

            root.GetComponent<CanvasGroup>().blocksRaycasts = false;

            var vlg = root.AddComponent<VerticalLayoutGroup>();
            vlg.padding = new RectOffset(PadX, PadX, PadY, PadY);
            vlg.spacing = 0f;
            vlg.childAlignment = TextAnchor.UpperCenter;
            vlg.childControlWidth = true;
            vlg.childControlHeight = true;
            vlg.childForceExpandWidth = true;
            vlg.childForceExpandHeight = false;

            var csf = root.AddComponent<ContentSizeFitter>();
            csf.verticalFit = ContentSizeFitter.FitMode.PreferredSize;
            csf.horizontalFit = ContentSizeFitter.FitMode.Unconstrained;

            // Background — fills the auto-sized panel, ignored by the layout.
            var bg = NewImage("Background", rt, Color.white);
            var bgImg = bg.GetComponent<Image>();
            bgImg.sprite = bgSprite;
            bgImg.type = bgSprite != null ? Image.Type.Sliced : Image.Type.Simple;
            bgImg.pixelsPerUnitMultiplier = 1f;
            bgImg.raycastTarget = false;
            bg.AddComponent<LayoutElement>().ignoreLayout = true;
            SetStretch(bg.GetComponent<RectTransform>());

            var rowTemplate = BuildRowTemplate(rt);

            WireView(root.GetComponent<SelectionMenuView>(), rt, rowTemplate,
                EmberText, EmberText, EmberDisabled);

            root.SetActive(false);   // disabled-by-default, like every other UI screen
            return root;
        }

        static GameObject BuildRowTemplate(RectTransform parent)
        {
            var row = NewRect("Row_Template", parent);
            row.gameObject.AddComponent<LayoutElement>().preferredHeight = RowHeight;

            // Left ember accent bar — vertical-centred in the option band, shown on the selected row.
            var bar = NewImage("AccentBar", row, EmberBar);
            var barRt = bar.GetComponent<RectTransform>();
            barRt.anchorMin = barRt.anchorMax = new Vector2(0f, ContentCenter);
            barRt.pivot = new Vector2(0f, 0.5f);
            barRt.anchoredPosition = Vector2.zero;
            barRt.sizeDelta = new Vector2(3f, OptionHeight - 8f);
            bar.GetComponent<Image>().raycastTarget = false;
            bar.SetActive(false);

            // Trishul cursor icon — shown on the selected row.
            var trishul = NewImage("Trishul", row, Color.white);
            var tRt = trishul.GetComponent<RectTransform>();
            tRt.anchorMin = tRt.anchorMax = new Vector2(0f, ContentCenter);
            tRt.pivot = new Vector2(0.5f, 0.5f);
            tRt.anchoredPosition = new Vector2(16f, 0f);
            tRt.sizeDelta = new Vector2(20f, 20f);
            var tImg = trishul.GetComponent<Image>();
            tImg.sprite = trishulSprite;
            tImg.preserveAspect = true;
            tImg.raycastTarget = false;
            trishul.SetActive(false);

            // Option label — fills the option band (left 48 to clear the trishul, bottom 8 clears the divider).
            var text = NewText("Text", row, "Option", cinzel, 26, EmberText);
            var txtRt = text.rectTransform;
            txtRt.anchorMin = Vector2.zero;
            txtRt.anchorMax = Vector2.one;
            txtRt.offsetMin = new Vector2(48f, DividerHeight);
            txtRt.offsetMax = new Vector2(-8f, 0f);
            text.fontStyle = FontStyles.Bold;
            text.alignment = TextAlignmentOptions.MidlineLeft;
            text.characterSpacing = 4f;

            // Ember divider along the bottom (hidden on the last row at runtime).
            var div = NewImage("Divider", row, Color.white);
            var dRt = div.GetComponent<RectTransform>();
            dRt.anchorMin = new Vector2(0f, 0f);
            dRt.anchorMax = new Vector2(1f, 0f);
            dRt.pivot = new Vector2(0.5f, 0f);
            dRt.anchoredPosition = Vector2.zero;
            dRt.sizeDelta = new Vector2(-32f, DividerHeight);
            var dImg = div.GetComponent<Image>();
            dImg.sprite = dividerSprite;
            dImg.type = dividerSprite != null ? Image.Type.Sliced : Image.Type.Simple;
            dImg.raycastTarget = false;

            row.gameObject.SetActive(false);
            return row.gameObject;
        }

        // ==================================================================
        // parchment variant
        // ==================================================================

        static void BuildParchmentVariant()
        {
            var baseAsset = AssetDatabase.LoadAssetAtPath<GameObject>(BasePath);
            if (baseAsset == null) { Debug.LogError("Base prefab not found for variant."); return; }

            var inst = (GameObject)PrefabUtility.InstantiatePrefab(baseAsset);

            inst.transform.Find("Background").GetComponent<Image>().color = ParchBg;
            var rowT = inst.transform.Find("Row_Template");
            rowT.Find("AccentBar").GetComponent<Image>().color = ParchBrass;
            rowT.Find("Text").GetComponent<TextMeshProUGUI>().color = ParchInk;

            WireView(inst.GetComponent<SelectionMenuView>(), null, null, ParchInk, ParchSel, EmberDisabled);

            EnsureDir(ParchPath);
            PrefabUtility.SaveAsPrefabAsset(inst, ParchPath, out bool ok);   // instance-of-base → variant
            Object.DestroyImmediate(inst);
            if (ok) Debug.Log($"SelectionMenu parchment variant saved to {ParchPath}");
            else    Debug.LogError("SelectionMenu parchment variant save failed.");
        }

        // Writes the SelectionMenuView serialized fields. optionsContainer/rowTemplate are only
        // set for the base (the variant inherits them); pass null to leave them untouched.
        static void WireView(SelectionMenuView view, RectTransform container, GameObject rowTemplate,
            Color textDefault, Color textSelected, Color textDisabled)
        {
            var so = new SerializedObject(view);
            if (container != null)   so.FindProperty("optionsContainer").objectReferenceValue = container;
            if (rowTemplate != null) so.FindProperty("rowTemplate").objectReferenceValue = rowTemplate;
            if (container != null)
            {
                so.FindProperty("selectedGlowMat").objectReferenceValue = glowMat;
                so.FindProperty("optionFont").objectReferenceValue = cinzel;
            }
            so.FindProperty("textDefault").colorValue = textDefault;
            so.FindProperty("textSelected").colorValue = textSelected;
            so.FindProperty("textDisabled").colorValue = textDisabled;
            so.ApplyModifiedPropertiesWithoutUndo();
        }

        // ==================================================================
        // helpers
        // ==================================================================

        static Sprite LoadSprite(string file) => AssetDatabase.LoadAssetAtPath<Sprite>(SpriteDir + file);

        static void EnsureDir(string assetPath)
        {
            var dir = Path.GetDirectoryName(assetPath);
            if (!Directory.Exists(dir)) Directory.CreateDirectory(dir);
        }

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

        static TextMeshProUGUI NewText(string name, Transform parent, string text,
            TMP_FontAsset font, float size, Color color)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(TextMeshProUGUI));
            go.transform.SetParent(parent, false);
            var t = go.GetComponent<TextMeshProUGUI>();
            t.text = text;
            if (font != null) t.font = font;
            t.fontSize = size;
            t.color = color;
            t.enableWordWrapping = false;
            t.overflowMode = TextOverflowModes.Overflow;
            t.raycastTarget = false;
            return t;
        }

        static void SetStretch(RectTransform rt)
        {
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.pivot = new Vector2(0.5f, 0.5f);
            rt.offsetMin = Vector2.zero;
            rt.offsetMax = Vector2.zero;
        }

        static Color HexA(string hex, float a)
        {
            byte r = byte.Parse(hex.Substring(0, 2), System.Globalization.NumberStyles.HexNumber);
            byte g = byte.Parse(hex.Substring(2, 2), System.Globalization.NumberStyles.HexNumber);
            byte b = byte.Parse(hex.Substring(4, 2), System.Globalization.NumberStyles.HexNumber);
            return new Color(r / 255f, g / 255f, b / 255f, a);
        }
    }
}
