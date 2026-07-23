using UnityEngine;

namespace ProjectAstra.Core.UI.BattleMap.HUD
{
    // The four screen corners a HUD panel can dock to. Shared by the panels that
    // reposition themselves as the cursor moves (FE GBA "musical chairs").
    public enum HudCorner { TopLeft, TopRight, BottomLeft, BottomRight }

    // Docks a RectTransform to a screen corner: anchor and pivot on that corner, then
    // an inward pad. Generalizes the left/right flip TileInfoView.ApplySide did by hand.
    public static class HudCornerLayout
    {
        public static void Apply(RectTransform rect, HudCorner corner, float pad)
        {
            if (rect == null) return;
            Vector2 anchor = AnchorFor(corner);
            rect.anchorMin = rect.anchorMax = rect.pivot = anchor;
            rect.anchoredPosition = OffsetFor(corner, pad);
        }

        private static Vector2 AnchorFor(HudCorner corner)
        {
            switch (corner)
            {
                case HudCorner.TopLeft:    return new Vector2(0, 1);
                case HudCorner.TopRight:   return new Vector2(1, 1);
                case HudCorner.BottomLeft: return new Vector2(0, 0);
                default:                   return new Vector2(1, 0); // BottomRight
            }
        }

        // Push inward from the docked corner by pad on both axes.
        private static Vector2 OffsetFor(HudCorner corner, float pad)
        {
            bool onLeft = corner == HudCorner.TopLeft || corner == HudCorner.BottomLeft;
            bool onBottom = corner == HudCorner.BottomLeft || corner == HudCorner.BottomRight;
            return new Vector2(onLeft ? pad : -pad, onBottom ? pad : -pad);
        }
    }
}
