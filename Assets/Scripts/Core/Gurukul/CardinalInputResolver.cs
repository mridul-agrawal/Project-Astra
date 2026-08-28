using System.Collections.Generic;
using ProjectAstra.Core.Animation;

namespace ProjectAstra.Core.Gurukul
{
    // Turns four held direction buttons into the single direction the protagonist should walk.
    // Fed the raw held state every frame rather than subscribing to anything, so it is pure C# and
    // the whole rule set unit-tests without an input device.
    //
    // The rules, all from the movement spec:
    //   - one cardinal axis at a time; never a diagonal
    //   - when two perpendicular directions are held, the one pressed most recently wins
    //   - when both are first pressed on the same update, horizontal wins
    //   - when both directions on one axis are held, that axis goes neutral until one is released
    //   - releasing the active direction resumes the other held one at once, with no new press
    public class CardinalInputResolver
    {
        // Most recently pressed last, so resolving walks it backwards.
        private readonly List<Facing> pressOrder = new(4);

        private bool northHeld, southHeld, eastHeld, westHeld;

        public Facing? Resolve(bool north, bool south, bool east, bool west)
        {
            UpdatePressOrder(north, south, east, west);
            return MostRecentUsableDirection();
        }

        // Drops any movement in progress and forgets what was held — for losing focus, a controller
        // unplugging, or control being taken away mid-stride.
        public void Clear()
        {
            pressOrder.Clear();
            northHeld = southHeld = eastHeld = westHeld = false;
        }

        private void UpdatePressOrder(bool north, bool south, bool east, bool west)
        {
            Release(Facing.North, northHeld, north);
            Release(Facing.South, southHeld, south);
            Release(Facing.East, eastHeld, east);
            Release(Facing.West, westHeld, west);

            // Vertical first so that when a horizontal and a vertical are pressed on the same
            // update, the horizontal lands later in the order and therefore wins.
            Press(Facing.North, northHeld, north);
            Press(Facing.South, southHeld, south);
            Press(Facing.East, eastHeld, east);
            Press(Facing.West, westHeld, west);

            northHeld = north;
            southHeld = south;
            eastHeld = east;
            westHeld = west;
        }

        private void Release(Facing facing, bool wasHeld, bool isHeld)
        {
            if (wasHeld && !isHeld) pressOrder.Remove(facing);
        }

        private void Press(Facing facing, bool wasHeld, bool isHeld)
        {
            if (!wasHeld && isHeld) pressOrder.Add(facing);
        }

        private Facing? MostRecentUsableDirection()
        {
            bool horizontalNeutral = eastHeld && westHeld;
            bool verticalNeutral = northHeld && southHeld;

            for (int i = pressOrder.Count - 1; i >= 0; i--)
            {
                Facing candidate = pressOrder[i];
                bool axisNeutral = Cardinal.IsHorizontal(candidate) ? horizontalNeutral : verticalNeutral;
                if (!axisNeutral) return candidate;
            }
            return null;
        }
    }
}
