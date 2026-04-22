using System.IO;
using ProjectAstra.Core.UI;
using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

namespace ProjectAstra.EditorTools
{
    // =============================================================================
    // War's Ledger prefab builder. Parchment-dominant full-screen modal.
    //
    // Concept source: docs/mockups/Wars Ledger.html (variant A)
    // Figma source:   J4Aj1Pg5lIrBhwsGXgYqd8 (WAR_LEDGER_FIGMA in .secrets/figma_files.env)
    //
    // The parchment sheet chrome (foxing + grain + drop-shadow-cropped fill) is a
    // single baked sprite `parchment_sheet.png` sized to the logical sheet
    // 1680×952. This builder overlays every live text element: chapter meta,
    // Devanagari column headers, entry templates (cloned at runtime), unnamed
    // tail line, and the footer CONTINUE prompt.
    // =============================================================================
    public static class WarLedgerBuilder
    {
        const float CANVAS_W = 1920f;
        const float CANVAS_H = 1080f;
        const float SHEET_X = 120f, SHEET_Y = 64f;
        const float SHEET_W = 1680f, SHEET_H = 952f;
        const float PAD_X = 96f, PAD_TOP = 72f;

        // Column layout — 1:1.15:1 fractions of inner width (1488px).
        const float INNER_W = SHEET_W - PAD_X * 2f; // 1488
        const float COL0_W = 472f;
        const float COL1_W = 543f;
        const float COL2_W = 473f;
        const float COL0_X = PAD_X;                       // 96
        const float COL1_X = PAD_X + COL0_W;              // 568
        const float COL2_X = PAD_X + COL0_W + COL1_W;     // 1111

        const float COL_Y = PAD_TOP + 130f;   // 202 — start of three-column band
        const float COL_HEAD_H = 96f;
        const float ENTRY_Y = COL_Y + COL_HEAD_H + 14f;   // ≈312

        const string SpriteDir   = "Assets/UI/WarLedger/Sprites/";
        const string FontDir     = "Assets/UI/WarLedger/Fonts/";
        const string MaterialDir = "Assets/UI/WarLedger/Materials/";
        const string TradeFontDir = "Assets/UI/TradeScreen/Fonts/";
        const string PrefabPath  = "Assets/UI/WarLedger/WarLedger.prefab";

        // Variant A palette — warm cream parchment.
        static readonly Color Parchment   = Hex("f2e6c4");
        static readonly Color ParchDim    = Hex("c9b98a");
        static readonly Color InkDeep     = Hex("3d2a1a");
        static readonly Color Ink         = Hex("1a140f");
        static readonly Color Brass       = Hex("c9993a");
        static readonly Color BrassLite   = Hex("e8c66a");
        static readonly Color Vermillion  = Hex("b0382a");

        static WarLedgerRefs refs;
        static TMP_FontAsset cinzel, cormorant, ebGaramond, jetBrainsMono, devanagari;
        static Sprite sprParchment;
        static Material matDevaHeaderInk, matInkBodyUnderlay;

        [MenuItem("Project Astra/Build War Ledger (prefab)")]
        public static void BuildPrefab()
        {
            LoadResources();

            var root = BuildHierarchy();
            if (root == null) return;

            Directory.CreateDirectory(Path.GetDirectoryName(PrefabPath));
            PrefabUtility.SaveAsPrefabAsset(root, PrefabPath, out bool ok);
            Object.DestroyImmediate(root);
            if (ok) Debug.Log($"WarLedger prefab saved to {PrefabPath}");
            else    Debug.LogError("WarLedger prefab save failed.");

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
        }

        static void LoadResources()
        {
            cinzel        = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(TradeFontDir + "Cinzel SDF.asset");
            cormorant     = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(TradeFontDir + "CormorantGaramond SDF.asset");
            ebGaramond    = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(TradeFontDir + "EBGaramond SDF.asset");
            jetBrainsMono = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(TradeFontDir + "JetBrainsMono SDF.asset");
            devanagari    = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(FontDir + "NotoSerifDevanagari SDF.asset");
            if (devanagari == null)
                Debug.LogWarning("WarLedgerBuilder: Devanagari SDF missing — run 'Project Astra/Generate WarLedger Fonts' first.");

            sprParchment = AssetDatabase.LoadAssetAtPath<Sprite>(SpriteDir + "parchment_sheet.png");
            matDevaHeaderInk   = AssetDatabase.LoadAssetAtPath<Material>(WarLedgerMaterials.DevaHeaderInk);
            matInkBodyUnderlay = AssetDatabase.LoadAssetAtPath<Material>(WarLedgerMaterials.InkBodyUnderlay);
            if (sprParchment == null)
                Debug.LogWarning("WarLedgerBuilder: parchment_sheet.png missing — run scripts/download_war_ledger_assets.sh first.");
        }

        // =============================================================================

        static GameObject BuildHierarchy()
        {
            // Root fills canvas stretch — the ledger is a full-screen takeover, with the
            // indigo-wash vignette painted into the root image's color so no separate
            // backdrop sprite is needed.
            var root = new GameObject("WarLedger", typeof(RectTransform));
            var rt = root.GetComponent<RectTransform>();
            rt.anchorMin = Vector2.zero; rt.anchorMax = Vector2.one;
            rt.pivot = new Vector2(0.5f, 0.5f);
            rt.offsetMin = rt.offsetMax = Vector2.zero;
            refs = root.AddComponent<WarLedgerRefs>();

            // Dim-indigo wash behind the parchment (full-canvas raycast blocker too)
            var wash = NewImage("IndigoWash", root.transform, new Color(0x07/255f, 0x04/255f, 0x10/255f, 1f));
            SetStretch(wash.GetComponent<RectTransform>(), 0);
            wash.GetComponent<Image>().raycastTarget = true;

            // Parchment sheet
            var sheet = NewRect("ParchmentSheet", root.transform);
            sheet.anchorMin = sheet.anchorMax = new Vector2(0.5f, 0.5f);
            sheet.pivot = new Vector2(0.5f, 0.5f);
            sheet.anchoredPosition = Vector2.zero;
            sheet.sizeDelta = new Vector2(SHEET_W, SHEET_H);

            var sheetBg = NewImage("SheetBackground", sheet, Color.white);
            SetStretch(sheetBg.GetComponent<RectTransform>(), 0);
            sheetBg.GetComponent<Image>().sprite = sprParchment;
            refs.parchmentSheet = sheetBg.GetComponent<Image>();

            BuildChapterMeta(sheet);
            BuildColumnHairlines(sheet);
            BuildLeftColumn(sheet);
            BuildMiddleColumn(sheet);
            BuildRightColumn(sheet);
            BuildFooter(sheet);

            return root;
        }

        // =============================================================================
        // Chapter meta (eyebrow + title + brass rule)
        // =============================================================================

        static void BuildChapterMeta(RectTransform sheet)
        {
            // CHAPTER label
            var eyebrow = NewText("ChapterEyebrow", sheet, "CHAPTER",
                cinzel, 13, InkDeep, TextAlignmentOptions.Right, FontStyles.Normal);
            eyebrow.characterSpacing = 44;
            SetTopLeftBox(eyebrow.rectTransform, 0, PAD_TOP, SHEET_W/2f - 20, 18);
            refs.chapterEyebrow = eyebrow;

            // 04 number
            var num = NewText("ChapterNumber", sheet, "01",
                jetBrainsMono, 14, InkDeep, TextAlignmentOptions.Left, FontStyles.Normal);
            SetTopLeftBox(num.rectTransform, SHEET_W/2f + 14, PAD_TOP, SHEET_W/2f - 20, 18);
            refs.chapterNumber = num;

            // Chapter title italic
            var title = NewText("ChapterTitle", sheet, "",
                cormorant, 34, InkDeep, TextAlignmentOptions.Center, FontStyles.Italic | FontStyles.Bold);
            SetTopLeftBox(title.rectTransform, 0, PAD_TOP + 30, SHEET_W, 44);
            refs.chapterTitle = title;

            // Brass rule
            var ruleW = SHEET_W * 0.62f;
            var rule = NewImage("HeaderRule", sheet, Color.white);
            var ruleRt = rule.GetComponent<RectTransform>();
            SetTopLeftBox(ruleRt, (SHEET_W - ruleW)/2f, PAD_TOP + 94, ruleW, 1);
            rule.GetComponent<Image>().color = new Color(Brass.r, Brass.g, Brass.b, 0.55f);
        }

        static void BuildColumnHairlines(RectTransform sheet)
        {
            float hairTop = COL_Y + 6;
            float hairBot = SHEET_H - 110 - 30;
            float hairH = hairBot - hairTop;
            var col = new Color(InkDeep.r, InkDeep.g, InkDeep.b, 0.22f);

            var h1 = NewImage("ColumnHairline_L", sheet, col);
            SetTopLeftBox(h1.GetComponent<RectTransform>(), PAD_X + COL0_W - 1, hairTop, 1, hairH);
            var h2 = NewImage("ColumnHairline_R", sheet, col);
            SetTopLeftBox(h2.GetComponent<RectTransform>(), PAD_X + COL0_W + COL1_W - 1, hairTop, 1, hairH);
        }

        // =============================================================================
        // Columns — each has a header (Devanagari + English), an entries container
        // parented to the sheet, and a disabled template row the runtime clones.
        // =============================================================================

        static void BuildColumnHead(RectTransform sheet, float x, float w, string idSuffix,
            string deva, string en, out TextMeshProUGUI devaTmp, out TextMeshProUGUI enTmp)
        {
            var d = NewText("ColDeva_" + idSuffix, sheet, deva,
                devanagari ?? cormorant, 32, InkDeep, TextAlignmentOptions.Left, FontStyles.Bold);
            if (matDevaHeaderInk != null) d.fontMaterial = matDevaHeaderInk;
            SetTopLeftBox(d.rectTransform, x, COL_Y, w, 40);
            devaTmp = d;

            var e = NewText("ColEn_" + idSuffix, sheet, en,
                cinzel, 11, ParchDim, TextAlignmentOptions.Left, FontStyles.Normal);
            e.characterSpacing = 32;
            SetTopLeftBox(e.rectTransform, x, COL_Y + 48, w, 14);
            enTmp = e;

            // Brass rule under head
            var rule = NewImage("ColHeadRule_" + idSuffix, sheet,
                new Color(Brass.r, Brass.g, Brass.b, 0.55f));
            SetTopLeftBox(rule.GetComponent<RectTransform>(), x, COL_Y + 76, w - 4, 1);
        }

        // ---- Left column — Those Who Fell -----------------------------------------

        static void BuildLeftColumn(RectTransform sheet)
        {
            BuildColumnHead(sheet, COL0_X, COL0_W - 40, "Fell",
                "\u091C\u094B \u0917\u093F\u0930\u0947", "Those Who Fell",
                out refs.leftHeadDeva, out refs.leftHeadEn);

            // Entries container — children flow vertically via VerticalLayoutGroup
            var container = NewRect("LeftEntries", sheet);
            SetTopLeftBox(container, COL0_X, ENTRY_Y, COL0_W - 40, SHEET_H - ENTRY_Y - 180);
            var vlg = container.gameObject.AddComponent<VerticalLayoutGroup>();
            vlg.childControlWidth = true; vlg.childControlHeight = false;
            vlg.childForceExpandWidth = true; vlg.childForceExpandHeight = false;
            vlg.spacing = 26f; vlg.padding = new RectOffset(0, 0, 0, 0);
            refs.leftEntriesContainer = container;

            // Template row — one entry (Bullet + Name + Epitaph). Runtime clones this.
            var tmpl = NewRect("LeftEntry_Template", container);
            tmpl.sizeDelta = new Vector2(0, 68);
            var le = tmpl.gameObject.AddComponent<LayoutElement>();
            le.preferredHeight = 68;

            var bullet = NewText("Bullet", tmpl, "\u25C6",
                cormorant, 9, Vermillion, TextAlignmentOptions.Left, FontStyles.Bold);
            SetTopLeftBox(bullet.rectTransform, 0, 4, 14, 14);

            var name = NewText("Name", tmpl, "",
                cormorant, 22, InkDeep, TextAlignmentOptions.MidlineLeft, FontStyles.Bold);
            SetTopLeftBox(name.rectTransform, 18, 0, COL0_W - 40 - 18, 28);

            var epit = NewText("Epitaph", tmpl, "",
                ebGaramond, 14, new Color(Ink.r, Ink.g, Ink.b, 0.78f), TextAlignmentOptions.TopLeft, FontStyles.Italic);
            SetTopLeftBox(epit.rectTransform, 24, 32, COL0_W - 40 - 24, 40);

            tmpl.gameObject.SetActive(false);
            refs.leftEntryTemplate = tmpl.gameObject;

            // Unnamed tail — sits at the bottom of the left column area.
            var tail = NewText("UnnamedTail", sheet, "",
                ebGaramond, 14, new Color(Ink.r, Ink.g, Ink.b, 0.7f), TextAlignmentOptions.TopLeft, FontStyles.Italic);
            SetTopLeftBox(tail.rectTransform, COL0_X, SHEET_H - 110 - 60, COL0_W - 40, 32);
            refs.leftUnnamedTail = tail;
        }

        // ---- Middle column — What Was Kept and What Was Not -----------------------

        static void BuildMiddleColumn(RectTransform sheet)
        {
            BuildColumnHead(sheet, COL1_X, COL1_W - 40, "Kept",
                "\u0915\u094D\u092F\u093E \u0928\u093F\u092D\u093E\u092F\u093E, \u0915\u094D\u092F\u093E \u0928\u0939\u0940\u0902",
                "What Was Kept and What Was Not",
                out refs.middleHeadDeva, out refs.middleHeadEn);

            var container = NewRect("MiddleEntries", sheet);
            SetTopLeftBox(container, COL1_X, ENTRY_Y, COL1_W - 40, SHEET_H - ENTRY_Y - 180);
            var vlg = container.gameObject.AddComponent<VerticalLayoutGroup>();
            vlg.childControlWidth = true; vlg.childControlHeight = false;
            vlg.childForceExpandWidth = true; vlg.childForceExpandHeight = false;
            vlg.spacing = 20f;
            refs.middleEntriesContainer = container;

            var tmpl = NewRect("MiddleEntry_Template", container);
            var le = tmpl.gameObject.AddComponent<LayoutElement>();
            le.preferredHeight = 60;

            var commit = NewText("CommitText", tmpl, "",
                ebGaramond, 16, InkDeep, TextAlignmentOptions.TopLeft, FontStyles.Normal);
            commit.enableWordWrapping = true;
            SetTopLeftBox(commit.rectTransform, 0, 0, COL1_W - 40, 28);

            var res = NewText("Resolution", tmpl, "",
                cormorant, 16, InkDeep, TextAlignmentOptions.TopLeft, FontStyles.Bold);
            SetTopLeftBox(res.rectTransform, 0, 30, COL1_W - 40, 22);

            // vermillion underline under "Kept." — enabled only by runtime
            var underline = NewImage("KeptRule", tmpl, Vermillion);
            SetTopLeftBox(underline.GetComponent<RectTransform>(), 0, 52, 48, 1);
            underline.SetActive(false);

            // brass separator between entries — shown by runtime except after the last
            var sep = NewImage("Sep", tmpl, new Color(Brass.r, Brass.g, Brass.b, 0.35f));
            SetTopLeftBox(sep.GetComponent<RectTransform>(), (COL1_W-40)*0.2f, 58, (COL1_W-40)*0.6f, 1);

            tmpl.gameObject.SetActive(false);
            refs.middleEntryTemplate = tmpl.gameObject;
        }

        // ---- Right column — The Living --------------------------------------------

        static void BuildRightColumn(RectTransform sheet)
        {
            BuildColumnHead(sheet, COL2_X, COL2_W - 40, "Living",
                "\u091C\u094B \u092C\u091A\u0947", "The Living",
                out refs.rightHeadDeva, out refs.rightHeadEn);

            var container = NewRect("RightEntries", sheet);
            SetTopLeftBox(container, COL2_X, ENTRY_Y, COL2_W - 40, SHEET_H - ENTRY_Y - 180);
            var vlg = container.gameObject.AddComponent<VerticalLayoutGroup>();
            vlg.childControlWidth = true; vlg.childControlHeight = false;
            vlg.childForceExpandWidth = true; vlg.childForceExpandHeight = false;
            vlg.spacing = 18f;
            refs.rightEntriesContainer = container;

            var tmpl = NewRect("RightEntry_Template", container);
            var le = tmpl.gameObject.AddComponent<LayoutElement>();
            le.preferredHeight = 44;

            var line = NewText("NameState", tmpl, "",
                cormorant, 18, InkDeep, TextAlignmentOptions.MidlineLeft, FontStyles.Bold);
            SetTopLeftBox(line.rectTransform, 0, 0, COL2_W - 40, 24);

            var note = NewText("Note", tmpl, "",
                ebGaramond, 13, new Color(Ink.r, Ink.g, Ink.b, 0.65f), TextAlignmentOptions.TopLeft, FontStyles.Italic);
            SetTopLeftBox(note.rectTransform, 6, 26, COL2_W - 46, 18);
            note.gameObject.SetActive(false);

            tmpl.gameObject.SetActive(false);
            refs.rightEntryTemplate = tmpl.gameObject;
        }

        // ---- Footer ----------------------------------------------------------------

        static void BuildFooter(RectTransform sheet)
        {
            var cont = NewText("FooterContinue", sheet, "\u23CE    CONTINUE",
                cinzel, 13, InkDeep, TextAlignmentOptions.Right, FontStyles.Normal);
            cont.characterSpacing = 28;
            SetTopLeftBox(cont.rectTransform, SHEET_W - PAD_X - 260, SHEET_H - 110, 260, 24);
            refs.footerContinue = cont;
        }

        // =============================================================================
        // Helpers
        // =============================================================================

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
