using System;
using System.Collections.Generic;
using UnityEngine;
using ProjectAstra.Core.Turn;
using ProjectAstra.Core.Units;

namespace ProjectAstra.Core
{
    // Finds units of a given faction that sit orthogonally adjacent
    // (Manhattan distance 1) to a position. The unit lookup is passed in as
    // a delegate so this stays testable without scene dependencies.
    public static class AdjacentAllyFinder
    {
        private static readonly Vector2Int[] CardinalOffsets =
        {
            new(1, 0), new(-1, 0), new(0, 1), new(0, -1)
        };

        public static List<TestUnit> FindAdjacentAllies(
            Vector2Int position,
            Faction faction,
            TestUnit exclude,
            Func<Vector2Int, TestUnit> unitLookup)
        {
            var result = new List<TestUnit>();
            foreach (var offset in CardinalOffsets)
            {
                var unit = unitLookup(position + offset);
                if (unit == null || unit == exclude) continue;
                if (!unit.gameObject.activeInHierarchy) continue;

                Faction? unitFaction = TurnManager.Instance != null
                    ? TurnManager.Instance.UnitRegistry.GetFaction(unit)
                    : unit.faction;

                if (unitFaction == faction)
                    result.Add(unit);
            }
            return result;
        }
    }
}
