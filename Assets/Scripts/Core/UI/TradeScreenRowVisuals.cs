using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace ProjectAstra.Core.UI
{
    // Visual state for a single inventory row in the Trade Screen.
    // Values map 1:1 to the JSX mockup's state styles; see docs/mockups/.../e7379e7e-*.js.
    public enum TradeRowVisualState
    {
        Default,
        Hover,     // cursor rests on this row
        Focused,   // cursor rests on this row in browsing phase (brass glow)
        Pressed,   // cursor rests on this row in selected phase
        Selected,  // this row is picked for swap
        Disabled,  // item is broken/depleted
    }

    // Shared visual logic for Trade Screen rows. Both the editor builder (demo path)
    // and the runtime TradeScreenUI call these helpers so one source controls how
    // a row looks in a given state.
    //
    // A row hierarchy built via TradeScreenBuilder.BuildItemRow(...) is:
    //   Row_{index}
    //   ├── Background          (Image)
    //   ├── BorderTop/Bottom/Left/Right (4x Image, 1px edges)
    //   ├── Sigil               (Image)
    //   ├── ItemName            (TMP)
    //   ├── Qty                 (TMP)
    //   ├── SelectionDiamond    (Image, disabled by default)
    //   ├── FocusCaret          (Image, disabled by default)
    //   ├── EmptyLabel          (TMP, shown when slot is empty)
    //   └── DashedTop           (Image, shown when slot is empty)
    public static class TradeScreenRowVisuals
    {
        // Match TradeScreenBuilder's CODEX palette — keep in sync.
        static readonly Color ColParchment   = Hex("f0e5c8");
        static readonly Color ColParchmentHi = Hex("fff5d8");
        static readonly Color ColBrass       = Hex("c9993a");
        static readonly Color ColBrassLite   = Hex("e8c66a");
        static readonly Color ColVermillion  = Hex("b0382a");

        public static void SetRowState(Transform row, TradeRowVisualState state)
        {
            var bg = row.Find("Background")?.GetComponent<Image>();
            if (bg != null) bg.color = BackgroundColor(state);

            // All 4 edges get the same border color per state.
            var borderColor = BorderColor(state);
            SetEdgeColor(row, "BorderTop", borderColor);
            SetEdgeColor(row, "BorderBottom", borderColor);
            SetEdgeColor(row, "BorderLeft", borderColor);
            SetEdgeColor(row, "BorderRight", borderColor);

            var diamond = row.Find("SelectionDiamond");
            if (diamond != null) diamond.gameObject.SetActive(state == TradeRowVisualState.Selected);

            var caret = row.Find("FocusCaret");
            if (caret != null) caret.gameObject.SetActive(state == TradeRowVisualState.Focused);
        }

        // Populates the row with item data. Pass InventoryItem.None to render the
        // empty-slot state. `disabled` applies strike-through + dim when the item
        // is depleted/broken but still occupies the slot.
        public static void SetRowItem(Transform row, InventoryItem item, bool isLeft, bool disabled)
        {
            bool empty = item.IsEmpty;

            SetActive(row, "EmptyLabel", empty);
            SetActive(row, "DashedTop", empty);
            SetActive(row, "Sigil", !empty);
            SetActive(row, "ItemName", !empty);
            SetActive(row, "Qty", !empty);

            if (empty) return;

            Color textCol = disabled
                ? new Color(ColParchment.r, ColParchment.g, ColParchment.b, 0.35f)
                : ColParchment;

            var nameT = row.Find("ItemName")?.GetComponent<TextMeshProUGUI>();
            if (nameT != null)
            {
                nameT.text = item.DisplayName;
                nameT.color = textCol;
                var style = FontStyles.Normal;
                if (disabled) style |= FontStyles.Strikethrough;
                nameT.fontStyle = style;
            }

            var qtyT = row.Find("Qty")?.GetComponent<TextMeshProUGUI>();
            if (qtyT != null)
            {
                qtyT.text = FormatQty(item);
                qtyT.color = textCol;
            }

            var sigilImg = row.Find("Sigil")?.GetComponent<Image>();
            if (sigilImg != null)
            {
                sigilImg.color = disabled
                    ? new Color(ColBrass.r, ColBrass.g, ColBrass.b, 0.4f)
                    : Color.white;
            }
        }

        // Called during Pressed / Selected to brighten text — state visuals that
        // depend on more than bg/border. Kept separate because SetRowState runs
        // often and SetRowItem runs only on data changes.
        public static void ApplyTextEmphasis(Transform row, TradeRowVisualState state)
        {
            bool emphasize = state == TradeRowVisualState.Pressed || state == TradeRowVisualState.Selected;
            var nameT = row.Find("ItemName")?.GetComponent<TextMeshProUGUI>();
            if (nameT != null && nameT.gameObject.activeSelf && (nameT.fontStyle & FontStyles.Strikethrough) == 0)
                nameT.color = emphasize ? ColParchmentHi : ColParchment;
            var qtyT = row.Find("Qty")?.GetComponent<TextMeshProUGUI>();
            if (qtyT != null && qtyT.gameObject.activeSelf)
                qtyT.color = emphasize ? ColParchmentHi : ColParchment;
        }

        // Depleted weapons and consumables still hold a slot but grey out.
        public static bool IsDisabled(InventoryItem item)
            => !item.IsEmpty && item.IsDepleted;

        static string FormatQty(InventoryItem item)
        {
            if (item.Indestructible) return "\u221E";
            return item.CurrentUses.ToString();
        }

        static Color BackgroundColor(TradeRowVisualState state) => state switch
        {
            TradeRowVisualState.Hover    => new Color(ColBrassLite.r, ColBrassLite.g, ColBrassLite.b, 0.10f),
            TradeRowVisualState.Pressed  => new Color(ColBrassLite.r, ColBrassLite.g, ColBrassLite.b, 0.22f),
            TradeRowVisualState.Focused  => new Color(ColBrassLite.r, ColBrassLite.g, ColBrassLite.b, 0.14f),
            TradeRowVisualState.Selected => new Color(ColVermillion.r, ColVermillion.g, ColVermillion.b, 0.35f),
            _ => Color.clear,
        };

        static Color BorderColor(TradeRowVisualState state) => state switch
        {
            TradeRowVisualState.Hover    => new Color(ColBrassLite.r, ColBrassLite.g, ColBrassLite.b, 0.30f),
            TradeRowVisualState.Pressed  => ColBrassLite,
            TradeRowVisualState.Focused  => ColBrassLite,
            TradeRowVisualState.Selected => ColVermillion,
            _ => Color.clear,
        };

        static void SetEdgeColor(Transform row, string name, Color color)
        {
            var img = row.Find(name)?.GetComponent<Image>();
            if (img != null) img.color = color;
        }

        static void SetActive(Transform row, string childName, bool active)
        {
            var child = row.Find(childName);
            if (child != null) child.gameObject.SetActive(active);
        }

        static Color Hex(string rgb)
        {
            if (ColorUtility.TryParseHtmlString("#" + rgb, out var c)) return c;
            return Color.magenta;
        }
    }
}
