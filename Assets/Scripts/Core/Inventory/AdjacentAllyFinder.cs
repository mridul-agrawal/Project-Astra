using System;
using System.Collections.Generic;
using UnityEngine;

namespace ProjectAstra.Core
{
    /// <summary>
    /// Finds player-faction units orthogonally adjacent (Manhattan distance 1) to a position.
    /// Accepts a unit lookup delegate so it stays testable without scene dependencies.
    /// </summary>
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
