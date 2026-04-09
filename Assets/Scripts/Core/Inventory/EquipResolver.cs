namespace ProjectAstra.Core
{
    /// <summary>
    /// Decides whether a given unit may equip a given weapon. Walks a fallback chain so
    /// units with full data (UnitInstance + WeaponRankTracker) get the proper rank-aware
    /// check while bare TestUnits stay usable in tests and editor scenes.
    /// </summary>
    public static class EquipResolver
    {
        public static bool CanEquip(TestUnit unit, WeaponData weapon)
        {
            if (unit == null) return false;
            if (weapon.IsEmpty || weapon.IsBroken) return false;

            if (weapon.characterLocked)
            {
                var unitId = unit.UnitInstance?.Definition?.UnitId;
                if (unitId != weapon.ownerUnitId) return false;
            }

            if (unit.WeaponRankTracker != null)
                return unit.WeaponRankTracker.CanEquip(weapon);

            var classDef = unit.UnitInstance?.CurrentClass;
            if (classDef != null && classDef.WeaponWhitelist != null && classDef.WeaponWhitelist.Length > 0)
                return ContainsType(classDef.WeaponWhitelist, weapon.weaponType);

            var fallback = unit.AllowedWeaponTypes;
            if (fallback != null && fallback.Length > 0)
                return ContainsType(fallback, weapon.weaponType);

            return true;
        }

        private static bool ContainsType(WeaponType[] list, WeaponType target)
        {
            for (int i = 0; i < list.Length; i++)
                if (list[i] == target) return true;
            return false;
        }
    }
}
