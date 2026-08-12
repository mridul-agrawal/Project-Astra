using System.Collections.Generic;
using MPUIKIT;
using ProjectAstra.Core.UI.UnitInfo;
using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

namespace ProjectAstra.EditorTools
{
    // ==========================================================================================
    // Restyles the existing Unit Info Screen prefab to the signed-off Unit Stat Screen spec,
    // in place. Same approach as RestyleUnitCard, one screen up in scale.
    //
    // Nothing referenced is ever destroyed. Every widget the five Views point at is kept and
    // moved into its new home, new chrome is built around it, and the pass ends by checking that
    // each of those references is still alive - so the prefab can be restyled again and again
    // without re-wiring the BattleMap scene.
    //
    // Spec values are logical px in a 480x270 space. The project canvas is 1920x1080, an exact
    // 4x, so every number is the spec value multiplied by Scale.
    //
    // Run via 'Project Astra/Restyle Unit Info Screen'.
    // ==========================================================================================
    public static class RestyleUnitInfoScreen
    {
        const float Scale = 4f;
        static float Sc(float logical) => logical * Scale;

        const string PrefabPath = "Assets/UI/UnitInfoScreen/UnitInfoScreen.prefab";
        const string FontDir    = "Assets/UI/Shared/Fonts/";
        const string SpriteDir  = "Assets/UI/UnitInfoScreen/Generated/";
        const string ActedMat   = "Assets/UI/Shared/Materials/UnitCardActedPortrait.mat";
        const string BustPath   = "Assets/UI/UnitInfoScreen/Generated/bust_placeholder.png";

        // §2 palette.
        static readonly Color Bg             = Hex("14171d");
        static readonly Color BgRaised       = Hex("181c23");
        static readonly Color Edge           = Hex("3d4453");
        static readonly Color TextBody       = Hex("cfd5de");
        static readonly Color TextBright     = Color.white;
        static readonly Color TextDim        = Hex("aeb6c4");
        static readonly Color TextFaint      = Hex("8b93a0");
        static readonly Color TextMicro      = Hex("79808c");
        static readonly Color TextGhost      = Hex("666e7b");
        static readonly Color AllyAccent     = Hex("4f9dff");
        static readonly Color Gold           = Hex("e8b34b");
        static readonly Color BarTrack       = Hex("272d38");
        static readonly Color BarTrackBorder = Hex("10141a");
        static readonly Color GroupAttack    = Hex("d09a4a");
        static readonly Color GroupGuard     = Hex("6f9fdc");
        static readonly Color GroupBody      = Hex("79b579");
        static readonly Color PortraitBorder = Hex("4a5261");
        static readonly Color PortraitBg     = Hex("232833");
        static readonly Color Diamond        = Hex("5f6672");
        static readonly Color LevelText      = Hex("c3c9d3");
        static readonly Color ActedChipBg    = Hex("3a3f4a");
        static readonly Color SlotCompact    = Hex("171b22");
        static readonly Color SlotName       = Hex("dfe4ec");
        static readonly Color EmptyLabel     = Hex("4a5261");
        static readonly Color BandFill       = new Color32(0x0d, 0x10, 0x15, 204);   // .8 alpha
        static readonly Color BandHairline   = new Color32(0xae, 0xb6, 0xc4, 89);    // .35 alpha

        // §1 geometry, in canvas px.
        static readonly float HeaderHeight = Sc(21f);
        static readonly float FooterHeight = Sc(28f);
        static readonly float BandPadX     = Sc(8f);
        static readonly float BodyPad      = Sc(6f);
        static readonly float ColumnWidth  = Sc(142f);
        static readonly float Gap          = Sc(5f);
        static readonly float Hairline     = Sc(1f);
        static readonly float RowHeight    = Sc(13f);
        static readonly float BarHeight    = Sc(7f);

        const float CanvasW = 1920f, CanvasH = 1080f;

        static readonly List<Object> tracked = new();
        static readonly List<(Object owner, string path)> wiredFields = new();
        static readonly HashSet<GameObject> touched = new();

        [MenuItem("Project Astra/Restyle Unit Info Screen")]
        public static void Restyle()
        {
            GameObject root = PrefabUtility.LoadPrefabContents(PrefabPath);
            if (root == null)
            {
                Debug.LogError("[RestyleUnitInfoScreen] Could not load " + PrefabPath);
                return;
            }

            try
            {
                RestyleContents(root);
            }
            finally
            {
                PrefabUtility.SaveAsPrefabAsset(root, PrefabPath);
                PrefabUtility.UnloadPrefabContents(root);
            }

            AssetDatabase.SaveAssets();
            Debug.Log("[RestyleUnitInfoScreen] Unit Info screen restyled to spec.");
        }

        static void RestyleContents(GameObject root)
        {
            touched.Clear();
            Touch(root);

            var controller = root.GetComponent<UnitInfoUIController>();
            var summary = root.GetComponentInChildren<UnitSummaryView>(true);
            var stats   = root.GetComponentInChildren<UnitStatsView>(true);
            var gear    = root.GetComponentInChildren<UnitGearView>(true);
            var tabs    = root.GetComponentInChildren<UnitInfoTabBarView>(true);
            var footer  = root.GetComponentInChildren<UnitInfoFooterView>(true);

            TrackReferences(controller, summary, stats, gear, tabs, footer);

            GameObject panel = StyleShell(root, controller);
            GameObject body = EnsureChild(panel, "Body");
            PlaceBody(Rect(body));

            StyleHeader(tabs, panel);
            StyleIdentityColumn(summary, body);
            StyleStatsTab(stats, body);
            StyleGearTab(gear, body);
            StyleFooter(footer, panel);

            GameObject marker = BuildFocusMarker(body);
            WireController(controller, panel, marker, summary, stats);

            PruneStaleDecoration(root);
            VerifyReferencesSurvived();
        }

        // The prefab was built to an older layout, so it carries panels and badges the spec has no
        // place for. Anything this pass never touched but that still draws is switched off - which
        // keeps the old parchment chrome from showing through without destroying a single object.
        static void PruneStaleDecoration(GameObject root)
        {
            int hidden = 0;
            var names = new List<string>();
            foreach (var graphic in root.GetComponentsInChildren<Graphic>(true))
            {
                GameObject go = graphic.gameObject;
                if (touched.Contains(go) || go == root) continue;

                go.SetActive(false);
                hidden++;
                names.Add(Path(go));
            }
            if (hidden > 0)
                Debug.Log($"[RestyleUnitInfoScreen] Hid {hidden} leftover graphic(s): " + string.Join(", ", names));
        }

        // ---- reference safety ----------------------------------------------------------------

        // Which View field held something before the pass ran. The check afterwards is that every
        // one of them still holds something - the invariant that keeps the BattleMap scene working.
        //
        // Recording the field rather than the object it pointed at is deliberate: swapping a plain
        // Image for MPUIKit's procedural one destroys the old component on purpose and re-points
        // the field at the replacement. That is a healthy reference, not a broken one.
        static void TrackReferences(params Object[] components)
        {
            tracked.Clear();
            wiredFields.Clear();

            foreach (var component in components)
            {
                if (component == null) continue;
                CollectFrom(component);
            }
        }

        static void CollectFrom(Object component)
        {
            var iterator = new SerializedObject(component).GetIterator();
            while (iterator.Next(true))
            {
                if (iterator.propertyType != SerializedPropertyType.ObjectReference) continue;
                if (iterator.objectReferenceValue == null) continue;
                if (!(iterator.objectReferenceValue is GameObject || iterator.objectReferenceValue is Component))
                    continue;

                tracked.Add(iterator.objectReferenceValue);
                wiredFields.Add((component, iterator.propertyPath));
            }
        }

        static void VerifyReferencesSurvived()
        {
            var emptied = new List<string>();
            foreach (var (component, path) in wiredFields)
            {
                var property = new SerializedObject(component).FindProperty(path);
                if (property != null && property.objectReferenceValue == null)
                    emptied.Add(component.GetType().Name + "." + path);
            }

            if (emptied.Count > 0)
                Debug.LogError($"[RestyleUnitInfoScreen] {emptied.Count} serialized reference(s) came out empty - " +
                               "the prefab was NOT styled safely: " + string.Join(", ", emptied));
            else
                Debug.Log($"[RestyleUnitInfoScreen] All {wiredFields.Count} serialized references still wired.");
        }

        // ---- §1 shell ------------------------------------------------------------------------

        static GameObject StyleShell(GameObject root, UnitInfoUIController controller)
        {
            Stretch(Rect(root));

            GameObject panel = FindChild(root, "Panel") ?? EnsureChild(root, "Panel");
            Stretch(Rect(panel));
            var background = EnsureImage(panel);
            background.color = Bg;
            background.sprite = null;

            EnsureComponent<CanvasGroup>(panel);
            EnsureComponent<UnitInfoScreenTransition>(panel);
            return panel;
        }

        static void PlaceBody(RectTransform body)
        {
            body.anchorMin = body.anchorMax = body.pivot = new Vector2(0f, 1f);
            body.anchoredPosition = new Vector2(BodyPad, -(HeaderHeight + BodyPad));
            body.sizeDelta = new Vector2(CanvasW - BodyPad * 2f,
                                         CanvasH - HeaderHeight - FooterHeight - BodyPad * 2f);
        }

        // ---- §3 header -----------------------------------------------------------------------

        static void StyleHeader(UnitInfoTabBarView tabs, GameObject panel)
        {
            if (tabs == null) return;

            GameObject band = tabs.gameObject;
            Touch(band);
            band.transform.SetParent(panel.transform, false);
            TopLeft(Rect(band), 0f, 0f, CanvasW, HeaderHeight);

            var fill = EnsureImage(band);
            fill.color = BgRaised;
            fill.sprite = null;
            BottomHairline(band);

            StyleHeaderTitle(band);
            StyleTabChips(tabs, band);
        }

        static void StyleHeaderTitle(GameObject band)
        {
            GameObject row = EnsureChild(band, "TitleRow");
            TopLeft(Rect(row), BandPadX, 0f, Sc(200f), HeaderHeight);
            ConfigureRow(row, Gap, TextAnchor.MiddleLeft);

            var title = FindLabel(band, "Title");
            if (title != null)
            {
                Reparent(title.gameObject, row, 0);
                ApplyFont(title, "Bold", Sc(9f), TextBright);
                title.text = "UNIT";
                title.characterSpacing = 18f;       // .18em
                NoWrap(title);
            }

            var pip = EnsureChildImage(row, "Diamond", 1);
            Rect(pip.gameObject).sizeDelta = new Vector2(Sc(4f), Sc(4f));
            pip.color = Diamond;
            pip.sprite = Glyph("glyph_diamond");
            EnsureLayoutSize(pip.gameObject, Sc(4f), Sc(4f));

            var name = EnsureChildLabel(row, "HeaderUnitName");
            ApplyFont(name, "Regular", Sc(7f), TextFaint);
            name.text = "";
            NoWrap(name);
            EnsureLayoutSize(name.gameObject, Sc(60f), Sc(11f));
        }

        // The tab bar keeps its four objects - an active and an inactive face per tab, one shown
        // at a time - so they are stacked in a shared holder rather than laid out side by side.
        static void StyleTabChips(UnitInfoTabBarView tabs, GameObject band)
        {
            GameObject cluster = EnsureChild(band, "Controls");
            var rect = Rect(cluster);
            rect.anchorMin = new Vector2(1f, 1f);
            rect.anchorMax = new Vector2(1f, 1f);
            rect.pivot = new Vector2(1f, 1f);
            rect.anchoredPosition = new Vector2(-BandPadX, 0f);
            rect.sizeDelta = new Vector2(Sc(200f), HeaderHeight);
            ConfigureRow(cluster, Gap, TextAnchor.MiddleRight);
            EnsureComponent<ContentSizeFitter>(cluster).horizontalFit = ContentSizeFitter.FitMode.PreferredSize;

            Keycap(cluster, "KeycapQ", "Q", 0);
            GameObject statsHolder = TabHolder(cluster, "TabStats", 1);
            GameObject gearHolder = TabHolder(cluster, "TabGear", 2);
            Keycap(cluster, "KeycapE", "E", 3);
            Keycap(cluster, "KeycapClose", "X CLOSE", 4);

            StyleTabFace(tabs.StatsActive, statsHolder, "STATS", true);
            StyleTabFace(tabs.StatsInactive, statsHolder, "STATS", false);
            StyleTabFace(tabs.GearActive, gearHolder, "GEAR", true);
            StyleTabFace(tabs.GearInactive, gearHolder, "GEAR", false);
        }

        static GameObject TabHolder(GameObject parent, string name, int index)
        {
            GameObject holder = EnsureChild(parent, name);
            holder.transform.SetSiblingIndex(index);
            float width = name == "TabStats" ? Sc(46f) : Sc(40f);
            EnsureLayoutSize(holder, width, Sc(13f));
            Rect(holder).sizeDelta = new Vector2(width, Sc(13f));
            return holder;
        }

        static void StyleTabFace(GameObject face, GameObject holder, string label, bool active)
        {
            if (face == null) return;

            Touch(face);
            face.transform.SetParent(holder.transform, false);
            Stretch(Rect(face));

            var chip = EnsureMpImage(face);
            chip.sprite = null;
            chip.color = active ? AllyAccent : Edge;
            SetCornerRadius(chip, 0f);
            SetStroke(chip, active ? 0f : Hairline);
            SetOutline(chip, 0f, Color.clear);

            var text = FindLabel(face, "Label") ?? EnsureChildLabel(face, "Label");
            Stretch(Rect(text.gameObject));
            ApplyFont(text, active ? "Bold" : "Regular", Sc(7f), active ? TextBright : TextFaint);
            text.text = label;
            text.characterSpacing = 10f;            // .1em
            text.alignment = TextAlignmentOptions.Center;
            NoWrap(text);
        }

        static void Keycap(GameObject parent, string name, string label, int index)
        {
            GameObject cap = EnsureChild(parent, name);
            cap.transform.SetSiblingIndex(index);

            var frame = HairlineFrame(cap, Edge);
            SetCornerRadius(frame, Sc(2f));
            SetStroke(frame, Hairline);

            var text = EnsureChildLabel(cap, "Label");
            Stretch(Rect(text.gameObject));
            ApplyFont(text, "Regular", Sc(6f), TextMicro);
            text.text = label;
            text.alignment = TextAlignmentOptions.Center;
            NoWrap(text);

            float width = Sc(6f) * label.Length * 0.62f + Sc(6f);
            EnsureLayoutSize(cap, width, Sc(11f));
        }

        // ---- §4 identity column ---------------------------------------------------------------

        static void StyleIdentityColumn(UnitSummaryView view, GameObject body)
        {
            if (view == null) return;

            GameObject column = view.gameObject;
            Touch(column);
            column.transform.SetParent(body.transform, false);
            float height = Rect(body).sizeDelta.y;
            TopLeft(Rect(column), 0f, 0f, ColumnWidth, height);

            var frame = HairlineFrame(column, Edge);

            float inner = ColumnWidth - Sc(6f) * 2f;
            float clusterHeight = Sc(66f);
            float portraitHeight = height - Sc(6f) * 2f - Gap - clusterHeight;

            GameObject portrait = StylePortrait(view, column, inner, portraitHeight);
            StyleIdentityBand(view, portrait, inner);
            StyleVitalCluster(view, column, inner, clusterHeight, height);
        }

        static GameObject StylePortrait(UnitSummaryView view, GameObject column, float width, float height)
        {
            GameObject frame = EnsureChild(column, "PortraitFrame");
            frame.transform.SetSiblingIndex(0);
            TopLeft(Rect(frame), Sc(6f), Sc(6f), width, height);

            var backdrop = EnsureImage(frame);
            backdrop.color = PortraitBg;
            backdrop.sprite = null;
            SetOutline(backdrop, Hairline, PortraitBorder);

            if (view.Portrait != null)
            {
                Reparent(view.Portrait.gameObject, frame, 0);
                Stretch(Rect(view.Portrait.gameObject));
                view.Portrait.preserveAspect = false;
                view.Portrait.sprite = Load<Sprite>(BustPath);
                // The old panel tinted the portrait navy, which multiplies real art down to black.
                view.Portrait.color = Color.white;
            }
            return frame;
        }

        // §4 overlays the name band on the portrait's lower edge rather than below it.
        static void StyleIdentityBand(UnitSummaryView view, GameObject portrait, float width)
        {
            GameObject band = EnsureChild(portrait, "IdentityBand");
            var rect = Rect(band);
            rect.anchorMin = new Vector2(0f, 0f);
            rect.anchorMax = new Vector2(1f, 0f);
            rect.pivot = new Vector2(0.5f, 0f);
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = new Vector2(0f, Sc(20f));

            var fill = EnsureImage(band);
            fill.color = BandFill;
            fill.sprite = null;
            TopHairline(band, BandHairline);

            GameObject row = EnsureChild(band, "Row");
            Stretch(Rect(row));
            Rect(row).offsetMin = new Vector2(Sc(5f), Sc(3f));
            Rect(row).offsetMax = new Vector2(-Sc(5f), -Sc(3f));
            ConfigureRow(row, Sc(4f), TextAnchor.MiddleLeft);

            PlaceIdentityName(view, row);
            PlaceClassChip(view, row);
            PlaceLevel(view, row);
            PlaceActedChip(view, row);
        }

        static void PlaceIdentityName(UnitSummaryView view, GameObject row)
        {
            if (view.UnitName == null) return;

            Reparent(view.UnitName.gameObject, row, 0);
            ApplyFont(view.UnitName, "Bold", Sc(10f), TextBright);
            view.UnitName.text = "NAME";
            NoWrap(view.UnitName);
            view.UnitName.overflowMode = TextOverflowModes.Ellipsis;
            EnsureLayoutSize(view.UnitName.gameObject, Sc(52f), Sc(15f));
        }

        static void PlaceClassChip(UnitSummaryView view, GameObject row)
        {
            if (view.ClassLabel == null) return;

            GameObject chip = EnsureChild(row, "ClassChip");
            chip.transform.SetSiblingIndex(1);
            var frame = HairlineFrame(chip, Edge);

            Reparent(view.ClassLabel.gameObject, chip, 0);
            Stretch(Rect(view.ClassLabel.gameObject));
            Rect(view.ClassLabel.gameObject).offsetMin = new Vector2(Sc(4f), 0f);
            Rect(view.ClassLabel.gameObject).offsetMax = new Vector2(-Sc(4f), 0f);
            ApplyFont(view.ClassLabel, "Regular", Sc(6f), TextDim);
            view.ClassLabel.alignment = TextAlignmentOptions.Center;
            NoWrap(view.ClassLabel);
            EnsureLayoutSize(chip, Sc(34f), Sc(9f));

            // The spec's band has no class icon, so it stays wired but out of the way.
            if (view.ClassIcon != null) view.ClassIcon.gameObject.SetActive(false);
        }

        static void PlaceLevel(UnitSummaryView view, GameObject row)
        {
            if (view.Level == null) return;

            Reparent(view.Level.gameObject, row, 2);
            ApplyFont(view.Level, "Regular", Sc(7f), LevelText);
            view.Level.text = "LV 1";
            NoWrap(view.Level);
            EnsureLayoutSize(view.Level.gameObject, Sc(22f), Sc(11f));
        }

        static void PlaceActedChip(UnitSummaryView view, GameObject row)
        {
            GameObject chip = EnsureChild(row, "ActedChip");
            chip.transform.SetSiblingIndex(3);
            var fill = EnsureImage(chip);
            fill.color = ActedChipBg;
            fill.sprite = null;

            var text = EnsureChildLabel(chip, "Label");
            Stretch(Rect(text.gameObject));
            ApplyFont(text, "Bold", Sc(5.5f), TextDim);
            text.text = "ACTED";
            text.alignment = TextAlignmentOptions.Center;
            NoWrap(text);

            EnsureLayoutSize(chip, Sc(24f), Sc(8f));
            view.ActedChip = chip;
            chip.SetActive(false);
        }

        static void StyleVitalCluster(UnitSummaryView view, GameObject column,
                                      float width, float height, float columnHeight)
        {
            GameObject cluster = EnsureChild(column, "VitalCluster");
            TopLeft(Rect(cluster), Sc(6f), columnHeight - Sc(6f) - height, width, height);

            var frame = HairlineFrame(cluster, Edge);

            float hpHeight = Sc(37f);
            StyleHpBlock(view, cluster, width, hpHeight);
            StyleQuietRows(view, cluster, width, hpHeight, height);
        }

        static void StyleHpBlock(UnitSummaryView view, GameObject cluster, float width, float height)
        {
            GameObject block = EnsureChild(cluster, "HpBlock");
            block.transform.SetSiblingIndex(0);
            TopLeft(Rect(block), 0f, 0f, width, height);

            var wash = EnsureImage(block);
            wash.color = new Color(AllyAccent.r, AllyAccent.g, AllyAccent.b, 0.15f);
            wash.sprite = null;
            BottomHairline(block);
            view.HpBlockBackground = wash;

            var label = EnsureChildLabel(block, "HpLabel");
            TopLeft(Rect(label.gameObject), Sc(5f), Sc(4f), Sc(20f), Sc(7f));
            ApplyFont(label, "Regular", Sc(6f), TextMicro);
            label.text = "HP";
            label.characterSpacing = 12f;           // .12em
            NoWrap(label);

            StyleHpNumerals(view, block);
            StyleHpBar(view, block, width);
        }

        // The current value and its "/ max" sit on one baseline, at the two sizes §4 asks for.
        static void StyleHpNumerals(UnitSummaryView view, GameObject block)
        {
            GameObject row = EnsureChild(block, "HpNumerals");
            TopLeft(Rect(row), Sc(5f), Sc(11f), Sc(120f), Sc(15f));
            ConfigureRow(row, Sc(3f), TextAnchor.LowerLeft);

            if (view.HpValue != null)
            {
                Reparent(view.HpValue.gameObject, row, 0);
                ApplyFont(view.HpValue, "Bold", Sc(13f), TextBright);
                view.HpValue.text = "18";
                BaselineRun(view.HpValue);
            }

            var max = EnsureChildLabel(row, "HpMax");
            ApplyFont(max, "Regular", Sc(8f), TextFaint);
            max.text = "/ 18";
            BaselineRun(max);
            view.HpMax = max;
        }

        static void StyleHpBar(UnitSummaryView view, GameObject block, float width)
        {
            if (view.HpFill == null) return;

            GameObject track = view.HpFill.transform.parent != null
                ? view.HpFill.transform.parent.gameObject
                : EnsureChild(block, "HpTrack");
            Reparent(track, block, track.transform.GetSiblingIndex());
            TopLeft(Rect(track), Sc(5f), Sc(27f), width - Sc(10f), BarHeight);
            StyleBarTrack(track);

            Stretch(Rect(view.HpFill.gameObject));
            var fill = EnsurePlainImage(view.HpFill.gameObject);
            fill.color = AllyAccent;
            AsHorizontalFill(fill);
            RewireImage(view, "HpFill", fill);
        }

        static void StyleQuietRows(UnitSummaryView view, GameObject cluster,
                                   float width, float top, float clusterHeight)
        {
            GameObject rows = EnsureChild(cluster, "QuietRows");
            TopLeft(Rect(rows), 0f, top + Hairline, width, clusterHeight - top - Hairline);
            var column = EnsureComponent<VerticalLayoutGroup>(rows);
            column.padding = new RectOffset((int)Sc(5f), (int)Sc(5f), (int)Sc(4f), (int)Sc(4f));
            column.spacing = Sc(4f);
            column.childAlignment = TextAnchor.UpperLeft;
            column.childControlWidth = true;
            column.childControlHeight = true;
            column.childForceExpandWidth = true;
            column.childForceExpandHeight = false;

            StyleExpRow(view, rows, width);
            StyleStatusRow(view, rows, width);
        }

        static void StyleExpRow(UnitSummaryView view, GameObject rows, float width)
        {
            if (view.ExpRow == null) return;

            Reparent(view.ExpRow, rows, 0);
            ClearGraphic(view.ExpRow.GetComponent<Image>());
            ConfigureRow(view.ExpRow, Sc(4f), TextAnchor.MiddleLeft);
            EnsureLayoutSize(view.ExpRow, width, Sc(7f));

            var label = FindLabel(view.ExpRow, "ExpLabel") ?? EnsureChildLabel(view.ExpRow, "ExpLabel");
            Reparent(label.gameObject, view.ExpRow, 0);
            ApplyFont(label, "Regular", Sc(6f), TextMicro);
            label.text = "EXP";
            NoWrap(label);
            EnsureLayoutSize(label.gameObject, Sc(18f), Sc(7f));

            StyleExpBar(view, width);

            if (view.ExpValue != null)
            {
                Reparent(view.ExpValue.gameObject, view.ExpRow, 2);
                ApplyFont(view.ExpValue, "Regular", Sc(6f), TextDim);
                view.ExpValue.alignment = TextAlignmentOptions.MidlineRight;
                NoWrap(view.ExpValue);
                EnsureLayoutSize(view.ExpValue.gameObject, Sc(28f), Sc(7f));
            }
        }

        static void StyleExpBar(UnitSummaryView view, float width)
        {
            if (view.ExpFill == null) return;

            GameObject track = view.ExpFill.transform.parent.gameObject;
            Reparent(track, view.ExpRow, 1);
            StyleBarTrack(track);
            EnsureLayoutSize(track, width - Sc(56f), Sc(4f));

            Stretch(Rect(view.ExpFill.gameObject));
            var fill = EnsurePlainImage(view.ExpFill.gameObject);
            fill.color = new Color(AllyAccent.r, AllyAccent.g, AllyAccent.b, 0.7f);
            AsHorizontalFill(fill);
            RewireImage(view, "ExpFill", fill);
        }

        static void StyleStatusRow(UnitSummaryView view, GameObject rows, float width)
        {
            if (view.StatusRow == null) return;

            Reparent(view.StatusRow, rows, 1);
            ClearGraphic(view.StatusRow.GetComponent<Image>());
            ConfigureRow(view.StatusRow, Sc(4f), TextAnchor.MiddleLeft);
            EnsureLayoutSize(view.StatusRow, width, Sc(9f));

            // The old badge wrapper is not in the spec, so the label comes out of it and the
            // wrapper is left behind, still wired, just switched off.
            var oldBadge = FindChild(view.StatusRow, "StatusBadge");
            if (view.StatusText != null)
            {
                Reparent(view.StatusText.gameObject, view.StatusRow, 0);
                ApplyFont(view.StatusText, "Regular", Sc(6f), TextMicro);
                view.StatusText.text = "STATUS";
                view.StatusText.alignment = TextAlignmentOptions.MidlineLeft;
                NoWrap(view.StatusText);
                EnsureLayoutSize(view.StatusText.gameObject, Sc(30f), Sc(9f));
            }
            if (oldBadge != null) oldBadge.SetActive(false);

            var dash = EnsureChildLabel(view.StatusRow, "StatusEmpty");
            ApplyFont(dash, "Regular", Sc(7f), Diamond);
            dash.text = "—";
            NoWrap(dash);
            EnsureLayoutSize(dash.gameObject, Sc(8f), Sc(9f));
            view.StatusEmpty = dash;

            BuildStatusChips(view, view.StatusRow);
            WireStatusStyles(view);
        }

        static void BuildStatusChips(UnitSummaryView view, GameObject row)
        {
            const int poolSize = 4;
            var chips = new UnitSummaryView.StatusChipWidgets[poolSize];

            for (int i = 0; i < poolSize; i++)
            {
                GameObject chip = EnsureChild(row, "StatusChip" + i);
                chip.transform.SetSiblingIndex(2 + i);
                EnsureLayoutSize(chip, Sc(12f), Sc(9f));

                var fill = EnsureImage(chip);
                fill.sprite = null;

                var border = EnsureChildImage(chip, "Border", 0);
                Stretch(Rect(border.gameObject));
                var stroke = EnsureMpImage(border.gameObject);
                stroke.sprite = null;
                SetCornerRadius(stroke, 0f);
                SetStroke(stroke, Hairline);

                var glyph = EnsureChildImage(chip, "Glyph", 1);
                CenterBox(Rect(glyph.gameObject), Sc(6f), Sc(6f));

                chips[i] = new UnitSummaryView.StatusChipWidgets
                {
                    Root = chip, Fill = fill, Border = stroke, Glyph = glyph,
                };
                chip.SetActive(false);
            }
            view.StatusChips = chips;
        }

        // §2 names three statuses; stress is the one the game actually tracks, so it takes the
        // danger red and a ring while the spec's three keep their own colours and glyphs.
        static void WireStatusStyles(UnitSummaryView view)
        {
            view.StatusStyles = new[]
            {
                Style(UnitStatusKind.Stressed, "glyph_ring", Hex("e0524f")),
                Style(UnitStatusKind.Poisoned, "glyph_triangle_down", Hex("a05ac9")),
                Style(UnitStatusKind.Slowed, "glyph_diamond", Hex("5a9fd0")),
                Style(UnitStatusKind.Shielded, "glyph_square", Hex("58b06a")),
            };
        }

        static UnitSummaryView.StatusStyle Style(UnitStatusKind kind, string glyph, Color tint) =>
            new UnitSummaryView.StatusStyle { Kind = kind, Glyph = Glyph(glyph), Tint = tint };

        // ---- §5 and §6 stats tab --------------------------------------------------------------

        static void StyleStatsTab(UnitStatsView view, GameObject body)
        {
            if (view == null) return;

            GameObject tab = view.gameObject;
            Touch(tab);
            tab.transform.SetParent(body.transform, false);
            float width = Rect(body).sizeDelta.x - ColumnWidth - Gap;
            float height = Rect(body).sizeDelta.y;
            TopLeft(Rect(tab), ColumnWidth + Gap, 0f, width, height);
            EnsureComponent<CanvasGroup>(tab);
            view.Root = tab;

            float cardHeight = Sc(32f);
            StyleWeaponCard(view, tab, width, cardHeight);
            StyleStatGroups(view, tab, width, cardHeight + Sc(3f) + Sc(4f));
        }

        static void StyleWeaponCard(UnitStatsView view, GameObject tab, float width, float height)
        {
            if (view.WeaponPanel == null) return;

            Reparent(view.WeaponPanel, tab, 0);
            TopLeft(Rect(view.WeaponPanel), 0f, Sc(3f), width, height);

            var frame = HairlineFrame(view.WeaponPanel, Edge);

            FloatingLabel(view.WeaponPanel, "EQUIPPED WEAPON");
            StyleWeaponIdentity(view, width);
            StyleWeaponChips(view, width);
            view.WeaponHighlight = null;        // the moving §8 marker covers the card too
        }

        // §5 hangs the caption on the frame's top edge, sitting on a patch of the screen colour
        // so the hairline appears to run behind it.
        static void FloatingLabel(GameObject frame, string text)
        {
            GameObject patch = EnsureChild(frame, "FloatingLabel");
            TopLeft(Rect(patch), Sc(6f), -Sc(4f), Sc(72f), Sc(8f));

            var background = EnsureImage(patch);
            background.color = Bg;
            background.sprite = null;

            var label = EnsureChildLabel(patch, "Label");
            Stretch(Rect(label.gameObject));
            Rect(label.gameObject).offsetMin = new Vector2(Sc(2f), 0f);
            Rect(label.gameObject).offsetMax = new Vector2(-Sc(2f), 0f);
            ApplyFont(label, "Regular", Sc(5.5f), TextMicro);
            label.text = text;
            label.characterSpacing = 14f;           // .14em
            label.alignment = TextAlignmentOptions.MidlineLeft;
            NoWrap(label);
        }

        static void StyleWeaponIdentity(UnitStatsView view, float width)
        {
            GameObject row = EnsureChild(view.WeaponPanel, "WeaponIdentity");
            TopLeft(Rect(row), Sc(6f), Sc(6f), width * 0.5f, Sc(20f));
            ConfigureRow(row, Gap, TextAnchor.MiddleLeft);

            if (view.WeaponIcon != null)
            {
                GameObject box = EnsureChild(row, "IconBox");
                box.transform.SetSiblingIndex(0);
                EnsureLayoutSize(box, Sc(20f), Sc(20f));
                var boxFrame = HairlineFrame(box, Edge);

                Reparent(view.WeaponIcon.gameObject, box, 0);
                CenterBox(Rect(view.WeaponIcon.gameObject), Sc(10f), Sc(10f));
                view.WeaponIcon.color = Gold;
                view.WeaponIcon.sprite = Glyph("glyph_blade");
            }

            if (view.WeaponName != null)
            {
                Reparent(view.WeaponName.gameObject, row, 1);
                ApplyFont(view.WeaponName, "Bold", Sc(9f), TextBright);
                NoWrap(view.WeaponName);
                view.WeaponName.overflowMode = TextOverflowModes.Ellipsis;
                EnsureLayoutSize(view.WeaponName.gameObject, Sc(64f), Sc(15f));
            }

            if (view.WeaponTypeLabel != null)
            {
                GameObject badge = EnsureChild(row, "TypeBadge");
                badge.transform.SetSiblingIndex(2);
                var badgeFrame = HairlineFrame(badge, TextMicro);
                EnsureLayoutSize(badge, Sc(30f), Sc(9f));

                Reparent(view.WeaponTypeLabel.gameObject, badge, 0);
                Stretch(Rect(view.WeaponTypeLabel.gameObject));
                ApplyFont(view.WeaponTypeLabel, "Regular", Sc(6f), TextDim);
                view.WeaponTypeLabel.alignment = TextAlignmentOptions.Center;
                NoWrap(view.WeaponTypeLabel);
            }
        }

        static void StyleWeaponChips(UnitStatsView view, float width)
        {
            GameObject row = EnsureChild(view.WeaponPanel, "WeaponChips");
            var rect = Rect(row);
            rect.anchorMin = new Vector2(1f, 1f);
            rect.anchorMax = new Vector2(1f, 1f);
            rect.pivot = new Vector2(1f, 1f);
            rect.anchoredPosition = new Vector2(-Sc(6f), -Sc(11f));
            rect.sizeDelta = new Vector2(width * 0.45f, Sc(10f));
            ConfigureRow(row, Sc(4f), TextAnchor.MiddleRight);
            EnsureComponent<ContentSizeFitter>(row).horizontalFit = ContentSizeFitter.FitMode.PreferredSize;

            StatChip(row, view.Mt, "MT", 0);
            StatChip(row, view.Hit, "HIT", 1);
            StatChip(row, view.Crt, "CRT", 2);
            StatChip(row, view.Rng, "RNG", 3);
        }

        // The value label is the wired one; its chip frame and key label are rebuilt around it.
        static void StatChip(GameObject row, TextMeshProUGUI value, string key, int index)
        {
            if (value == null) return;

            GameObject chip = AncestorNamed(value.transform, "Chip_") ?? EnsureChild(row, "Chip_" + key);
            Reparent(chip, row, index);

            HairlineFrame(chip, Edge);
            ChipBox(chip);

            var keyLabel = FindLabel(chip, "K") ?? EnsureChildLabel(chip, "K");
            Reparent(keyLabel.gameObject, chip, 0);
            ApplyFont(keyLabel, "Regular", Sc(6f), TextDim);
            keyLabel.text = key;
            NoWrap(keyLabel);

            Reparent(value.gameObject, chip, 1);
            ApplyFont(value, "Bold", Sc(7f), TextBright);
            NoWrap(value);
        }

        static void StyleStatGroups(UnitStatsView view, GameObject tab, float width, float top)
        {
            if (view.Rows == null) return;

            GameObject stack = EnsureChild(tab, "StatGroups");
            TopLeft(Rect(stack), 0f, top, width, Rect(tab).sizeDelta.y - top);
            var column = EnsureComponent<VerticalLayoutGroup>(stack);
            column.spacing = Sc(4f);
            column.childAlignment = TextAnchor.UpperLeft;
            column.childControlWidth = true;
            column.childControlHeight = true;
            column.childForceExpandWidth = true;
            column.childForceExpandHeight = false;

            StyleGroupBlock(view, stack, "ATTACK", GroupAttack, 0, 0, 4, width);
            StyleGroupBlock(view, stack, "GUARD", GroupGuard, 1, 4, 2, width);
            StyleGroupBlock(view, stack, "BODY", GroupBody, 2, 6, 3, width);
        }

        // Each group is a rail in the group colour with its header and rows to the right of it.
        static void StyleGroupBlock(UnitStatsView view, GameObject stack, string name, Color tint,
                                          int order, int firstRow, int rowCount, float width)
        {
            GameObject block = EnsureChild(stack, "Group_" + name);
            block.transform.SetSiblingIndex(order);
            float height = Sc(8f) + rowCount * RowHeight;
            EnsureLayoutSize(block, width, height);

            var rail = EnsureChildImage(block, "Rail", 0);
            var railRect = Rect(rail.gameObject);
            railRect.anchorMin = new Vector2(0f, 0f);
            railRect.anchorMax = new Vector2(0f, 1f);
            railRect.pivot = new Vector2(0f, 0.5f);
            railRect.offsetMin = Vector2.zero;
            railRect.offsetMax = new Vector2(Sc(3f), 0f);
            rail.color = tint;
            rail.sprite = null;

            StyleGroupHeader(view.Root, block, name, tint, width);
            for (int i = 0; i < rowCount; i++)
                StyleStatRow(view, block, firstRow + i, i, tint, width);
        }

        static void StyleGroupHeader(GameObject tab, GameObject block, string name, Color tint, float width)
        {
            var header = FindLabel(tab, "Group_" + name) ?? EnsureChildLabel(block, "Header");
            Reparent(header.gameObject, block, 1);
            TopLeft(Rect(header.gameObject), Sc(3f) + Sc(5f), Sc(1f), width, Sc(7f));
            ApplyFont(header, "Bold", Sc(6f), tint);
            header.text = name;
            header.characterSpacing = 14f;          // .14em
            header.alignment = TextAlignmentOptions.MidlineLeft;
            NoWrap(header);
        }

        static void StyleStatRow(UnitStatsView view, GameObject block, int rowIndex,
                                 int positionInGroup, Color tint, float width)
        {
            if (rowIndex >= view.Rows.Length) return;

            var widgets = view.Rows[rowIndex];
            GameObject row = RowRootOf(widgets, block, rowIndex);
            Reparent(row, block, 2 + positionInGroup);
            TopLeft(Rect(row), Sc(3f) + Sc(5f), Sc(8f) + positionInGroup * RowHeight,
                    width - Sc(8f), RowHeight);

            ClearGraphic(row.GetComponent<Image>());
            HideHighlight(widgets);
            StyleRowContents(view, widgets, row, rowIndex, tint, width - Sc(8f));

            view.Rows[rowIndex].RowRoot = Rect(row);
        }

        static GameObject RowRootOf(UnitStatsView.StatRowWidgets widgets, GameObject block, int index)
        {
            GameObject row = widgets.Label != null && widgets.Label.transform.parent != null
                ? widgets.Label.transform.parent.gameObject
                : EnsureChild(block, "StatRow" + index);
            row.name = "StatRow" + index;
            return row;
        }

        // The per-row highlight art is replaced by the single sliding marker, so the old objects
        // stay wired and inert rather than being deleted out from under the View.
        static void HideHighlight(UnitStatsView.StatRowWidgets widgets)
        {
            if (widgets.Highlight == null) return;
            ClearGraphic(widgets.Highlight.GetComponent<Image>());
            widgets.Highlight.SetActive(false);
        }

        static readonly string[] StatGlyphs =
        {
            "glyph_triangle_up", "glyph_diamond", "glyph_plus", "glyph_triangle_right",
            "glyph_square", "glyph_pentagon", "glyph_triangle_down", "glyph_ring",
            "glyph_double_chevron",
        };

        static void StyleRowContents(UnitStatsView view, UnitStatsView.StatRowWidgets widgets,
                                     GameObject row, int rowIndex, Color tint, float width)
        {
            float x = Sc(6f);
            if (widgets.Icon != null)
            {
                Reparent(widgets.Icon.gameObject, row, 0);
                MiddleLeftBox(Rect(widgets.Icon.gameObject), x, Sc(8f), Sc(8f));
                widgets.Icon.color = tint;
                widgets.Icon.sprite = Glyph(StatGlyphs[Mathf.Min(rowIndex, StatGlyphs.Length - 1)]);
            }
            x += Sc(8f) + Gap;

            if (widgets.Label != null)
            {
                Reparent(widgets.Label.gameObject, row, 1);
                MiddleLeftBox(Rect(widgets.Label.gameObject), x, Sc(48f), Sc(9f));
                ApplyFont(widgets.Label, "Regular", Sc(6.5f), TextDim);
                widgets.Label.characterSpacing = 6f;        // .06em
                widgets.Label.alignment = TextAlignmentOptions.MidlineLeft;
                NoWrap(widgets.Label);
            }
            x += Sc(48f) + Gap;

            if (widgets.Value != null)
            {
                Reparent(widgets.Value.gameObject, row, 2);
                MiddleLeftBox(Rect(widgets.Value.gameObject), x, Sc(14f), Sc(9f));
                ApplyFont(widgets.Value, "Bold", Sc(8f), TextBright);
                widgets.Value.alignment = TextAlignmentOptions.MidlineRight;
                NoWrap(widgets.Value);
            }
            x += Sc(14f) + Gap;

            float capWidth = Sc(14f);
            float barWidth = width - x - Gap - capWidth - Sc(6f);
            StyleRowBar(view, widgets, rowIndex, row, x, barWidth, tint);
            x += barWidth + Gap;

            if (widgets.Cap != null)
            {
                Reparent(widgets.Cap.gameObject, row, 4);
                MiddleLeftBox(Rect(widgets.Cap.gameObject), x, capWidth, Sc(9f));
                ApplyFont(widgets.Cap, "Regular", Sc(7f), TextGhost);
                widgets.Cap.alignment = TextAlignmentOptions.MidlineRight;
                NoWrap(widgets.Cap);
            }
        }

        static void StyleRowBar(UnitStatsView view, UnitStatsView.StatRowWidgets widgets, int rowIndex,
                                GameObject row, float x, float barWidth, Color tint)
        {
            if (widgets.Fill == null) return;

            GameObject track = widgets.Fill.transform.parent.gameObject;
            Reparent(track, row, 3);
            MiddleLeftBox(Rect(track), x, barWidth, BarHeight);
            StyleBarTrack(track);

            Stretch(Rect(widgets.Fill.gameObject));
            var fill = EnsurePlainImage(widgets.Fill.gameObject);
            fill.color = tint;
            AsHorizontalFill(fill);

            view.Rows[rowIndex].Fill = fill;
        }

        // ---- §9 gear tab ----------------------------------------------------------------------

        static void StyleGearTab(UnitGearView view, GameObject body)
        {
            if (view == null) return;

            GameObject tab = view.gameObject;
            Touch(tab);
            tab.transform.SetParent(body.transform, false);
            float width = Rect(body).sizeDelta.x - ColumnWidth - Gap;
            float height = Rect(body).sizeDelta.y;
            TopLeft(Rect(tab), ColumnWidth + Gap, 0f, width, height);
            EnsureComponent<CanvasGroup>(tab);
            view.Root = tab;

            var column = EnsureComponent<VerticalLayoutGroup>(tab);
            column.padding = new RectOffset(0, 0, (int)Sc(3f), 0);
            column.spacing = Sc(3f);
            column.childAlignment = TextAnchor.UpperLeft;
            column.childControlWidth = true;
            column.childControlHeight = true;
            column.childForceExpandWidth = true;
            column.childForceExpandHeight = false;

            view.CompactHeight = Sc(28f);
            view.WeaponSelectedHeight = Sc(46f);
            view.ConsumableSelectedHeight = Sc(40f);
            view.CompactBackground = SlotCompact;
            view.SelectedBackground = Hex("1a202b");

            if (view.Slots == null) return;
            for (int i = 0; i < view.Slots.Length; i++)
                StyleGearSlot(view, i, width);
            tab.SetActive(false);
        }

        static void StyleGearSlot(UnitGearView view, int index, float width)
        {
            var widgets = view.Slots[index];
            GameObject slot = SlotRootOf(widgets, view.Root, index);
            Reparent(slot, view.Root, index);

            var background = EnsureImage(slot);
            background.color = SlotCompact;
            background.sprite = null;
            SetOutline(background, Hairline, Edge);

            var sizer = EnsureComponent<LayoutElement>(slot);
            sizer.preferredHeight = Sc(28f);
            sizer.preferredWidth = width;

            widgets.SlotRoot = slot;
            widgets.Sizer = sizer;
            widgets.SlotBackground = background;

            StyleSelectionRing(ref widgets, slot);
            StyleCompactRow(ref widgets, slot, width);
            StyleExpansion(ref widgets, slot, width);
            HideSlotLeftovers(widgets);

            view.Slots[index] = widgets;
        }

        static GameObject SlotRootOf(UnitGearView.GearSlotWidgets widgets, GameObject tab, int index)
        {
            GameObject slot = widgets.Name != null
                ? ContainerUnder(widgets.Name.transform, tab.transform)
                : null;
            slot = slot != null ? slot : EnsureChild(tab, "GearSlot" + index);
            slot.name = "GearSlot" + index;
            return slot;
        }

        static void StyleSelectionRing(ref UnitGearView.GearSlotWidgets widgets, GameObject slot)
        {
            GameObject ring = widgets.Highlight != null ? widgets.Highlight : EnsureChild(slot, "Highlight");
            Reparent(ring, slot, 0);
            Stretch(Rect(ring));

            var stroke = EnsureMpImage(ring);
            stroke.color = AllyAccent;
            stroke.sprite = null;
            SetCornerRadius(stroke, 0f);
            SetStroke(stroke, Hairline);
            SetOutline(stroke, 0f, Color.clear);
            ring.SetActive(false);
            widgets.Highlight = ring;
        }

        static void StyleCompactRow(ref UnitGearView.GearSlotWidgets widgets, GameObject slot, float width)
        {
            GameObject row = EnsureChild(slot, "CompactRow");
            TopLeft(Rect(row), Sc(6f), Sc(1f), width - Sc(12f), Sc(26f));
            ConfigureRow(row, Gap, TextAnchor.MiddleLeft);

            StyleSlotIcon(ref widgets, row);
            StyleSlotName(ref widgets, row);
            StyleEquippedBadge(ref widgets, row);
            StyleUses(ref widgets, row);
            StyleEmptyState(ref widgets, slot, row);
        }

        static void StyleSlotIcon(ref UnitGearView.GearSlotWidgets widgets, GameObject row)
        {
            GameObject box = EnsureChild(row, "IconBox");
            box.transform.SetSiblingIndex(0);
            EnsureLayoutSize(box, Sc(14f), Sc(14f));

            var frame = HairlineFrame(box, Edge);

            widgets.IconBox = box;
            if (widgets.Icon == null) return;
            Reparent(widgets.Icon.gameObject, box, 0);
            CenterBox(Rect(widgets.Icon.gameObject), Sc(8f), Sc(8f));
            widgets.Icon.color = Gold;
            widgets.Icon.sprite = Glyph("glyph_blade");
        }

        static void StyleSlotName(ref UnitGearView.GearSlotWidgets widgets, GameObject row)
        {
            if (widgets.Name == null) return;

            Reparent(widgets.Name.gameObject, row, 1);
            ApplyFont(widgets.Name, "Regular", Sc(7.5f), SlotName);
            widgets.Name.alignment = TextAlignmentOptions.MidlineLeft;
            NoWrap(widgets.Name);
            widgets.Name.overflowMode = TextOverflowModes.Ellipsis;

            var flexible = EnsureComponent<LayoutElement>(widgets.Name.gameObject);
            flexible.flexibleWidth = 1f;
            flexible.preferredHeight = Sc(12f);
        }

        static void StyleEquippedBadge(ref UnitGearView.GearSlotWidgets widgets, GameObject row)
        {
            GameObject badge = widgets.EquippedMark != null
                ? widgets.EquippedMark
                : EnsureChild(row, "EquippedMark");
            Reparent(badge, row, 2);
            EnsureLayoutSize(badge, Sc(9f), Sc(9f));

            var fill = EnsureImage(badge);
            fill.color = Gold;
            fill.sprite = null;
            SetOutline(fill, 0f, Color.clear);

            var letter = EnsureChildLabel(badge, "Label");
            Stretch(Rect(letter.gameObject));
            ApplyFont(letter, "Bold", Sc(7f), Bg);
            letter.text = "E";
            letter.alignment = TextAlignmentOptions.Center;
            NoWrap(letter);

            badge.SetActive(false);
            widgets.EquippedMark = badge;
        }

        static void StyleUses(ref UnitGearView.GearSlotWidgets widgets, GameObject row)
        {
            if (widgets.Uses != null)
            {
                Reparent(widgets.Uses.gameObject, row, 3);
                ApplyFont(widgets.Uses, "Regular", Sc(6f), TextDim);
                widgets.Uses.alignment = TextAlignmentOptions.MidlineRight;
                NoWrap(widgets.Uses);
                EnsureLayoutSize(widgets.Uses.gameObject, Sc(26f), Sc(9f));
            }

            GameObject track = EnsureChild(row, "UsesTrack");
            track.transform.SetSiblingIndex(4);
            EnsureLayoutSize(track, Sc(22f), Sc(3f));
            StyleBarTrack(track);

            var bar = EnsureChildImage(track, "Fill", 0);
            Stretch(Rect(bar.gameObject));
            var fill = EnsurePlainImage(bar.gameObject);
            fill.color = TextDim;
            AsHorizontalFill(fill);

            widgets.UsesTrack = track;
            widgets.UsesBar = fill;
        }

        static void StyleEmptyState(ref UnitGearView.GearSlotWidgets widgets, GameObject slot, GameObject row)
        {
            GameObject empty = widgets.EmptyState != null ? widgets.EmptyState : EnsureChild(slot, "EmptyState");
            Reparent(empty, slot, 1);
            Stretch(Rect(empty));

            var label = empty.GetComponent<TextMeshProUGUI>() ?? EnsureChildLabel(empty, "Label");
            ApplyFont(label, "Regular", Sc(6f), EmptyLabel);
            label.text = "EMPTY SLOT";
            label.characterSpacing = 14f;           // .14em
            label.alignment = TextAlignmentOptions.MidlineLeft;
            NoWrap(label);
            if (label.gameObject == empty)
            {
                Rect(empty).offsetMin = new Vector2(Sc(25f), 0f);
                Rect(empty).offsetMax = new Vector2(-Sc(6f), 0f);
            }

            widgets.DashedFrame = DashedFrame(slot);
            empty.SetActive(false);
            widgets.EmptyState = empty;
        }

        // §9 gives empty slots a dashed edge. Tiled strips keep the dashes an even length, which
        // a stretched 9-slice would not.
        static GameObject DashedFrame(GameObject slot)
        {
            GameObject frame = EnsureChild(slot, "DashedFrame");
            Stretch(Rect(frame));
            frame.SetActive(false);

            DashEdge(frame, "Top", 0, new Vector2(0f, 1f), new Vector2(1f, 1f), true);
            DashEdge(frame, "Bottom", 1, new Vector2(0f, 0f), new Vector2(1f, 0f), true);
            DashEdge(frame, "Left", 2, new Vector2(0f, 0f), new Vector2(0f, 1f), false);
            DashEdge(frame, "Right", 3, new Vector2(1f, 0f), new Vector2(1f, 1f), false);
            return frame;
        }

        static void DashEdge(GameObject frame, string name, int index, Vector2 min, Vector2 max, bool horizontal)
        {
            var edge = EnsureChildImage(frame, name, index);
            var rect = Rect(edge.gameObject);
            rect.anchorMin = min;
            rect.anchorMax = max;
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.sizeDelta = horizontal ? new Vector2(0f, Hairline) : new Vector2(Hairline, 0f);
            rect.anchoredPosition = Vector2.zero;

            edge.sprite = Glyph(horizontal ? "dash_horizontal" : "dash_vertical");
            edge.type = Image.Type.Tiled;
            edge.color = Edge;
        }

        static void StyleExpansion(ref UnitGearView.GearSlotWidgets widgets, GameObject slot, float width)
        {
            GameObject expansion = EnsureChild(slot, "Expansion");
            TopLeft(Rect(expansion), Sc(25f), Sc(28f), width - Sc(31f), Sc(12f));
            ConfigureRow(expansion, Sc(4f), TextAnchor.MiddleLeft);
            widgets.ExpansionFader = EnsureComponent<CanvasGroup>(expansion);

            StyleGradeBadge(ref widgets, expansion);
            widgets.Rng = ExpansionChip(expansion, widgets.Rng, "RNG", 1);
            widgets.Weight = ExpansionChip(expansion, widgets.Weight, "WT", 2);
            widgets.Mt = ExpansionChip(expansion, widgets.Mt, "MT", 3);
            widgets.Hit = ExpansionChip(expansion, widgets.Hit, "HIT", 4);
            widgets.Crt = ExpansionChip(expansion, widgets.Crt, "CRT", 5);
            StyleEffectChip(ref widgets, expansion);

            // The old always-on chip row becomes the expansion itself.
            widgets.Chips = expansion;
            widgets.Expansion = expansion;
            expansion.SetActive(false);
        }

        static void StyleGradeBadge(ref UnitGearView.GearSlotWidgets widgets, GameObject expansion)
        {
            GameObject chip = EnsureChild(expansion, "GradeBadge");
            chip.transform.SetSiblingIndex(0);
            EnsureLayoutSize(chip, Sc(16f), Sc(10f));

            var fill = EnsureImage(chip);
            fill.color = Gold;
            fill.sprite = null;

            var border = EnsureChildImage(chip, "Border", 0);
            Stretch(Rect(border.gameObject));
            var stroke = EnsureMpImage(border.gameObject);
            stroke.sprite = null;
            SetCornerRadius(stroke, 0f);
            SetStroke(stroke, Hairline);

            var letter = EnsureChildLabel(chip, "Label");
            Stretch(Rect(letter.gameObject));
            ApplyFont(letter, "Bold", Sc(7f), Bg);
            letter.text = "Prf";
            letter.alignment = TextAlignmentOptions.Center;
            NoWrap(letter);

            widgets.GradeChip = chip;
            widgets.GradeBadge = letter;
            widgets.GradeBadgeFill = fill;
            widgets.GradeBadgeBorder = stroke;
        }

        static TextMeshProUGUI ExpansionChip(GameObject expansion, TextMeshProUGUI existing,
                                             string key, int index)
        {
            GameObject chip = existing != null
                ? AncestorNamed(existing.transform, "Chip_") ?? EnsureChild(expansion, "Chip_" + key)
                : EnsureChild(expansion, "Chip_" + key);
            Reparent(chip, expansion, index);

            HairlineFrame(chip, Edge);
            ChipBox(chip);

            var keyLabel = FindLabel(chip, "K") ?? EnsureChildLabel(chip, "K");
            Reparent(keyLabel.gameObject, chip, 0);
            ApplyFont(keyLabel, "Regular", Sc(6f), TextDim);
            keyLabel.text = key;
            NoWrap(keyLabel);

            var value = existing ?? EnsureChildLabel(chip, "V");
            Reparent(value.gameObject, chip, 1);
            ApplyFont(value, "Bold", Sc(7f), TextBright);
            NoWrap(value);
            return value;
        }

        static void StyleEffectChip(ref UnitGearView.GearSlotWidgets widgets, GameObject expansion)
        {
            GameObject chip = EnsureChild(expansion, "Chip_EFFECT");
            chip.transform.SetSiblingIndex(6);

            var frame = HairlineFrame(chip, Edge);
            EnsureLayoutSize(chip, Sc(46f), Sc(10f));

            var value = EnsureChildLabel(chip, "V");
            Stretch(Rect(value.gameObject));
            ApplyFont(value, "Bold", Sc(7f), TextBright);
            value.alignment = TextAlignmentOptions.Center;
            NoWrap(value);

            widgets.EffectChip = chip;
            widgets.Effect = value;
            chip.SetActive(false);
        }

        // The slot number and type badge are not part of §9's compact row, so they stay wired
        // and switched off rather than being removed.
        static void HideSlotLeftovers(UnitGearView.GearSlotWidgets widgets)
        {
            if (widgets.IndexLabel != null) widgets.IndexLabel.gameObject.SetActive(false);
            if (widgets.TypeBadge != null) widgets.TypeBadge.gameObject.SetActive(false);
        }

        // ---- §7 footer ------------------------------------------------------------------------

        static void StyleFooter(UnitInfoFooterView view, GameObject panel)
        {
            if (view == null) return;

            GameObject band = view.gameObject;
            Touch(band);
            band.transform.SetParent(panel.transform, false);
            var rect = Rect(band);
            rect.anchorMin = new Vector2(0f, 0f);
            rect.anchorMax = new Vector2(1f, 0f);
            rect.pivot = new Vector2(0.5f, 0f);
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = new Vector2(0f, FooterHeight);

            var fill = EnsureImage(band);
            fill.color = BgRaised;
            fill.sprite = null;
            TopHairline(band, Edge);

            StyleFooterLines(view, band);
            StyleFooterHints(band);
        }

        static void StyleFooterLines(UnitInfoFooterView view, GameObject band)
        {
            GameObject column = EnsureChild(band, "Explainer");
            TopLeft(Rect(column), BandPadX, Sc(4f), Sc(300f), Sc(23f));

            GameObject first = EnsureChild(column, "Line1");
            TopLeft(Rect(first), 0f, 0f, Sc(300f), Sc(11f));
            ConfigureRow(first, Sc(3f), TextAnchor.MiddleLeft);

            if (view.Icon != null)
            {
                Reparent(view.Icon.gameObject, first, 0);
                EnsureLayoutSize(view.Icon.gameObject, Sc(7f), Sc(7f));
                view.Icon.color = TextDim;
            }
            if (view.Title != null)
            {
                Reparent(view.Title.gameObject, first, 1);
                ApplyFont(view.Title, "Bold", Sc(7f), TextBright);
                NoWrap(view.Title);
                EnsureLayoutSize(view.Title.gameObject, Sc(42f), Sc(11f));
            }
            if (view.Description != null)
            {
                Reparent(view.Description.gameObject, first, 2);
                ApplyFont(view.Description, "Regular", Sc(7f), TextBody);
                NoWrap(view.Description);
                EnsureLayoutSize(view.Description.gameObject, Sc(200f), Sc(11f));
            }

            var detail = EnsureChildLabel(column, "Line2");
            TopLeft(Rect(detail.gameObject), 0f, Sc(11f), Sc(300f), Sc(11f));
            ApplyFont(detail, "Regular", Sc(7f), TextFaint);
            detail.alignment = TextAlignmentOptions.TopLeft;
            NoWrap(detail);
            view.Detail = detail;
        }

        static void StyleFooterHints(GameObject band)
        {
            var hints = FindLabel(band, "Hints") ?? EnsureChildLabel(band, "Hints");
            var rect = Rect(hints.gameObject);
            rect.anchorMin = new Vector2(1f, 0.5f);
            rect.anchorMax = new Vector2(1f, 0.5f);
            rect.pivot = new Vector2(1f, 0.5f);
            rect.anchoredPosition = new Vector2(-BandPadX, 0f);
            rect.sizeDelta = new Vector2(Sc(140f), Sc(9f));

            ApplyFont(hints, "Regular", Sc(6f), TextMicro);
            hints.text = "↑↓ MOVE · Q/E TAB · X CLOSE";
            hints.alignment = TextAlignmentOptions.MidlineRight;
            NoWrap(hints);
        }

        // ---- §8 focus marker ------------------------------------------------------------------

        static GameObject BuildFocusMarker(GameObject body)
        {
            GameObject marker = EnsureChild(body, "FocusMarker");
            marker.transform.SetAsLastSibling();

            // Top-left anchors and pivot, because UnitInfoFocusMarker positions it by the target's
            // top-left corner expressed in this container's space.
            var rect = Rect(marker);
            rect.anchorMin = rect.anchorMax = rect.pivot = new Vector2(0f, 1f);
            rect.sizeDelta = new Vector2(Sc(100f), RowHeight);
            EnsureComponent<UnitInfoFocusMarker>(marker);

            var glow = EnsureChildImage(marker, "Glow", 0);
            var glowRect = Rect(glow.gameObject);
            Stretch(glowRect);
            glowRect.offsetMin = new Vector2(-Sc(6f), -Sc(6f));
            glowRect.offsetMax = new Vector2(Sc(6f), Sc(6f));
            glow.sprite = Glyph("focus_glow");
            glow.type = Image.Type.Sliced;
            glow.color = AllyAccent;

            var ring = EnsureChildImage(marker, "Ring", 1);
            Stretch(Rect(ring.gameObject));
            var stroke = EnsureMpImage(ring.gameObject);
            stroke.color = AllyAccent;
            stroke.sprite = null;
            SetCornerRadius(stroke, 0f);
            SetStroke(stroke, Hairline);

            marker.SetActive(false);
            return marker;
        }

        // ---- wiring ---------------------------------------------------------------------------

        static void WireController(UnitInfoUIController controller, GameObject panel, GameObject marker,
                                   UnitSummaryView summary, UnitStatsView stats)
        {
            if (controller == null) return;

            var so = new SerializedObject(controller);
            so.FindProperty("panelRoot").objectReferenceValue = panel;
            so.FindProperty("transition").objectReferenceValue =
                panel.GetComponent<UnitInfoScreenTransition>();
            so.FindProperty("focusMarker").objectReferenceValue =
                marker.GetComponent<UnitInfoFocusMarker>();
            so.FindProperty("focusMarkerRing").objectReferenceValue =
                FindChild(marker, "Ring").GetComponent<Image>();
            so.FindProperty("focusMarkerGlow").objectReferenceValue =
                FindChild(marker, "Glow").GetComponent<Image>();

            WireAccentTargets(so, summary, stats);
            so.ApplyModifiedPropertiesWithoutUndo();

            WirePortraitTreatment(summary);
        }

        // §2 swaps these to the danger red on enemy sheets; the alpha is each one's own strength.
        static void WireAccentTargets(SerializedObject so, UnitSummaryView summary, UnitStatsView stats)
        {
            var targets = new List<(Image image, float alpha)>();
            if (summary != null)
            {
                if (summary.HpFill != null) targets.Add((summary.HpFill, 1f));
                if (summary.HpBlockBackground != null) targets.Add((summary.HpBlockBackground, 0.15f));
                if (summary.ExpFill != null) targets.Add((summary.ExpFill, 0.7f));
            }

            var array = so.FindProperty("accentTargets");
            array.arraySize = targets.Count;
            for (int i = 0; i < targets.Count; i++)
            {
                var element = array.GetArrayElementAtIndex(i);
                element.FindPropertyRelative("Target").objectReferenceValue = targets[i].image;
                element.FindPropertyRelative("Alpha").floatValue = targets[i].alpha;
            }
        }

        static void WirePortraitTreatment(UnitSummaryView summary)
        {
            if (summary == null) return;
            summary.HeaderName = FindHeaderName(summary);
            summary.ActedPortraitMaterial = Load<Material>(ActedMat);
            summary.PlaceholderBust = Load<Sprite>(BustPath);
            EditorUtility.SetDirty(summary);
        }

        // The header label sits on the tab bar, but the name arrives through the summary model,
        // so the summary view is what holds the reference.
        static TextMeshProUGUI FindHeaderName(UnitSummaryView summary) =>
            FindLabel(summary.transform.root.gameObject, "HeaderUnitName");

        // ---- shared chrome --------------------------------------------------------------------

        static void StyleBarTrack(GameObject track)
        {
            var image = EnsureMpImage(track);
            image.color = BarTrack;
            image.sprite = null;
            image.type = Image.Type.Simple;
            SetCornerRadius(image, 0f);
            SetOutline(image, Hairline, BarTrackBorder);
        }

        // The sprite is not decoration: uGUI ignores fillAmount on a Filled image that has none,
        // and draws the whole quad instead - so every bar would sit at 100%.
        static void AsHorizontalFill(Image fill)
        {
            fill.sprite = Glyph("bar_fill");
            fill.type = Image.Type.Filled;
            fill.fillMethod = Image.FillMethod.Horizontal;
            fill.fillOrigin = (int)Image.OriginHorizontal.Left;
            fill.preserveAspect = false;
        }

        static void BottomHairline(GameObject band)
        {
            var line = EnsureChildImage(band, "EdgeBottom", band.transform.childCount);
            line.transform.SetAsLastSibling();
            var rect = Rect(line.gameObject);
            rect.anchorMin = new Vector2(0f, 0f);
            rect.anchorMax = new Vector2(1f, 0f);
            rect.pivot = new Vector2(0.5f, 0f);
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = new Vector2(0f, Hairline);
            rect.anchoredPosition = Vector2.zero;
            line.color = Edge;
            line.sprite = null;
        }

        static void TopHairline(GameObject band, Color color)
        {
            var line = EnsureChildImage(band, "EdgeTop", band.transform.childCount);
            line.transform.SetAsLastSibling();
            var rect = Rect(line.gameObject);
            rect.anchorMin = new Vector2(0f, 1f);
            rect.anchorMax = new Vector2(1f, 1f);
            rect.pivot = new Vector2(0.5f, 1f);
            rect.offsetMin = new Vector2(0f, -Hairline);
            rect.offsetMax = Vector2.zero;
            rect.anchoredPosition = Vector2.zero;
            line.color = color;
            line.sprite = null;
        }

        // ---- helpers --------------------------------------------------------------------------

        // Walks up from a wired widget to the ancestor sitting directly under `parent`. Resolving a
        // container this way is what makes a second run adopt what the first run built, instead of
        // mistaking an inner row for the container and standing a duplicate up beside it.
        static GameObject ContainerUnder(Transform start, Transform parent)
        {
            for (Transform t = start; t != null; t = t.parent)
                if (t.parent == parent) return t.gameObject;
            return null;
        }

        static GameObject AncestorNamed(Transform start, string prefix)
        {
            for (Transform t = start; t != null; t = t.parent)
                if (t.name.StartsWith(prefix)) return t.gameObject;
            return null;
        }

        static void Touch(GameObject go)
        {
            if (go != null) touched.Add(go);
        }

        static RectTransform Rect(GameObject go) => go.GetComponent<RectTransform>();

        static void TopLeft(RectTransform rect, float x, float y, float w, float h)
        {
            rect.anchorMin = rect.anchorMax = rect.pivot = new Vector2(0f, 1f);
            rect.anchoredPosition = new Vector2(x, -y);
            rect.sizeDelta = new Vector2(w, h);
        }

        static void MiddleLeftBox(RectTransform rect, float x, float w, float h)
        {
            rect.anchorMin = rect.anchorMax = rect.pivot = new Vector2(0f, 0.5f);
            rect.anchoredPosition = new Vector2(x, 0f);
            rect.sizeDelta = new Vector2(w, h);
        }

        static void CenterBox(RectTransform rect, float w, float h)
        {
            rect.anchorMin = rect.anchorMax = rect.pivot = new Vector2(0.5f, 0.5f);
            rect.anchoredPosition = Vector2.zero;
            rect.sizeDelta = new Vector2(w, h);
        }

        static void Stretch(RectTransform rect)
        {
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;
        }

        static void ConfigureRow(GameObject go, float spacing, TextAnchor alignment)
        {
            var row = EnsureComponent<HorizontalLayoutGroup>(go);
            row.spacing = spacing;
            row.childAlignment = alignment;
            row.childControlWidth = true;
            row.childControlHeight = true;
            row.childForceExpandWidth = false;
            row.childForceExpandHeight = false;
        }

        // §5's chip language: a key and a value side by side, the frame hugging whatever they need.
        static void ChipBox(GameObject chip)
        {
            var row = EnsureComponent<HorizontalLayoutGroup>(chip);
            row.spacing = Sc(2f);
            row.padding = new RectOffset((int)Sc(4f), (int)Sc(4f), 0, 0);
            row.childAlignment = TextAnchor.MiddleLeft;
            row.childControlWidth = true;
            row.childControlHeight = true;
            row.childForceExpandWidth = false;
            row.childForceExpandHeight = false;

            var fitter = EnsureComponent<ContentSizeFitter>(chip);
            fitter.horizontalFit = ContentSizeFitter.FitMode.PreferredSize;
            fitter.verticalFit = ContentSizeFitter.FitMode.Unconstrained;

            var element = EnsureComponent<LayoutElement>(chip);
            element.preferredWidth = -1f;
            element.preferredHeight = Sc(10f);
        }

        static void EnsureLayoutSize(GameObject go, float width, float height)
        {
            var element = EnsureComponent<LayoutElement>(go);
            element.preferredWidth = width;
            element.preferredHeight = height;
        }

        static void ApplyFont(TextMeshProUGUI label, string weight, float size, Color color)
        {
            Touch(label.gameObject);
            var font = Load<TMP_FontAsset>($"{FontDir}JetBrainsMono-{weight} SDF.asset");
            if (font != null) label.font = font;

            label.fontSize = size;
            label.color = color;
            label.characterSpacing = 0f;
            label.lineSpacing = 0f;
            label.richText = false;
            label.raycastTarget = false;
        }

        static void NoWrap(TextMeshProUGUI label)
        {
            label.textWrappingMode = TextWrappingModes.NoWrap;
            label.overflowMode = TextOverflowModes.Overflow;
        }

        static void BaselineRun(TextMeshProUGUI label)
        {
            label.alignment = TextAlignmentOptions.BottomLeft;
            NoWrap(label);
        }

        static void ClearGraphic(Image image)
        {
            if (image == null) return;
            image.color = Color.clear;
            image.sprite = null;
            SetOutline(image, 0f, Color.clear);
        }

        static void Reparent(GameObject go, GameObject parent, int siblingIndex)
        {
            Touch(go);
            go.transform.SetParent(parent.transform, false);
            go.transform.SetSiblingIndex(siblingIndex);
        }

        static GameObject FindChild(GameObject parent, string name)
        {
            var found = parent.transform.Find(name);
            return found != null ? found.gameObject : null;
        }

        static TextMeshProUGUI FindLabel(GameObject root, string name)
        {
            foreach (var label in root.GetComponentsInChildren<TextMeshProUGUI>(true))
                if (label.gameObject.name == name) return label;
            return null;
        }

        static T EnsureComponent<T>(GameObject go) where T : Component
        {
            var existing = go.GetComponent<T>();
            return existing != null ? existing : go.AddComponent<T>();
        }

        static GameObject EnsureChild(GameObject parent, string name)
        {
            var existing = parent.transform.Find(name);
            if (existing != null)
            {
                Touch(existing.gameObject);
                return existing.gameObject;
            }

            var go = new GameObject(name, typeof(RectTransform));
            go.transform.SetParent(parent.transform, false);
            Touch(go);
            return go;
        }

        static TextMeshProUGUI EnsureChildLabel(GameObject parent, string name)
        {
            GameObject go = EnsureChild(parent, name);
            Touch(go);
            return EnsureComponent<TextMeshProUGUI>(go);
        }

        static Image EnsureChildImage(GameObject parent, string name, int siblingIndex)
        {
            GameObject go = EnsureChild(parent, name);
            go.transform.SetSiblingIndex(siblingIndex);
            var image = EnsureComponent<Image>(go);
            image.raycastTarget = false;
            return image;
        }

        static Image EnsureImage(GameObject go)
        {
            Touch(go);
            var image = EnsureComponent<Image>(go);
            image.raycastTarget = false;
            return image;
        }

        // Swaps a plain Image for MPUIKit's procedural one, keeping colour and sprite, so strokes
        // and radii are available. Any View field pointing at the old component is re-pointed by
        // the caller.
        static Image EnsureMpImage(GameObject go)
        {
            Touch(go);
            var existing = go.GetComponent<MPImage>();
            if (existing != null) return existing;

            var plain = go.GetComponent<Image>();
            Color color = plain != null ? plain.color : Color.white;
            Sprite sprite = plain != null ? plain.sprite : null;
            if (plain != null) Object.DestroyImmediate(plain);

            var replacement = go.AddComponent<MPImage>();
            replacement.color = color;
            replacement.sprite = sprite;
            replacement.raycastTarget = false;
            return replacement;
        }

        // A hollow hairline box. Drawn as a stroke in the graphic's own colour rather than an
        // outline, because an outline is modulated by the graphic's alpha - so an outlined but
        // transparent frame renders nothing at all.
        static Image HairlineFrame(GameObject go, Color color)
        {
            var frame = EnsureMpImage(go);
            frame.sprite = null;
            frame.color = color;
            SetCornerRadius(frame, 0f);
            SetStroke(frame, Hairline);
            SetOutline(frame, 0f, Color.clear);
            return frame;
        }

        // Bar fills stay plain Images: MPUIKit builds its own geometry and ignores fillAmount,
        // which is what the Views use to size every bar.
        static Image EnsurePlainImage(GameObject go)
        {
            Touch(go);
            var procedural = go.GetComponent<MPImage>();
            if (procedural == null) return EnsureImage(go);

            Color color = procedural.color;
            Sprite sprite = procedural.sprite;
            Object.DestroyImmediate(procedural);

            var plain = go.AddComponent<Image>();
            plain.color = color;
            plain.sprite = sprite;
            plain.raycastTarget = false;
            return plain;
        }

        static void SetStroke(Image image, float width)
        {
            if (!(image is MPImage procedural)) return;

            procedural.StrokeWidth = width;
            procedural.SetAllDirty();
        }

        static void SetCornerRadius(Image image, float radius)
        {
            if (!(image is MPImage procedural)) return;

            procedural.DrawShape = DrawShape.Rectangle;
            Rectangle shape = procedural.Rectangle;
            shape.CornerRadius = new Vector4(radius, radius, radius, radius);
            procedural.Rectangle = shape;
            procedural.SetAllDirty();
        }

        // MPUIKit draws the hairline natively; reached through SerializedObject so a plain Image
        // simply ignores the call.
        static void SetOutline(Image image, float width, Color color)
        {
            if (image == null) return;

            var so = new SerializedObject(image);
            var widthProp = so.FindProperty("m_OutlineWidth");
            var colorProp = so.FindProperty("m_OutlineColor");
            if (widthProp == null || colorProp == null) return;

            widthProp.floatValue = width;
            colorProp.colorValue = color;
            so.ApplyModifiedPropertiesWithoutUndo();
        }

        static void RewireImage(Object view, string field, Image replacement)
        {
            var so = new SerializedObject(view);
            var property = so.FindProperty(field);
            if (property == null) return;

            property.objectReferenceValue = replacement;
            so.ApplyModifiedPropertiesWithoutUndo();
        }

        static Sprite Glyph(string name) => Load<Sprite>(SpriteDir + name + ".png");

        static T Load<T>(string path) where T : Object => AssetDatabase.LoadAssetAtPath<T>(path);

        static string Path(GameObject go)
        {
            string path = go.name;
            for (var t = go.transform.parent; t != null; t = t.parent) path = t.name + "/" + path;
            return path;
        }

        static Color Hex(string hex)
        {
            byte r = byte.Parse(hex.Substring(0, 2), System.Globalization.NumberStyles.HexNumber);
            byte g = byte.Parse(hex.Substring(2, 2), System.Globalization.NumberStyles.HexNumber);
            byte b = byte.Parse(hex.Substring(4, 2), System.Globalization.NumberStyles.HexNumber);
            return new Color(r / 255f, g / 255f, b / 255f, 1f);
        }
    }
}
