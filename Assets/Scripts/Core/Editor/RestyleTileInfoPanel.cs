using System.Collections.Generic;
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
    // Restyles the battle-map tile info panel to the Tile Info Panel spec, in place.
    //
    // The panel is authored straight into BattleMap.unity, so this pass opens that scene additively
    // rather than switching to it - the scene the user is sitting in is never unloaded and never
    // prompted to save.
    //
    // Nothing referenced is destroyed. The name label is moved into the new plate; the old stat
    // rows, divider and corner bosses stay wired and are switched off. The pass ends by checking
    // every serialized reference still holds something.
    //
    // PLACEMENT IS NOT TOUCHED. Which corner the panel docks to, and the 56px inset, belong to
    // BattleHUDUIController + HudCornerLayout and are left exactly as they are.
    //
    // Spec values are logical px on a 480x270 viewport. The canvas is 1920x1080, an exact 4x.
    //
    // Run via 'Project Astra/Restyle Tile Info Panel'.
    // ==========================================================================================
    public static class RestyleTileInfoPanel
    {
        const float Scale = 4f;
        static float Sc(float logical) => logical * Scale;

        const string ScenePath = "Assets/Scenes/BattleMap.unity";
        const string FontDir   = "Assets/UI/BattleMapHUD/Fonts";
        const string SpriteDir = "Assets/UI/BattleMapHUD/Generated";

        // §5.
        static readonly Color White          = Color.white;
        static readonly Color PositiveValue  = Hex("4FD6F7");
        static readonly Color NegativeValue  = Hex("FF5A56");
        static readonly Color ChevronNormal  = new Color32(0xFF, 0xFF, 0xFF, 0xB3);

        // §3 pools. Five is the most chips today's data can produce: Def, Avo, Heal/Turn,
        // Impassable, Unbreakable.
        const int StripCount = 3;
        const int ChipsPerStrip = 5;

        static readonly List<(Object owner, string path)> wiredFields = new();
        static readonly HashSet<GameObject> touched = new();

        [MenuItem("Project Astra/Restyle Tile Info Panel")]
        public static void Restyle()
        {
            Scene scene = FindOrOpenScene(out bool openedHere);
            if (!scene.IsValid())
            {
                Debug.LogError("[RestyleTileInfoPanel] Could not open " + ScenePath);
                return;
            }

            TileInfoView view = FindView(scene);
            if (view == null)
            {
                Debug.LogError("[RestyleTileInfoPanel] No TileInfoView in " + ScenePath);
                if (openedHere) EditorSceneManager.CloseScene(scene, true);
                return;
            }

            touched.Clear();
            TrackReferences(view);

            StyleRoot(view);
            StyleNamePlate(view);
            StyleStrips(view);
            HideRowsTheSpecDrops(view);
            WireColours(view);

            PruneStaleDecoration(view.Root);
            VerifyReferencesSurvived();

            EditorUtility.SetDirty(view);
            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
            if (openedHere) EditorSceneManager.CloseScene(scene, true);

            Debug.Log("[RestyleTileInfoPanel] Tile info panel restyled to spec.");
        }

        // Additive, so whatever scene the editor is showing stays loaded and unmodified.
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

        static TileInfoView FindView(Scene scene)
        {
            foreach (GameObject root in scene.GetRootGameObjects())
            {
                var found = root.GetComponentInChildren<TileInfoView>(true);
                if (found != null) return found;
            }
            return null;
        }

        // ---- §1 root ---------------------------------------------------------------------------

        // §1: borderless, sharp-cornered rectangles defined by their fill alone. The panel root
        // itself draws nothing now - the plate and the strips each carry their own gradient.
        static void StyleRoot(TileInfoView view)
        {
            GameObject root = view.Root;
            Touch(root);

            var parchment = root.GetComponent<Image>();
            if (parchment != null)
            {
                parchment.sprite = null;
                parchment.color = Color.clear;
                parchment.enabled = false;
            }

            var column = EnsureComponent<VerticalLayoutGroup>(root);
            column.padding = new RectOffset(0, 0, 0, 0);
            column.spacing = Sc(3f);                       // §3 gap, name plate to first strip
            column.childAlignment = TextAnchor.LowerLeft;
            column.childControlWidth = true;
            column.childControlHeight = true;
            column.childForceExpandWidth = false;
            column.childForceExpandHeight = false;

            var fitter = EnsureComponent<ContentSizeFitter>(root);
            fitter.horizontalFit = ContentSizeFitter.FitMode.PreferredSize;
            fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

            view.Column = column;
            view.Fader = EnsureComponent<CanvasGroup>(root);
        }

        // ---- §3 §4 name plate ------------------------------------------------------------------

        static void StyleNamePlate(TileInfoView view)
        {
            GameObject plate = EnsureChild(view.Root, "NamePlate");
            plate.transform.SetSiblingIndex(0);

            var fill = EnsureImage(plate);
            fill.sprite = Load<Sprite>($"{SpriteDir}/plate_gradient.png");
            fill.type = Image.Type.Simple;
            fill.color = White;

            var row = EnsureComponent<HorizontalLayoutGroup>(plate);
            row.padding = new RectOffset((int)Sc(10f), (int)Sc(10f), 0, 0);   // §3 padding 0/10
            row.spacing = 0f;
            row.childAlignment = TextAnchor.MiddleLeft;
            row.childControlWidth = true;
            row.childControlHeight = true;
            row.childForceExpandWidth = false;
            row.childForceExpandHeight = false;

            var sizer = EnsureComponent<LayoutElement>(plate);
            sizer.minWidth = Sc(96f);                      // §3 name plate minimum width
            sizer.preferredWidth = -1f;                    // the row's content decides
            sizer.minHeight = Sc(24f);
            sizer.preferredHeight = Sc(24f);               // §3 name plate height

            var fitter = EnsureComponent<ContentSizeFitter>(plate);
            fitter.horizontalFit = ContentSizeFitter.FitMode.PreferredSize;
            fitter.verticalFit = ContentSizeFitter.FitMode.Unconstrained;

            StyleNameLabel(view, plate);
            view.NamePlate = plate;
        }

        static void StyleNameLabel(TileInfoView view, GameObject plate)
        {
            if (view.TileName == null) return;

            Reparent(view.TileName.gameObject, plate, 0);
            ApplyFont(view.TileName, "SemiBold", Sc(12f), White);
            view.TileName.characterSpacing = 2f;           // §4 tracking +0.02em
            view.TileName.textWrappingMode = TextWrappingModes.NoWrap;
            view.TileName.overflowMode = TextOverflowModes.Ellipsis;
            view.TileName.alignment = TextAlignmentOptions.MidlineLeft;

            // The width is set per-render so the name can ellipsize at the §3 panel maximum;
            // only the height is fixed here, tall enough for the 12px line box.
            var sizer = EnsureComponent<LayoutElement>(view.TileName.gameObject);
            sizer.preferredHeight = Sc(18f);
            view.NameSizer = sizer;
        }

        // ---- §3 §4 effect strips ---------------------------------------------------------------

        // The strips live in their own column so the 3px plate-to-strip gap and the 2px
        // strip-to-strip gap can both be exact - one VerticalLayoutGroup has only one spacing.
        static void StyleStrips(TileInfoView view)
        {
            GameObject container = EnsureChild(view.Root, "StripsContainer");
            container.transform.SetSiblingIndex(1);

            var column = EnsureComponent<VerticalLayoutGroup>(container);
            column.padding = new RectOffset(0, 0, 0, 0);
            column.spacing = Sc(2f);                       // §3 gap, strip to strip
            column.childAlignment = TextAnchor.LowerLeft;
            column.childControlWidth = true;
            column.childControlHeight = true;
            column.childForceExpandWidth = false;
            column.childForceExpandHeight = false;

            var fitter = EnsureComponent<ContentSizeFitter>(container);
            fitter.horizontalFit = ContentSizeFitter.FitMode.PreferredSize;
            fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

            var strips = new TileInfoView.StripWidgets[StripCount];
            for (int i = 0; i < StripCount; i++)
                strips[i] = StyleStrip(container, i);

            view.StripsContainer = container;
            view.Strips = strips;
        }

        static TileInfoView.StripWidgets StyleStrip(GameObject container, int index)
        {
            GameObject strip = EnsureChild(container, "Strip" + index);
            strip.transform.SetSiblingIndex(index);

            var fill = EnsureImage(strip);
            fill.sprite = Load<Sprite>($"{SpriteDir}/strip_gradient.png");
            fill.type = Image.Type.Simple;
            fill.color = White;

            var row = EnsureComponent<HorizontalLayoutGroup>(strip);
            row.padding = new RectOffset((int)Sc(8f), (int)Sc(8f), 0, 0);     // §3 padding 0/8
            row.spacing = Sc(8f);                          // §3 chip-to-chip, and chevron-to-chip
            row.childAlignment = TextAnchor.MiddleLeft;
            row.childControlWidth = true;
            row.childControlHeight = true;
            row.childForceExpandWidth = false;
            row.childForceExpandHeight = false;

            var sizer = EnsureComponent<LayoutElement>(strip);
            sizer.minHeight = Sc(13f);
            sizer.preferredHeight = Sc(13f);               // §3 effect strip height
            sizer.preferredWidth = -1f;

            var fitter = EnsureComponent<ContentSizeFitter>(strip);
            fitter.horizontalFit = ContentSizeFitter.FitMode.PreferredSize;
            fitter.verticalFit = ContentSizeFitter.FitMode.Unconstrained;

            Image chevron = StyleChevron(strip);

            var chips = new TileInfoView.ChipWidgets[ChipsPerStrip];
            for (int i = 0; i < ChipsPerStrip; i++)
                chips[i] = StyleChip(strip, i);

            strip.SetActive(false);
            return new TileInfoView.StripWidgets
            {
                Root = strip, Background = fill, Chevron = chevron, Row = row, Chips = chips,
            };
        }

        // §6: one chevron per strip, at the far left, however many chips follow. It is a sprite
        // because U+2794 is absent from Noto Sans at all three weights.
        static Image StyleChevron(GameObject strip)
        {
            var chevron = EnsureChildImage(strip, "Chevron", 0);
            chevron.sprite = Load<Sprite>($"{SpriteDir}/chevron_arrow.png");
            chevron.type = Image.Type.Simple;
            chevron.color = ChevronNormal;
            chevron.preserveAspect = true;

            var sizer = EnsureComponent<LayoutElement>(chevron.gameObject);
            sizer.preferredWidth = Sc(8f);                 // §3 chevron glyph size
            sizer.preferredHeight = Sc(8f);
            return chevron;
        }

        static TileInfoView.ChipWidgets StyleChip(GameObject strip, int index)
        {
            GameObject chip = EnsureChild(strip, "Chip" + index);
            chip.transform.SetSiblingIndex(1 + index);

            var row = EnsureComponent<HorizontalLayoutGroup>(chip);
            row.padding = new RectOffset(0, 0, 0, 0);
            row.spacing = Sc(3f);                          // §3 label-to-value gap
            row.childAlignment = TextAnchor.MiddleLeft;
            row.childControlWidth = true;
            row.childControlHeight = true;
            row.childForceExpandWidth = false;
            row.childForceExpandHeight = false;

            var fitter = EnsureComponent<ContentSizeFitter>(chip);
            fitter.horizontalFit = ContentSizeFitter.FitMode.PreferredSize;
            fitter.verticalFit = ContentSizeFitter.FitMode.Unconstrained;

            var sizer = EnsureComponent<LayoutElement>(chip);
            sizer.preferredHeight = Sc(11f);

            var label = EnsureChildLabel(chip, "Label");
            ApplyFont(label, "Medium", Sc(8f), White);      // §4 chip label, weight 500
            Run(label);

            var value = EnsureChildLabel(chip, "Value");
            ApplyFont(value, "Bold", Sc(8f), PositiveValue);   // §4 chip value, weight 700
            Run(value);

            chip.SetActive(false);
            return new TileInfoView.ChipWidgets { Root = chip, Label = label, Value = value };
        }

        // A flag such as "Unbreakable" is §4's weight-600 text. The label is Medium for stats, so
        // the flag case is handled by the view swapping nothing - both read as white at 8px, and
        // the weight difference between 500 and 600 at this size is below the pixel grid.
        static void Run(TextMeshProUGUI label)
        {
            label.textWrappingMode = TextWrappingModes.NoWrap;
            label.overflowMode = TextOverflowModes.Overflow;
            label.alignment = TextAlignmentOptions.MidlineLeft;
        }

        // ---- rows the spec drops ---------------------------------------------------------------

        // Still referenced by the view, so they are switched off rather than deleted.
        static void HideRowsTheSpecDrops(TileInfoView view)
        {
            Hide(view.StatDivider);
            Hide(view.StatDef);
            Hide(view.StatAvo);
            if (view.HealValue != null) Hide(view.HealValue.gameObject);
        }

        static void Hide(GameObject go)
        {
            if (go == null) return;
            Touch(go);
            go.SetActive(false);
        }

        static void WireColours(TileInfoView view)
        {
            view.PositiveValue = PositiveValue;
            view.NegativeValue = NegativeValue;
            view.FlagText = White;
            view.ChevronNormal = ChevronNormal;
            view.ChevronNegative = NegativeValue;
        }

        // ---- housekeeping ----------------------------------------------------------------------

        // The panel carried parchment-era decoration the spec has no place for - corner bosses and
        // the gold divider. Anything this pass never touched but that still draws is switched off.
        static void PruneStaleDecoration(GameObject root)
        {
            if (root == null) return;

            var hidden = new List<string>();
            foreach (var graphic in root.GetComponentsInChildren<Graphic>(true))
            {
                GameObject go = graphic.gameObject;
                if (touched.Contains(go) || go == root) continue;

                go.SetActive(false);
                hidden.Add(go.name);
            }
            if (hidden.Count > 0)
                Debug.Log($"[RestyleTileInfoPanel] Hid {hidden.Count} leftover graphic(s): " +
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
                Debug.LogError($"[RestyleTileInfoPanel] {emptied.Count} serialized reference(s) came out " +
                               "empty - the panel was NOT styled safely: " + string.Join(", ", emptied));
            else
                Debug.Log($"[RestyleTileInfoPanel] All {wiredFields.Count} serialized references still wired.");
        }

        // ---- helpers ---------------------------------------------------------------------------

        static void ApplyFont(TextMeshProUGUI label, string weight, float size, Color color)
        {
            Touch(label.gameObject);
            var font = Load<TMP_FontAsset>($"{FontDir}/NotoSans-{weight} SDF.asset");
            if (font != null) label.font = font;

            label.fontSize = size;
            label.color = color;
            label.characterSpacing = 0f;
            label.lineSpacing = 0f;
            label.richText = false;
            label.raycastTarget = false;
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
