using System.IO;
using ProjectAstra.Core.UI.UnitInfo;
using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

namespace ProjectAstra.EditorTools
{
    // ==========================================================================================
    // Unit Info Screen — full-screen 2-tab (STATS/GEAR) unit details, built as a prefab at
    //   Assets/UI/UnitInfoScreen/UnitInfoScreen.prefab
    //
    // FUNCTIONAL-FIRST: placeholder-grade chrome (solid parchment/navy panels, existing Cinzel
    // font, plain fill bars) laid out to the reference structure so navigation + data are
    // verifiable now. The Figma parchment art pass swaps sprites into these same GameObjects later.
    //
    // Attaches the golden-standard Views + composition root and wires every serialized reference.
    // ==========================================================================================
    public static class UnitInfoScreenBuilder
    {
        const float W = 1920f, H = 1080f;
        const string FontPath = "Assets/UI/UnitInfoPanel/Fonts/Cinzel SDF.asset";
        const string StatInfoPath = "Assets/ScriptableObjects/UI/StatInfoTable.asset";
        const string PrefabPath = "Assets/UI/UnitInfoScreen/UnitInfoScreen.prefab";

        static readonly Color Parchment = Hex("ede4cf");
        static readonly Color Navy       = Hex("26314f");
        static readonly Color NavyDeep   = Hex("1b2a4a");
        static readonly Color Blue       = Hex("2e5aa8");
        static readonly Color Ink        = Hex("2a2620");
        static readonly Color Cream      = Hex("f5efdf");
        static readonly Color BarFill    = Hex("3e6fc0");
        static readonly Color BarTrack   = Hex("c7bc9c");
        static readonly Color Gold       = Hex("c9a24a");
        static readonly Color Crimson    = Hex("9b342b");

        static TMP_FontAsset font;

        [MenuItem("Project Astra/Build Unit Info Screen (prefab)")]
        public static void BuildPrefab()
        {
            font = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(FontPath);

            var root = BuildHierarchy();

            if (!Directory.Exists(Path.GetDirectoryName(PrefabPath)))
                Directory.CreateDirectory(Path.GetDirectoryName(PrefabPath));
            PrefabUtility.SaveAsPrefabAsset(root, PrefabPath, out bool ok);
            Object.DestroyImmediate(root);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log(ok ? "UnitInfoScreen prefab saved to " + PrefabPath : "UnitInfoScreen prefab save FAILED");
        }

        static GameObject BuildHierarchy()
        {
            // Root — full canvas, always active (woken by BattleUIActivator); holds the controller.
            var root = new GameObject("UnitInfoScreen", typeof(RectTransform));
            SetStretch(root.GetComponent<RectTransform>());
            var controller = root.AddComponent<UnitInfoUIController>();

            // Panel — the visible content, toggled on state enter/exit.
            var panel = NewImage("Panel", root.transform, NavyDeep);
            SetStretch(panel.GetComponent<RectTransform>());
            var content = NewImage("Content", panel.transform, Parchment);
            SetStretchInset(content.GetComponent<RectTransform>(), 18);

            var tabBar   = BuildTopBar(content.transform);
            var summary  = BuildLeftColumn(content.transform);
            var stats    = BuildStatsTab(content.transform);
            var gear     = BuildGearTab(content.transform);
            var footer   = BuildFooter(content.transform);

            // Wire the composition root's serialized refs.
            var so = new SerializedObject(controller);
            so.FindProperty("panelRoot").objectReferenceValue = panel;
            so.FindProperty("summaryView").objectReferenceValue = summary;
            so.FindProperty("statsView").objectReferenceValue = stats;
            so.FindProperty("gearView").objectReferenceValue = gear;
            so.FindProperty("tabBarView").objectReferenceValue = tabBar;
            so.FindProperty("footerView").objectReferenceValue = footer;
            so.FindProperty("statInfo").objectReferenceValue =
                AssetDatabase.LoadAssetAtPath<StatInfoTable>(StatInfoPath);
            so.ApplyModifiedPropertiesWithoutUndo();

            return root;
        }

        // ---------------- top bar + tabs ----------------

        static UnitInfoTabBarView BuildTopBar(Transform parent)
        {
            var bar = NewRect("TopBar", parent);
            SetTopLeftBox(bar, 0, 0, W, 90);
            var view = bar.gameObject.AddComponent<UnitInfoTabBarView>();

            Text(NewText("Title", bar, "UNIT", 34, Ink, TextAlignmentOptions.MidlineLeft), 40, 24, 400, 44);

            view.StatsActive   = Tab(bar, 1250, "STATS", Blue, Cream);
            view.StatsInactive = Tab(bar, 1250, "STATS", Parchment, Ink);
            view.GearActive    = Tab(bar, 1470, "GEAR",  Blue, Cream);
            view.GearInactive  = Tab(bar, 1470, "GEAR",  Parchment, Ink);
            return view;
        }

        static GameObject Tab(RectTransform bar, float x, string label, Color bg, Color fg)
        {
            var t = NewImage("Tab_" + label, bar, bg);
            SetTopLeftBox(t.GetComponent<RectTransform>(), x, 20, 200, 50);
            var txt = NewText("Label", t.transform, label, 24, fg, TextAlignmentOptions.Center);
            SetStretch(txt.rectTransform);
            return t;
        }

        // ---------------- left column (summary) ----------------

        static UnitSummaryView BuildLeftColumn(Transform parent)
        {
            var col = NewRect("LeftColumn", parent);
            SetTopLeftBox(col, 40, 110, 560, 840);
            var v = col.gameObject.AddComponent<UnitSummaryView>();

            var portrait = NewImage("Portrait", col, Navy);
            SetTopLeftBox(portrait.GetComponent<RectTransform>(), 10, 10, 300, 340);
            v.Portrait = portrait.GetComponent<Image>();

            v.UnitName = Text(NewText("Name", col, "NAME", 46, Ink, TextAlignmentOptions.MidlineLeft), 330, 20, 230, 60);

            var classIcon = NewImage("ClassIcon", col, Navy);
            SetTopLeftBox(classIcon.GetComponent<RectTransform>(), 330, 110, 44, 44);
            v.ClassIcon = classIcon.GetComponent<Image>();
            v.ClassLabel = Text(NewText("Class", col, "CLASS", 24, Ink, TextAlignmentOptions.MidlineLeft), 384, 110, 176, 44);

            v.Level = Text(NewText("Level", col, "Lv 1", 26, Ink, TextAlignmentOptions.MidlineLeft), 330, 175, 230, 40);

            // HP block
            var hpBg = NewImage("HpBlock", col, Navy);
            SetTopLeftBox(hpBg.GetComponent<RectTransform>(), 10, 380, 540, 140);
            v.HpValue = Text(NewText("HpValue", hpBg.transform, "18 / 18", 40, Cream, TextAlignmentOptions.MidlineLeft), 20, 12, 500, 60);
            var hpTrack = NewImage("HpTrack", hpBg.transform, NavyDeep);
            SetTopLeftBox(hpTrack.GetComponent<RectTransform>(), 20, 82, 500, 34);
            v.HpFill = Fill(hpTrack.transform, BarFill);

            // EXP
            v.ExpRow = NewRect("ExpRow", col).gameObject;
            SetTopLeftBox((RectTransform)v.ExpRow.transform, 10, 540, 540, 46);
            Text(NewText("ExpLabel", v.ExpRow.transform, "EXP", 24, Ink, TextAlignmentOptions.MidlineLeft), 0, 6, 90, 34);
            v.ExpValue = Text(NewText("ExpValue", v.ExpRow.transform, "0 / 100", 24, Ink, TextAlignmentOptions.MidlineLeft), 100, 6, 140, 34);
            var expTrack = NewImage("ExpTrack", v.ExpRow.transform, BarTrack);
            SetTopLeftBox(expTrack.GetComponent<RectTransform>(), 250, 12, 290, 22);
            v.ExpFill = Fill(expTrack.transform, Gold);

            // Status
            v.StatusRow = NewRect("StatusRow", col).gameObject;
            SetTopLeftBox((RectTransform)v.StatusRow.transform, 10, 600, 540, 50);
            Text(NewText("StatusLabel", v.StatusRow.transform, "STATUS", 24, Ink, TextAlignmentOptions.MidlineLeft), 0, 8, 130, 34);
            var badge = NewImage("StatusBadge", v.StatusRow.transform, Crimson);
            SetTopLeftBox(badge.GetComponent<RectTransform>(), 140, 4, 220, 42);
            v.StatusText = Text(NewText("StatusText", badge.transform, "STRESSED", 22, Cream, TextAlignmentOptions.Center), 0, 0, 220, 42);
            SetStretch(v.StatusText.rectTransform);
            return v;
        }

        // ---------------- STATS tab ----------------

        static UnitStatsView BuildStatsTab(Transform parent)
        {
            var tab = NewRect("StatsRoot", parent);
            SetTopLeftBox(tab, 630, 110, 1250, 840);
            var v = tab.gameObject.AddComponent<UnitStatsView>();
            v.Root = tab.gameObject;

            // Equipped weapon panel
            var wp = NewImage("WeaponPanel", tab, Navy);
            SetTopLeftBox(wp.GetComponent<RectTransform>(), 0, 0, 1250, 96);
            v.WeaponPanel = wp;
            var wIcon = NewImage("WeaponIcon", wp.transform, NavyDeep);
            SetTopLeftBox(wIcon.GetComponent<RectTransform>(), 14, 14, 68, 68);
            v.WeaponIcon = wIcon.GetComponent<Image>();
            v.WeaponName = Text(NewText("WeaponName", wp.transform, "WEAPON", 30, Cream, TextAlignmentOptions.MidlineLeft), 100, 14, 400, 68);
            v.WeaponTypeLabel = Text(NewText("WeaponType", wp.transform, "BOW", 22, Cream, TextAlignmentOptions.Center), 520, 30, 120, 36);
            v.Mt  = Chip(wp.transform, 680, "MT",  "3");
            v.Hit = Chip(wp.transform, 820, "HIT", "85");
            v.Crt = Chip(wp.transform, 960, "CRT", "0");
            v.Rng = Chip(wp.transform, 1100, "RNG", "2");

            // 9 stat rows with group headers
            v.Rows = new UnitStatsView.StatRowWidgets[9];
            float y = 120;
            Header(tab, ref y, "ATTACK");
            v.Rows[0] = StatRow(tab, ref y);
            v.Rows[1] = StatRow(tab, ref y);
            v.Rows[2] = StatRow(tab, ref y);
            v.Rows[3] = StatRow(tab, ref y);
            Header(tab, ref y, "GUARD");
            v.Rows[4] = StatRow(tab, ref y);
            v.Rows[5] = StatRow(tab, ref y);
            Header(tab, ref y, "BODY");
            v.Rows[6] = StatRow(tab, ref y);
            v.Rows[7] = StatRow(tab, ref y);
            v.Rows[8] = StatRow(tab, ref y);
            return v;
        }

        static void Header(RectTransform parent, ref float y, string label)
        {
            Text(NewText("Group_" + label, parent, label, 22, Gold, TextAlignmentOptions.MidlineLeft), 0, y, 300, 30);
            y += 34;
        }

        static UnitStatsView.StatRowWidgets StatRow(RectTransform parent, ref float y)
        {
            var row = NewRect("StatRow", parent);
            SetTopLeftBox(row, 0, y, 1250, 48);
            y += 52;

            var hl = NewImage("Highlight", row, A(Blue, 0.35f));
            SetStretch(hl.GetComponent<RectTransform>());
            hl.SetActive(false);

            var icon = NewImage("Icon", row, A(Ink, 0.5f));
            SetTopLeftBox(icon.GetComponent<RectTransform>(), 8, 8, 32, 32);
            var label = Text(NewText("Label", row, "STAT", 24, Ink, TextAlignmentOptions.MidlineLeft), 52, 6, 220, 36);
            var value = Text(NewText("Value", row, "0", 26, Ink, TextAlignmentOptions.MidlineRight), 280, 6, 60, 36);
            var track = NewImage("Track", row, BarTrack);
            SetTopLeftBox(track.GetComponent<RectTransform>(), 360, 14, 760, 20);
            var fill = Fill(track.transform, BarFill);
            var cap = Text(NewText("Cap", row, "10", 24, Ink, TextAlignmentOptions.MidlineRight), 1140, 6, 60, 36);

            return new UnitStatsView.StatRowWidgets
            {
                Icon = icon.GetComponent<Image>(), Label = label, Value = value, Cap = cap, Fill = fill, Highlight = hl,
            };
        }

        static TextMeshProUGUI Chip(Transform parent, float x, string label, string val)
        {
            var chip = NewImage("Chip_" + label, parent, Parchment);
            SetTopLeftBox(chip.GetComponent<RectTransform>(), x, 30, 130, 40);
            Text(NewText("K", chip.transform, label, 18, Ink, TextAlignmentOptions.MidlineLeft), 12, 0, 60, 40);
            return Text(NewText("V", chip.transform, val, 22, Ink, TextAlignmentOptions.MidlineRight), 60, 0, 60, 40);
        }

        // ---------------- GEAR tab ----------------

        static UnitGearView BuildGearTab(Transform parent)
        {
            var tab = NewRect("GearRoot", parent);
            SetTopLeftBox(tab, 630, 110, 1250, 840);
            var v = tab.gameObject.AddComponent<UnitGearView>();
            v.Root = tab.gameObject;
            v.Slots = new UnitGearView.GearSlotWidgets[5];
            for (int i = 0; i < 5; i++)
                v.Slots[i] = GearSlot(tab, i * 150f);
            tab.gameObject.SetActive(false);   // STATS shown first
            return v;
        }

        static UnitGearView.GearSlotWidgets GearSlot(RectTransform parent, float y)
        {
            var slot = NewImage("GearSlot", parent, A(Navy, 0.12f));
            SetTopLeftBox(slot.GetComponent<RectTransform>(), 0, y, 1250, 130);

            var hl = NewImage("Highlight", slot.transform, A(Blue, 0.35f));
            SetStretch(hl.GetComponent<RectTransform>());
            hl.SetActive(false);

            var idx = Text(NewText("Index", slot.transform, "1", 30, Ink, TextAlignmentOptions.Center), 10, 44, 44, 44);
            var icon = NewImage("Icon", slot.transform, A(Ink, 0.4f));
            SetTopLeftBox(icon.GetComponent<RectTransform>(), 66, 20, 90, 90);
            var name = Text(NewText("Name", slot.transform, "ITEM", 30, Ink, TextAlignmentOptions.MidlineLeft), 176, 10, 500, 50);
            var badge = Text(NewText("Type", slot.transform, "BOW", 20, Ink, TextAlignmentOptions.Center), 176, 66, 110, 40);
            var uses = Text(NewText("Uses", slot.transform, "45 / 45", 24, Ink, TextAlignmentOptions.MidlineRight), 1050, 10, 180, 50);

            var chips = NewRect("Chips", slot.transform);
            SetTopLeftBox(chips, 300, 66, 740, 40);
            var mt  = ChipInline(chips, 0, "MT", "3");
            var hit = ChipInline(chips, 150, "HIT", "85");
            var crt = ChipInline(chips, 300, "CRT", "0");
            var rng = ChipInline(chips, 450, "RNG", "2");

            var equipped = NewImage("EquippedMark", slot.transform, Gold);
            SetTopLeftBox(equipped.GetComponent<RectTransform>(), 0, 0, 8, 130);
            equipped.SetActive(false);

            var empty = Text(NewText("Empty", slot.transform, "— — —", 26, A(Ink, 0.4f), TextAlignmentOptions.MidlineLeft), 176, 66, 300, 40).gameObject;

            return new UnitGearView.GearSlotWidgets
            {
                IndexLabel = idx, Icon = icon.GetComponent<Image>(), Name = name, Chips = chips.gameObject,
                TypeBadge = badge, Mt = mt, Hit = hit, Crt = crt, Rng = rng, Uses = uses,
                Highlight = hl, EquippedMark = equipped, EmptyState = empty,
            };
        }

        static TextMeshProUGUI ChipInline(Transform parent, float x, string label, string val)
        {
            var chip = NewImage("Chip_" + label, parent, Parchment);
            SetTopLeftBox(chip.GetComponent<RectTransform>(), x, 0, 140, 40);
            Text(NewText("K", chip.transform, label, 16, Ink, TextAlignmentOptions.MidlineLeft), 10, 0, 60, 40);
            return Text(NewText("V", chip.transform, val, 20, Ink, TextAlignmentOptions.MidlineRight), 66, 0, 64, 40);
        }

        // ---------------- footer ----------------

        static UnitInfoFooterView BuildFooter(Transform parent)
        {
            var f = NewImage("Footer", parent, A(Navy, 0.14f));
            SetTopLeftBox(f.GetComponent<RectTransform>(), 40, 968, 1840, 90);
            var v = f.gameObject.AddComponent<UnitInfoFooterView>();

            var icon = NewImage("Icon", f.transform, A(Ink, 0.4f));
            SetTopLeftBox(icon.GetComponent<RectTransform>(), 16, 14, 62, 62);
            v.Icon = icon.GetComponent<Image>();
            v.Title = Text(NewText("Title", f.transform, "", 26, Ink, TextAlignmentOptions.MidlineLeft), 96, 8, 500, 34);
            v.Description = Text(NewText("Desc", f.transform, "", 20, Ink, TextAlignmentOptions.TopLeft), 96, 44, 1100, 40);

            Text(NewText("Hints", f.transform, "B  BACK", 22, Ink, TextAlignmentOptions.MidlineRight), 1250, 28, 570, 40);
            return v;
        }

        // ---------------- helpers ----------------

        static Image Fill(Transform track, Color color)
        {
            var fill = NewImage("Fill", track, color);
            SetStretch(fill.GetComponent<RectTransform>());
            var img = fill.GetComponent<Image>();
            img.type = Image.Type.Filled;
            img.fillMethod = Image.FillMethod.Horizontal;
            img.fillOrigin = (int)Image.OriginHorizontal.Left;
            img.fillAmount = 0.5f;
            return img;
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

        static TextMeshProUGUI NewText(string name, Transform parent, string text, float size, Color color, TextAlignmentOptions align)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(TextMeshProUGUI));
            go.transform.SetParent(parent, false);
            var t = go.GetComponent<TextMeshProUGUI>();
            t.text = text;
            if (font != null) t.font = font;
            t.fontSize = size;
            t.color = color;
            t.alignment = align;
            t.enableWordWrapping = align == TextAlignmentOptions.TopLeft;
            t.overflowMode = TextOverflowModes.Overflow;
            t.raycastTarget = false;
            return t;
        }

        static TextMeshProUGUI Text(TextMeshProUGUI t, float x, float y, float w, float h)
        {
            SetTopLeftBox(t.rectTransform, x, y, w, h);
            return t;
        }

        static void SetTopLeftBox(RectTransform rt, float x, float y, float w, float h)
        {
            rt.anchorMin = rt.anchorMax = new Vector2(0, 1);
            rt.pivot = new Vector2(0, 1);
            rt.anchoredPosition = new Vector2(x, -y);
            rt.sizeDelta = new Vector2(w, h);
        }

        static void SetStretch(RectTransform rt)
        {
            rt.anchorMin = Vector2.zero; rt.anchorMax = Vector2.one;
            rt.pivot = new Vector2(0.5f, 0.5f);
            rt.offsetMin = Vector2.zero; rt.offsetMax = Vector2.zero;
        }

        static void SetStretchInset(RectTransform rt, float inset)
        {
            rt.anchorMin = Vector2.zero; rt.anchorMax = Vector2.one;
            rt.pivot = new Vector2(0.5f, 0.5f);
            rt.offsetMin = new Vector2(inset, inset); rt.offsetMax = new Vector2(-inset, -inset);
        }

        static Color A(Color c, float a) => new Color(c.r, c.g, c.b, a);

        static Color Hex(string hex)
        {
            byte r = byte.Parse(hex.Substring(0, 2), System.Globalization.NumberStyles.HexNumber);
            byte g = byte.Parse(hex.Substring(2, 2), System.Globalization.NumberStyles.HexNumber);
            byte b = byte.Parse(hex.Substring(4, 2), System.Globalization.NumberStyles.HexNumber);
            return new Color(r / 255f, g / 255f, b / 255f, 1f);
        }
    }
}
