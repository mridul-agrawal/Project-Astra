namespace ProjectAstra.Core.Combat
{
    // What a staff does on use — picks the heal/ranged/AoE flow at the action
    // menu and combat layer. Stored on weapon assets as the integer value;
    // don't reorder.
    public enum StaffEffect
    {
        None,
        Heal,
        FullHeal,
        Ranged,
        AreaOfEffect,
    }
}
