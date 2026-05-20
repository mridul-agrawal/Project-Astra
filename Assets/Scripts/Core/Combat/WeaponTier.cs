namespace ProjectAstra.Core.Combat
{
    // Material tier of a weapon (Iron → Steel → Silver → Killer). Cosmetic
    // grouping today; persisted on weapon assets as integers, so don't reorder.
    public enum WeaponTier
    {
        Iron,
        Steel,
        Silver,
        Killer
    }
}
