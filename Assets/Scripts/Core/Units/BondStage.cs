namespace ProjectAstra.Core.Units
{
    // Support bond stages (SP-01). Stored on save data as the integer value —
    // don't reorder; add new entries only at the end. Values intentionally
    // match SupportBond.BondLevel so casts round-trip.
    public enum BondStage
    {
        Encounter = 0,
        Saathi    = 1,
        Vishwas   = 2,
        Bandhan   = 3,
    }
}
