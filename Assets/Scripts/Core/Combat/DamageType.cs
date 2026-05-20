namespace ProjectAstra.Core.Combat
{
    // Damage flavor — picks Str/Def vs Mag/Res in the damage formula. Stored
    // on weapon assets as the integer value; don't reorder.
    public enum DamageType
    {
        Physical,
        Magical
    }
}
