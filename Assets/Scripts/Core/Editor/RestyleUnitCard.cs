using MPUIKIT;
using ProjectAstra.Core.UI.BattleMap.HUD;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;

namespace ProjectAstra.EditorTools
{
    // ==========================================================================================
    // Restyles the existing battle-map unit card to the GBA-faithful spec, in place.
    //
    // It resolves every target through UnitCardView's own serialized references rather than by
    // name or path, and it never destroys a GameObject - so all six original references survive
    // and the card can be re-styled repeatedly without re-wiring the scene.
    //
    // Spec values are logical px in a 480x270 space. The project canvas is 1920x1080, an exact
    // 4x, so every number below is the spec value multiplied by Scale.
    //
    // Run via 'Project Astra/Restyle Unit Card'.
    // ==========================================================================================
    public static class RestyleUnitCard
    {
        const float Scale = 4f;
        static float Sc(float logical) => logical * Scale;

        const string FontDir      = "Assets/UI/Shared/Fonts/";
        const string ActedMatPath = "Assets/UI/Shared/Materials/UnitCardActedPortrait.mat";
        const string BustPath     = "Assets/UI/Shared/Sprites/unit_bust_placeholder.png";

        // §8 palette.
        static readonly Color CardBackground = new Color32(22, 26, 33, 240); // .94 alpha
        static readonly Color Border         = new Color32(0x4a, 0x52, 0x61, 0xff);
        static readonly Color Emphasis       = new Color32(0xff, 0xff, 0xff, 0xff);
        static readonly Color Muted          = new Color32(0x9a, 0xa2, 0xae, 0xff);
        static readonly Color BarTrack       = new Color32(0x33, 0x3a, 0x45, 0xff);
        static readonly Color BarTrackBorder = new Color32(0x10, 0x14, 0x1a, 0xff);
        static readonly Color AllyAccent     = new Color32(0x4f, 0x9d, 0xff, 0xff);
        static readonly Color HpFillColor    = new Color32(0x59, 0x9e, 0x66, 0xff);

        // ---- knobs worth eyeballing ---------------------------------------------------------
        // HP bar corner rounding, in logical px. The bar is 5 logical tall, so 2.5 is a full
        // pill and 0 is the square original. Change and re-run the menu item.
        const float BarCornerRadius = 1.5f;

        // Shared with TileInfoView and ObjectiveView so the three panels sit level.
        const float EdgePad = 56f;

        // Outline on the fill itself, set by hand in the Inspector and mirrored here so a
        // re-run keeps it. The colour is still transparent, so raise its alpha to see it.
        const float BarFillOutlineWidth = 4f;
        static readonly Color BarFillOutline = new Color(0f, 0f, 0f, 0f);

        [MenuItem("Project Astra/Restyle Unit Card")]
        public static void Restyle()
        {
            var view = Object.FindAnyObjectByType<UnitCardView>(FindObjectsInactive.Include);
            if (view == null)
            {
                Debug.LogError("[RestyleUnitCard] No UnitCardView in the open scene.");
                return;
            }

            StyleCard(view);
            StylePortrait(view);
            StyleTextColumn(view);
            StyleHpBar(view);
            HideRowsTheSpecDrops(view);
            WireNewReferences(view);

            EditorUtility.SetDirty(view);
            EditorSceneManager.MarkSceneDirty(view.gameObject.scene);
            Debug.Log("[RestyleUnitCard] Unit card restyled to spec.");
        }

        // ---- §3 card container -------------------------------------------------------------

        // Height is 44, not the spec's stated 54. The spec's own §4 content list only fills 44
        // (5 padding + 34 portrait + 5 padding); the extra 10 was empty space, most likely
        // measured from a build with the §10 optional rows switched on.
        static void StyleCard(UnitCardView view)
        {
            var card = Rect(view.Root);
            card.anchorMin = card.anchorMax = card.pivot = new Vector2(0f, 1f);
            card.sizeDelta = new Vector2(Sc(114f), Sc(44f));
            // Edit-time placeholder only; HudCornerLayout re-docks the card at runtime using
            // the same 56px pad as the tile info and objective panels.
            card.anchoredPosition = new Vector2(EdgePad, -EdgePad);

            var background = view.Root.GetComponent<Image>();
            if (background != null)
            {
                background.color = CardBackground;
                SetOutline(background, Sc(1f), Border);
            }

            EnsureComponent<CanvasGroup>(view.Root);
        }

        // MPUIKit draws the hairline border natively; reached through SerializedObject so this
        // script does not need a compile-time dependency on the package.
        static void SetOutline(Image image, float width, Color color)
        {
            var so = new SerializedObject(image);
            var widthProp = so.FindProperty("m_OutlineWidth");
            var colorProp = so.FindProperty("m_OutlineColor");
            if (widthProp == null || colorProp == null) return;

            widthProp.floatValue = width;
            colorProp.colorValue = color;
            so.ApplyModifiedPropertiesWithoutUndo();
        }

        // ---- §5 portrait ---------------------------------------------------------------------

        // The frame doubles as a Mask for crop-to-fill, so its own graphic has to stay opaque or
        // the stencil clips everything away. It is hidden instead, and the hairline border lives
        // on a sibling that sits outside the mask.
        static void StylePortrait(UnitCardView view)
        {
            var frame = Rect(view.PortraitImage.transform.parent.gameObject);
            PlacePortraitBox(frame);

            var frameImage = frame.GetComponent<Image>();
            if (frameImage != null)
            {
                frameImage.color = Color.white;
                SetOutline(frameImage, 0f, Color.clear);
            }

            var mask = frame.GetComponent<Mask>();
            if (mask != null) mask.showMaskGraphic = false;

            var backdrop = EnsureChildImage(frame.gameObject, "PortraitBackdrop", 0);
            Stretch(Rect(backdrop.gameObject));
            backdrop.color = new Color(AllyAccent.r, AllyAccent.g, AllyAccent.b, 0.15f);

            Stretch(Rect(view.PortraitImage.gameObject));
            view.PortraitImage.preserveAspect = true;
            view.PortraitImage.transform.SetAsLastSibling();

            StylePortraitBorder(view, frame);
        }

        static void StylePortraitBorder(UnitCardView view, RectTransform frame)
        {
            var border = EnsureChildImage(view.Root, "PortraitBorder", frame.GetSiblingIndex() + 1);
            PlacePortraitBox(Rect(border.gameObject));
            border.color = Color.clear;
            SetOutline(border, Sc(1f), Border);
        }

        static void PlacePortraitBox(RectTransform rect)
        {
            rect.anchorMin = rect.anchorMax = rect.pivot = new Vector2(0f, 1f);
            rect.sizeDelta = new Vector2(Sc(34f), Sc(34f));
            rect.anchoredPosition = new Vector2(Sc(6f), -Sc(5f));
        }

        // ---- §6 typography ---------------------------------------------------------------------

        static void StyleTextColumn(UnitCardView view)
        {
            var column = Rect(view.UnitName.transform.parent.gameObject);
            column.anchorMin = column.anchorMax = column.pivot = new Vector2(0f, 1f);
            column.sizeDelta = new Vector2(Sc(62f), Sc(34f));
            column.anchoredPosition = new Vector2(Sc(45f), -Sc(5f));

            StyleName(view.UnitName);
            StyleHpRow(view);
        }

        static void StyleName(TextMeshProUGUI label)
        {
            // The box is taller than the 1.3 line box because TMP drops a whole line when the
            // rect cannot fit the font's own line height. Top-aligned, so glyphs do not move.
            var rect = Rect(label.gameObject);
            rect.anchorMin = rect.anchorMax = rect.pivot = new Vector2(0f, 1f);
            rect.sizeDelta = new Vector2(Sc(62f), Sc(16f));
            rect.anchoredPosition = Vector2.zero;

            ApplyFont(label, "Bold", Sc(8f), Emphasis);
            label.characterSpacing = 2f;             // 0.02em
            label.textWrappingMode = TextWrappingModes.NoWrap;
            label.overflowMode = TextOverflowModes.Ellipsis;
            label.alignment = TextAlignmentOptions.TopLeft;
        }

        // "HP" then the numeral pair, laid out left to right on a shared baseline. The value and
        // its "/max" go in their own zero-gap group so they read as the single run the spec
        // asks for, whatever the digit count.
        static void StyleHpRow(UnitCardView view)
        {
            GameObject hpContainer = BarTrackOf(view).parent.gameObject;
            PlaceInColumn(Rect(hpContainer), -Sc(11.4f), Sc(28f));

            GameObject textRow = EnsureChild(hpContainer, "HpTextRow");
            PlaceInColumn(Rect(textRow), 0f, Sc(11f));
            ConfigureRow(textRow, Sc(3f));

            GameObject numeric = EnsureChild(textRow, "HpNumeric");
            ConfigureRow(numeric, 0f);

            var hpLabel = FindDescendantLabel(view.Root, "HPLabel");
            if (hpLabel != null)
            {
                Reparent(hpLabel.gameObject, textRow, 0);
                ApplyFont(hpLabel, "Regular", Sc(6f), Muted);
                hpLabel.characterSpacing = 6f;       // 0.06em
                hpLabel.text = "HP";
                AlignRun(hpLabel);
            }

            // Searched from the card root, not the group, so a re-run adopts the label a
            // previous run parked elsewhere instead of making a second one.
            var hpMax = FindDescendantLabel(view.Root, "HPMax")
                        ?? EnsureChildLabel(numeric, "HPMax");
            Reparent(view.HpValue.gameObject, numeric, 0);
            Reparent(hpMax.gameObject, numeric, 1);

            ApplyFont(view.HpValue, "Bold", Sc(8f), Emphasis);
            AlignRun(view.HpValue);
            ApplyFont(hpMax, "Regular", Sc(8f), Muted);
            AlignRun(hpMax);
        }

        static void AlignRun(TextMeshProUGUI label)
        {
            label.alignment = TextAlignmentOptions.BottomLeft;
            label.textWrappingMode = TextWrappingModes.NoWrap;
            label.overflowMode = TextOverflowModes.Overflow;
        }

        static void ConfigureRow(GameObject go, float spacing)
        {
            var row = EnsureComponent<HorizontalLayoutGroup>(go);
            row.spacing = spacing;
            row.childAlignment = TextAnchor.LowerLeft;
            row.childControlWidth = true;
            row.childControlHeight = true;
            row.childForceExpandWidth = false;
            row.childForceExpandHeight = false;
        }

        static void ApplyFont(TextMeshProUGUI label, string weight, float size, Color color)
        {
            var font = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(
                $"{FontDir}JetBrainsMono-{weight} SDF.asset");
            if (font != null) label.font = font;

            label.fontSize = size;
            label.color = color;
            label.lineSpacing = 0f;
            label.richText = false;
        }

        // ---- §7 HP bar -------------------------------------------------------------------------

        // The bar is three deep in the scene - HpBar wraps HpBarBg wraps HpFill - so the outer
        // node is the one that gets positioned and carries the track colour and border.
        static void StyleHpBar(UnitCardView view)
        {
            GameObject fillObject = view.HpFill.gameObject;
            Transform inner = view.HpFill.transform.parent;
            Transform outer = BarTrackOf(view);

            PlaceInColumn(Rect(outer.gameObject), -Sc(12f), Sc(5f));

            var trackImage = EnsureMpImage(outer.gameObject);
            if (trackImage != null)
            {
                trackImage.color = BarTrack;
                SetOutline(trackImage, Sc(1f), BarTrackBorder);
                SetCornerRadius(trackImage, Sc(BarCornerRadius));
            }

            if (inner != outer)
            {
                Stretch(Rect(inner.gameObject));
                ClearGraphic(inner.GetComponent<Image>());
            }

            // Width is driven per-render by UnitCardView, which sets the fill's anchors.
            var fill = EnsureMpImage(fillObject);
            Stretch(Rect(fillObject));
            fill.color = HpFillColor;
            fill.raycastTarget = false;
            SetOutline(fill, BarFillOutlineWidth, BarFillOutline);
            SetCornerRadius(fill, Sc(BarCornerRadius));

            RewireHpFill(view, fill);
        }

        // The bar shipped as a plain Image, which cannot round its corners. Swapping in
        // MPUIKit's procedural image is the one place a serialized reference changes, so the
        // View is re-pointed at the replacement straight away.
        static Image EnsureMpImage(GameObject go)
        {
            var existing = go.GetComponent<MPImage>();
            if (existing != null) return existing;

            var plain = go.GetComponent<Image>();
            Color color = plain != null ? plain.color : Color.white;
            bool raycast = plain != null && plain.raycastTarget;
            if (plain != null) Object.DestroyImmediate(plain);

            var replacement = go.AddComponent<MPImage>();
            replacement.color = color;
            replacement.raycastTarget = raycast;
            return replacement;
        }

        static void RewireHpFill(UnitCardView view, Image fill)
        {
            var so = new SerializedObject(view);
            so.FindProperty("HpFill").objectReferenceValue = fill;
            so.ApplyModifiedPropertiesWithoutUndo();
        }

        // MPUIKit rounds procedurally, so the radius is a component value rather than a
        // 9-sliced sprite - drag it in the Inspector for a quick look, or change the constant.
        // Set through the typed property, because its setter is what pushes the radius into
        // the shared material; writing the backing field alone renders as a square.
        static void SetCornerRadius(Image image, float radius)
        {
            if (!(image is MPImage procedural)) return;

            procedural.DrawShape = DrawShape.Rectangle;

            Rectangle shape = procedural.Rectangle;
            shape.CornerRadius = new Vector4(radius, radius, radius, radius);
            procedural.Rectangle = shape;

            procedural.SetAllDirty();
            EditorUtility.SetDirty(procedural);
        }

        static Transform BarTrackOf(UnitCardView view)
        {
            Transform inner = view.HpFill.transform.parent;
            return inner.parent != null ? inner.parent : inner;
        }

        // ---- rows the spec does not have ---------------------------------------------------------

        static void HideRowsTheSpecDrops(UnitCardView view)
        {
            if (view.LvlValue != null)
                view.LvlValue.transform.parent.gameObject.SetActive(false);

            var art = view.Root.transform.Find("Art");
            if (art != null) art.gameObject.SetActive(false);
        }

        // ---- wiring ------------------------------------------------------------------------------

        static void WireNewReferences(UnitCardView view)
        {
            var so = new SerializedObject(view);
            so.FindProperty("HpMax").objectReferenceValue =
                FindDescendantLabel(view.Root, "HPMax");
            so.FindProperty("PortraitBackdrop").objectReferenceValue =
                view.PortraitImage.transform.parent.Find("PortraitBackdrop").GetComponent<Image>();
            so.FindProperty("Fader").objectReferenceValue = view.Root.GetComponent<CanvasGroup>();
            so.FindProperty("ActedPortraitMaterial").objectReferenceValue =
                AssetDatabase.LoadAssetAtPath<Material>(ActedMatPath);
            so.FindProperty("PlaceholderBust").objectReferenceValue =
                AssetDatabase.LoadAssetAtPath<Sprite>(BustPath);
            so.ApplyModifiedPropertiesWithoutUndo();
        }

        // ---- helpers -----------------------------------------------------------------------------

        static RectTransform Rect(GameObject go) => go.GetComponent<RectTransform>();

        static void PlaceInColumn(RectTransform rect, float y, float height)
        {
            rect.anchorMin = rect.anchorMax = rect.pivot = new Vector2(0f, 1f);
            rect.sizeDelta = new Vector2(Sc(62f), height);
            rect.anchoredPosition = new Vector2(0f, y);
        }

        static void Stretch(RectTransform rect)
        {
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;
        }

        static void ClearGraphic(Image image)
        {
            if (image == null) return;
            image.color = Color.clear;
            SetOutline(image, 0f, Color.clear);
        }

        static void Reparent(GameObject go, GameObject parent, int siblingIndex)
        {
            go.transform.SetParent(parent.transform, false);
            go.transform.SetSiblingIndex(siblingIndex);
        }

        static TextMeshProUGUI FindDescendantLabel(GameObject root, string name)
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
            if (existing != null) return existing.gameObject;

            var go = new GameObject(name, typeof(RectTransform));
            go.transform.SetParent(parent.transform, false);
            return go;
        }

        static TextMeshProUGUI EnsureChildLabel(GameObject parent, string name)
        {
            var existing = FindDescendantLabel(parent, name);
            if (existing != null) return existing;

            var go = EnsureChild(parent, name);
            return EnsureComponent<TextMeshProUGUI>(go);
        }

        static Image EnsureChildImage(GameObject parent, string name, int siblingIndex)
        {
            var existing = parent.transform.Find(name);
            if (existing != null) return existing.GetComponent<Image>();

            var go = EnsureChild(parent, name);
            go.transform.SetSiblingIndex(siblingIndex);
            var image = go.AddComponent<Image>();
            image.raycastTarget = false;
            return image;
        }
    }
}
