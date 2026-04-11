using System.Collections.Generic;
using UnityEngine;

namespace ProjectAstra.Core
{
    public static class StaffRangeResolver
    {
        public static int GetEffectiveMinRange(WeaponData staff)
        {
            if (staff.staffEffect == StaffEffect.AreaOfEffect)
                return 0;
            return staff.minRange;
        }

        public static int GetEffectiveMaxRange(WeaponData staff, int magStat)
        {
            switch (staff.staffEffect)
            {
                case StaffEffect.Ranged:
                case StaffEffect.AreaOfEffect:
                    return Mathf.Max(1, magStat / 2);
                default:
                    return staff.maxRange;
            }
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
