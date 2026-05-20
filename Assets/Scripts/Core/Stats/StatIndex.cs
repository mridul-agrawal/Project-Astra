namespace ProjectAstra.Core.Stats
{
    // The nine stat slots a unit has. Don't reorder — add new stats only at the end.
    // Unity stores these as integers inside every unit and class asset (StatArray),
    // so reordering silently remaps every unit's stats.
    public enum StatIndex
    {
        HP = 0,
        Str = 1,
        Mag = 2,
        Skl = 3,
        Spd = 4,
        Def = 5,
        Res = 6,
        Con = 7,
        Niyati = 8
    }
}
