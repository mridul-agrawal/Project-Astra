using ProjectAstra.Core.Units;

namespace ProjectAstra.Core.Cursor
{
    // Turns "what is under the cursor" into the four hover kinds the visual variants
    // express. Kept as a pure function over a faction and an acted flag so it can be
    // tested without a scene, and so the cursor never has to reason about factions itself.
    public static class CursorHoverClassifier
    {
        // A null faction means the tile is empty. Allied units read as allies: the player
        // can't command them, but they are still not a threat, and the danger tint is
        // reserved for units that actually are.
        public static CursorHover Classify(Faction? faction, bool canAct)
        {
            if (faction == null) return CursorHover.Empty;
            if (faction == Faction.Enemy) return CursorHover.Enemy;
            return canAct ? CursorHover.ReadyAlly : CursorHover.ActedAlly;
        }

        // Only a ready ally of the player's own faction can be picked up. Allied-faction
        // units hover as ReadyAlly but confirm gives error feedback, matching the existing
        // UnitSelectionFlow.IsUnitSelectable rule.
        public static bool IsSelectable(CursorHover hover, Faction? faction) =>
            hover == CursorHover.ReadyAlly && faction == Faction.Player;

        public static CursorErrorKind ErrorFor(CursorHover hover) => hover switch
        {
            CursorHover.ActedAlly => CursorErrorKind.UnitAlreadyActed,
            CursorHover.Enemy => CursorErrorKind.NotYourUnit,
            _ => CursorErrorKind.InvalidTile,
        };
    }
}
