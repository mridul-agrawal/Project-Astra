using System.Collections.Generic;
using MPUIKIT;
using ProjectAstra.Core.UI.BattleMap.HUD;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace ProjectAstra.EditorTools
{
    // ==========================================================================================
    // Restyles the battle-map objectives panel to the Objectives Panel spec, in place.
    //
    // Like the tile panel, this panel is authored straight into BattleMap.unity, so the pass opens
    // that scene additively rather than switching to it - whatever scene the editor is showing stays
    // loaded and untouched.
    //
    // Nothing referenced is destroyed. The win/lose value labels move into the new sections, the old
    // headers are adopted as the WIN/LOSE section headers, and the parchment title, divider, corner
    // bosses, turn and enemy rows stay wired and switch off.
    //
    // PLACEMENT IS NOT TOUCHED. Which corner the panel docks to, and the 56px inset, belong to
    // BattleHUDUIController + HudCornerLayout.
    //
    // Spec values are logical px on a 480x270 viewport. The canvas is 1920x1080, an exact 4x.
    //
    // Run via 'Project Astra/Restyle Objectives Panel'.
    // ==========================================================================================
    public static class RestyleObjectivesPanel
    {
        const float Scale = 4f;
        static float Sc(float logical) => logical * Scale;

        const string ScenePath = "Assets/Scenes/BattleMap.unity";
        const string FontDir   = "Assets/UI/BattleMapHUD/Fonts";
        const string SpriteDir = "Assets/UI/BattleMapHUD/Generated";

        // §A3 / §B5.
        static readonly Color White        = Color.white;
        static readonly Color RingWhite    = new Color32(0xFF, 0xFF, 0xFF, 0xB3);
        static readonly Color Cyan         = Hex("4FD6F7");
        static readonly Color Red          = Hex("FF5A56");
        static readonly Color ObjHeader    = new Color32(0xFF, 0xFF, 0xFF, 0x8C);
        static readonly Color KeycapBorder = new Color32(0xFF, 0xFF, 0xFF, 0x66);
        static readonly Color KeycapBack   = new Color32(0x09, 0x0D, 0x1A, 0xE6);
        static readonly Color CheckDark    = Hex("0A0E1A");
        static readonly Color BoxBorder    = new Color32(0xFF, 0xFF, 0xFF, 0x80);
        static readonly Color DimText      = Hex("8B93A0");

        // §B6 wraps long objective text; five rows is the practical budget for a 170px plate.
        const int RowCount = 5;

        static readonly List<(Object owner, string path)> wiredFields = new();
        static readonly HashSet<GameObject> touched = new();

        [MenuItem("Project Astra/Restyle Objectives Panel")]
        public static void Restyle()
        {
            Scene scene = FindOrOpenScene(out bool openedHere);
            if (!scene.IsValid())
            {
                Debug.LogError("[RestyleObjectivesPanel] Could not open " + ScenePath);
                return;
            }

            ObjectiveView view = FindView(scene);
            if (view == null)
            {
                Debug.LogError("[RestyleObjectivesPanel] No ObjectiveView in " + ScenePath);
                if (openedHere) EditorSceneManager.CloseScene(scene, true);
                return;
            }

            touched.Clear();
            TrackReferences(view);

            StyleTab(view);
            StyleBanner(view);
            HideRowsTheSpecDrops(view);
            WireColours(view);

            PruneStaleDecoration(view.gameObject);
            VerifyReferencesSurvived();

            EditorUtility.SetDirty(view);
            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
            if (openedHere) EditorSceneManager.CloseScene(scene, true);

            Debug.Log("[RestyleObjectivesPanel] Objectives panel restyled to spec.");
        }

        // ---- §A collapsed tab -------------------------------------------------------------------

        static void StyleTab(ObjectiveView view)
        {
            GameObject tab = view.Nub;
            if (tab == null) return;
            Touch(tab);

            var rect = Rect(tab);
            rect.sizeDelta = new Vector2(Sc(24f), Sc(24f));        // §A2 square
            rect.anchoredPosition = Vector2.zero;

            var fill = EnsureImage(tab);
            fill.sprite = Load<Sprite>($"{SpriteDir}/plate_gradient.png");
            fill.type = Image.Type.Simple;
            fill.color = White;

            StyleBullseye(view, tab);
            StyleKeycap(view, tab);
        }

        // §A2 centres a 17px icon in the 24px tab. Rings and dot are two images so §A3 can tint them
        // white-70% and cyan independently.
        static void StyleBullseye(ObjectiveView view, GameObject tab)
        {
            var rings = EnsureChildImage(tab, "BullseyeRings", 0);
            CenterBox(Rect(rings.gameObject), Sc(17f), Sc(17f));
            rings.sprite = Load<Sprite>($"{SpriteDir}/objectives_bullseye_rings.png");
            rings.color = RingWhite;

            var dot = EnsureChildImage(tab, "BullseyeDot", 1);
            CenterBox(Rect(dot.gameObject), Sc(17f), Sc(17f));
            dot.sprite = Load<Sprite>($"{SpriteDir}/objectives_bullseye_dot.png");
            dot.color = Cyan;

            view.BullseyeRings = rings;
            view.BullseyeDot = dot;
        }

        // §A2: a tiny bordered chip, 1px inset from the tab's bottom and its outer side. The runtime
        // view mirrors the side when the corner changes; this only has to build it.
        static void StyleKeycap(ObjectiveView view, GameObject tab)
        {
            GameObject chip = EnsureChild(tab, "KeycapChip");
            chip.transform.SetAsLastSibling();

            var rect = Rect(chip);
            rect.anchorMin = rect.anchorMax = rect.pivot = new Vector2(1f, 0f);
            rect.sizeDelta = new Vector2(Sc(10f), Sc(9f));
            rect.anchoredPosition = new Vector2(-Sc(1f), Sc(1f));

            var backing = EnsureImage(chip);
            backing.sprite = null;
            backing.color = KeycapBack;

            var border = EnsureChildImage(chip, "Border", 0);
            Stretch(Rect(border.gameObject));
            var stroke = EnsureMpImage(border.gameObject);
            stroke.sprite = null;
            stroke.color = KeycapBorder;
            SetCornerRadius(stroke, 0f);
            SetStroke(stroke, Sc(1f));

            var label = EnsureChildLabel(chip, "Label");
            Stretch(Rect(label.gameObject));
            ApplyFont(label, "SemiBold", Sc(5.5f), White);         // §A2 5.5px / 600
            label.text = "O";
            label.alignment = TextAlignmentOptions.Center;
            NoWrap(label);

            view.KeycapChip = chip;
            view.KeycapLabel = label;

            // The old Cinzel peek glyph is what the keycap replaces.
            if (view.PeekButtonIcon != null) Hide(view.PeekButtonIcon);
        }

        // ---- §B expanded banner -----------------------------------------------------------------

        static void StyleBanner(ObjectiveView view)
        {
            GameObject banner = view.Expanded;
            if (banner == null) return;
            Touch(banner);

            var rect = Rect(banner);
            rect.anchoredPosition = new Vector2(-Sc(24f), 0f);      // §B2 inner edge past the tab

            var fill = EnsureImage(banner);
            fill.sprite = Load<Sprite>($"{SpriteDir}/plate_gradient.png");
            fill.type = Image.Type.Simple;
            fill.color = White;

            var column = EnsureComponent<VerticalLayoutGroup>(banner);
            column.padding = new RectOffset((int)Sc(8f), (int)Sc(8f), (int)Sc(6f), (int)Sc(6f));
            column.spacing = Sc(5f);                                // §B4 margin above each block
            column.childAlignment = TextAnchor.UpperLeft;
            column.childControlWidth = true;
            column.childControlHeight = true;
            column.childForceExpandWidth = false;
            column.childForceExpandHeight = false;

            var fitter = EnsureComponent<ContentSizeFitter>(banner);
            fitter.horizontalFit = ContentSizeFitter.FitMode.PreferredSize;
            fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

            var sizer = EnsureComponent<LayoutElement>(banner);
            sizer.preferredWidth = -1f;
            sizer.preferredHeight = -1f;

            StyleSection(view, banner);
            StyleObjectives(view, banner);
        }

        // WIN and LOSE: the old inline labels become the section headers, the old value labels the
        // entry lines beneath them. Each pair gets its own block, which is how §B4's asymmetric
        // margins - 5px above a header, 1px below it - come out right; a single spacing value on the
        // banner column could only ever express one of the two.
        static void StyleSection(ObjectiveView view, GameObject banner)
        {
            GameObject win = StyleBlock(banner, "WinBlock", 0);
            view.WinHeader = StyleHeader(view, win, "Label_Victory", "WinHeader", "WIN", Cyan);
            StyleEntry(view.WinText, win, 1);

            GameObject lose = StyleBlock(banner, "LoseBlock", 1);
            view.LoseHeader = StyleHeader(view, lose, "Label_Defeat", "LoseHeader", "LOSE", Red);
            StyleEntry(view.LoseText, lose, 1);
        }

        static GameObject StyleBlock(GameObject banner, string name, int order)
        {
            GameObject block = EnsureChild(banner, name);
            block.transform.SetSiblingIndex(order);

            var column = EnsureComponent<VerticalLayoutGroup>(block);
            column.padding = new RectOffset(0, 0, 0, 0);
            column.spacing = Sc(1f);                                // §B4 margin below a header
            column.childAlignment = TextAnchor.UpperLeft;
            column.childControlWidth = true;
            column.childControlHeight = true;
            column.childForceExpandWidth = false;
            column.childForceExpandHeight = false;

            var fitter = EnsureComponent<ContentSizeFitter>(block);
            fitter.horizontalFit = ContentSizeFitter.FitMode.PreferredSize;
            fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;
            return block;
        }

        static TextMeshProUGUI StyleHeader(ObjectiveView view, GameObject block, string adopt,
                                          string fallback, string text, Color colour)
        {
            var label = FindLabel(view.gameObject, adopt) ?? EnsureChildLabel(block, fallback);
            Reparent(label.gameObject, block, 0);

            ApplyFont(label, "SemiBold", Sc(6f), colour);           // §B4 6px / 600
            label.text = text;
            label.characterSpacing = 14f;                           // +0.14em
            label.alignment = TextAlignmentOptions.TopLeft;
            NoWrap(label);

            // Just the text's own box; the margins are the two layout groups' spacing.
            EnsureComponent<LayoutElement>(label.gameObject).preferredHeight = Sc(7f);
            return label;
        }

        static void StyleEntry(TextMeshProUGUI entry, GameObject banner, int order)
        {
            if (entry == null) return;

            Reparent(entry.gameObject, banner, order);
            ApplyFont(entry, "Medium", Sc(8f), White);              // §B4 8px / 500
            entry.lineSpacing = 0f;
            entry.textWrappingMode = TextWrappingModes.Normal;      // §B6 wraps in the plate
            entry.overflowMode = TextOverflowModes.Overflow;
            entry.alignment = TextAlignmentOptions.TopLeft;

            // Width is set per render so the plate can hug short lines and still cap at §B2's 170px.
            var sizer = EnsureComponent<LayoutElement>(entry.gameObject);
            sizer.preferredHeight = -1f;
        }

        // §B3's third block. Built whole; the view switches it on only when a map authors objectives.
        static void StyleObjectives(ObjectiveView view, GameObject banner)
        {
            GameObject section = EnsureChild(banner, "ObjectivesSection");
            section.transform.SetSiblingIndex(2);

            var column = EnsureComponent<VerticalLayoutGroup>(section);
            column.padding = new RectOffset(0, 0, 0, 0);
            column.spacing = Sc(4f);                                // §B4 between rows
            column.childAlignment = TextAnchor.UpperLeft;
            column.childControlWidth = true;
            column.childControlHeight = true;
            column.childForceExpandWidth = false;
            column.childForceExpandHeight = false;

            column.childForceExpandWidth = true;                     // every row spans the section

            var fitter = EnsureComponent<ContentSizeFitter>(section);
            fitter.horizontalFit = ContentSizeFitter.FitMode.PreferredSize;
            fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

            var header = EnsureChildLabel(section, "ObjectivesHeader");
            Reparent(header.gameObject, section, 0);
            ApplyFont(header, "SemiBold", Sc(6f), ObjHeader);
            header.text = "OBJECTIVES";
            header.characterSpacing = 14f;
            header.alignment = TextAlignmentOptions.TopLeft;
            NoWrap(header);
            EnsureComponent<LayoutElement>(header.gameObject).preferredHeight = Sc(13f);

            var rows = new ObjectiveView.ObjectiveRowWidgets[RowCount];
            for (int i = 0; i < RowCount; i++)
                rows[i] = StyleRow(section, i);

            view.ObjectivesSection = section;
            view.ObjectivesHeader = header;
            view.ObjectiveRows = rows;
            section.SetActive(false);
        }

        static ObjectiveView.ObjectiveRowWidgets StyleRow(GameObject section, int index)
        {
            GameObject row = EnsureChild(section, "Row" + index);
            row.transform.SetSiblingIndex(1 + index);

            var line = EnsureComponent<HorizontalLayoutGroup>(row);
            line.padding = new RectOffset(0, 0, 0, 0);
            line.spacing = Sc(5f);                                  // §B4 box / text / counter
            line.childAlignment = TextAnchor.UpperLeft;
            line.childControlWidth = true;
            line.childControlHeight = true;
            line.childForceExpandWidth = false;
            line.childForceExpandHeight = false;

            // No ContentSizeFitter here: the section sizes the row, and a fitter on a layout child
            // fights the parent that is already deciding its width.
            var stale = row.GetComponent<ContentSizeFitter>();
            if (stale != null) Object.DestroyImmediate(stale);

            var widgets = StyleCheckbox(row);

            var text = EnsureChildLabel(row, "Text");
            Reparent(text.gameObject, row, 1);
            ApplyFont(text, "Medium", Sc(8f), White);
            text.textWrappingMode = TextWrappingModes.Normal;
            text.alignment = TextAlignmentOptions.TopLeft;
            var textSizer = EnsureComponent<LayoutElement>(text.gameObject);
            textSizer.flexibleWidth = 1f;                           // takes the slack, pinning the counter right

            var counter = EnsureChildLabel(row, "Counter");
            Reparent(counter.gameObject, row, 2);
            ApplyFont(counter, "Bold", Sc(8f), Cyan);               // §B4 8px / 700
            counter.alignment = TextAlignmentOptions.TopRight;
            NoWrap(counter);
            EnsureComponent<LayoutElement>(counter.gameObject).preferredWidth = Sc(20f);

            widgets.Root = row;
            widgets.Text = text;
            widgets.Counter = counter;
            row.SetActive(false);
            return widgets;
        }

        // §B4's 8x8 box with a 1px optical offset, holding the fill, the hairline and the tick.
        static ObjectiveView.ObjectiveRowWidgets StyleCheckbox(GameObject row)
        {
            GameObject box = EnsureChild(row, "Checkbox");
            box.transform.SetSiblingIndex(0);
            var sizer = EnsureComponent<LayoutElement>(box);
            sizer.preferredWidth = Sc(8f);
            sizer.preferredHeight = Sc(9f);                         // 8 + the 1px optical offset

            var fill = EnsureImage(box);
            fill.sprite = null;
            fill.color = Cyan;

            var border = EnsureChildImage(box, "Border", 0);
            Stretch(Rect(border.gameObject));
            Rect(border.gameObject).offsetMax = new Vector2(0f, -Sc(1f));
            var stroke = EnsureMpImage(border.gameObject);
            stroke.sprite = null;
            stroke.color = BoxBorder;
            SetCornerRadius(stroke, 0f);
            SetStroke(stroke, Sc(1f));

            var check = EnsureChildImage(box, "Check", 1);
            CenterBox(Rect(check.gameObject), Sc(7f), Sc(7f));
            check.sprite = Load<Sprite>($"{SpriteDir}/objectives_check.png");
            check.color = CheckDark;

            return new ObjectiveView.ObjectiveRowWidgets
            {
                Box = fill, BoxBorder = stroke, Check = check,
            };
        }

        // ---- rows the spec drops -----------------------------------------------------------------

        static void HideRowsTheSpecDrops(ObjectiveView view)
        {
            if (view.NubTurn != null) Hide(view.NubTurn.gameObject);
            if (view.TurnValue != null) Hide(view.TurnValue.gameObject);
            if (view.EnemiesValue != null) Hide(view.EnemiesValue.gameObject);

            foreach (string name in new[] { "Title", "Divider", "Label_Turn", "Label_Enemies" })
            {
                Transform found = view.transform.Find("Expanded/" + name);
                if (found != null) Hide(found.gameObject);
            }
        }

        static void Hide(GameObject go)
        {
            if (go == null) return;
            Touch(go);
            go.SetActive(false);
        }

        static void WireColours(ObjectiveView view)
        {
            view.IncompleteText = White;
            view.CompleteText = DimText;
            view.CheckboxFill = Cyan;
            view.CheckboxBorder = BoxBorder;
        }

        // ---- housekeeping -------------------------------------------------------------------------

        static void PruneStaleDecoration(GameObject root)
        {
            var hidden = new List<string>();
            foreach (var graphic in root.GetComponentsInChildren<Graphic>(true))
            {
                GameObject go = graphic.gameObject;
                if (touched.Contains(go) || go == root) continue;

                go.SetActive(false);
                hidden.Add(go.name);
            }
            if (hidden.Count > 0)
                Debug.Log($"[RestyleObjectivesPanel] Hid {hidden.Count} leftover graphic(s): " +
                          string.Join(", ", hidden));
        }

        static void TrackReferences(Object component)
        {
            wiredFields.Clear();
            var iterator = new SerializedObject(component).GetIterator();
            while (iterator.Next(true))
            {
                if (iterator.propertyType != SerializedPropertyType.ObjectReference) continue;
                if (iterator.objectReferenceValue == null) continue;
                if (!(iterator.objectReferenceValue is GameObject || iterator.objectReferenceValue is Component))
                    continue;

                wiredFields.Add((component, iterator.propertyPath));
            }
        }

        static void VerifyReferencesSurvived()
        {
            var emptied = new List<string>();
            foreach (var (owner, path) in wiredFields)
            {
                var property = new SerializedObject(owner).FindProperty(path);
                if (property != null && property.objectReferenceValue == null)
                    emptied.Add(owner.GetType().Name + "." + path);
            }

            if (emptied.Count > 0)
                Debug.LogError($"[RestyleObjectivesPanel] {emptied.Count} serialized reference(s) came " +
                               "out empty - the panel was NOT styled safely: " + string.Join(", ", emptied));
            else
                Debug.Log($"[RestyleObjectivesPanel] All {wiredFields.Count} serialized references still wired.");
        }

        // ---- scene ------------------------------------------------------------------------------

        static Scene FindOrOpenScene(out bool openedHere)
        {
            for (int i = 0; i < SceneManager.sceneCount; i++)
            {
                Scene loaded = SceneManager.GetSceneAt(i);
                if (loaded.path == ScenePath && loaded.isLoaded)
                {
                    openedHere = false;
                    return loaded;
                }
            }
            openedHere = true;
            return EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Additive);
        }

        static ObjectiveView FindView(Scene scene)
        {
            foreach (GameObject root in scene.GetRootGameObjects())
            {
                var found = root.GetComponentInChildren<ObjectiveView>(true);
                if (found != null) return found;
            }
            return null;
        }

        // ---- helpers ----------------------------------------------------------------------------

        static RectTransform Rect(GameObject go) => go.GetComponent<RectTransform>();

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

        static void ApplyFont(TextMeshProUGUI label, string weight, float size, Color colour)
        {
            Touch(label.gameObject);
            var font = Load<TMP_FontAsset>($"{FontDir}/NotoSans-{weight} SDF.asset");
            if (font != null) label.font = font;

            label.fontSize = size;
            label.color = colour;
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

        static void Reparent(GameObject go, GameObject parent, int siblingIndex)
        {
            Touch(go);
            go.transform.SetParent(parent.transform, false);
            go.transform.SetSiblingIndex(siblingIndex);
        }

        static void Touch(GameObject go)
        {
            if (go != null) touched.Add(go);
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
            return EnsureImage(go);
        }

        static Image EnsureImage(GameObject go)
        {
            Touch(go);
            var image = EnsureComponent<Image>(go);
            image.raycastTarget = false;
            return image;
        }

        // Swaps a plain Image for MPUIKit's procedural one so a hairline can be drawn as a stroke.
        // An outline is modulated by the graphic's alpha, so an outlined transparent frame renders
        // nothing - a stroke draws the ring in the graphic's own colour.
        static Image EnsureMpImage(GameObject go)
        {
            Touch(go);
            var existing = go.GetComponent<MPImage>();
            if (existing != null) return existing;

            var plain = go.GetComponent<Image>();
            Color colour = plain != null ? plain.color : Color.white;
            if (plain != null) Object.DestroyImmediate(plain);

            var replacement = go.AddComponent<MPImage>();
            replacement.color = colour;
            replacement.raycastTarget = false;
            return replacement;
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

        static TextMeshProUGUI FindLabel(GameObject root, string name)
        {
            foreach (var label in root.GetComponentsInChildren<TextMeshProUGUI>(true))
                if (label.gameObject.name == name) return label;
            return null;
        }

        static T Load<T>(string path) where T : Object => AssetDatabase.LoadAssetAtPath<T>(path);

        static Color Hex(string hex)
        {
            byte r = byte.Parse(hex.Substring(0, 2), System.Globalization.NumberStyles.HexNumber);
            byte g = byte.Parse(hex.Substring(2, 2), System.Globalization.NumberStyles.HexNumber);
            byte b = byte.Parse(hex.Substring(4, 2), System.Globalization.NumberStyles.HexNumber);
            return new Color(r / 255f, g / 255f, b / 255f, 1f);
        }
    }
}
