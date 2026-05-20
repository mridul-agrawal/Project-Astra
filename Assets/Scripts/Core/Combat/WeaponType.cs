namespace ProjectAstra.Core.Combat
{
    // Weapon kind — drives the physical/magic triangle and class
    // equippability. Stored on weapon assets and class whitelists as the
    // integer value; don't reorder.
    public enum WeaponType
    {
        Sword,
        Lance,
        Axe,
        Bow,
        AnimaTome,
        LightTome,
        DarkTome,
        Staff
    }
}
