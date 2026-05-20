namespace ProjectAstra.Core.Combat
{
    // Weapon proficiency ranks (E lowest, S highest). Stored on unit rank
    // trackers and weapon minRank fields as integers; don't reorder. Integer
    // values match WeaponRankTracker's threshold indexing.
    public enum WeaponRank
    {
        E = 0,
        D = 1,
        C = 2,
        B = 3,
        A = 4,
        S = 5
    }
}
