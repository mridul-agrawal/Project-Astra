using System.Collections.Generic;
using UnityEngine;
using ProjectAstra.Core.Pathfinding;

namespace ProjectAstra.Core.Combat
{
    // Staff-specific range rules. Most staves use the literal min/max from
    // their WeaponData; Ranged and AreaOfEffect staves scale with the user's
    // Magic stat (max range = Mag / 2, min one tile).
    public static class StaffRangeResolver
    {
        public static int GetEffectiveMinRange(WeaponData staff)
        {
            return staff.staffEffect == StaffEffect.AreaOfEffect ? 0 : staff.minRange;
        }

        public static int GetEffectiveMaxRange(WeaponData staff, int magStat)
        {
            return staff.staffEffect switch
            {
                StaffEffect.Ranged or StaffEffect.AreaOfEffect => Mathf.Max(1, magStat / 2),
                _ => staff.maxRange,
            };
        }

        public static bool IsInRange(WeaponData staff, int magStat, Vector2Int from, Vector2Int to)
        {
            int dist = Mathf.Abs(from.x - to.x) + Mathf.Abs(from.y - to.y);
            int min = GetEffectiveMinRange(staff);
            int max = GetEffectiveMaxRange(staff, magStat);
            return dist >= min && dist <= max;
        }

        public static void GetTargetTiles(
            WeaponData staff, int magStat, Vector2Int center,
            int mapWidth, int mapHeight, HashSet<Vector2Int> result)
        {
            int min = GetEffectiveMinRange(staff);
            int max = GetEffectiveMaxRange(staff, magStat);
            Pathfinder.AddTilesInRange(center, min, max, mapWidth, mapHeight, result);
        }
    }
}
