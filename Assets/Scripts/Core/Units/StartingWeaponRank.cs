using System;
using ProjectAstra.Core.Combat;

namespace ProjectAstra.Core.Units
{
    // Authored starting proficiency for one weapon type. NOTE: not yet consumed at runtime —
    // seeding these into the unit's WeaponRankTracker on spawn is a future step (see UnitSpawner).
    [Serializable]
    public struct StartingWeaponRank
    {
        public WeaponType weaponType;
        public WeaponRank rank;
    }
}
