namespace ProjectAstra.Core.Combat.Playback
{
    // CC-07 — the moral weight of a critical hit.
    //
    //  Righteous — a heroic crit on a generic enemy. Bright white flash,
    //              triumphant timing, no extra hold.
    //  Tragic    — a crit that fells a Lord, a named commander, a player
    //              unit, or (later) a recruitable. Dim purple-grey flash,
    //              +0.30s pre-death hold, fall plays at 0.7× speed.
    public enum CritContext
    {
        Righteous,
        Tragic
    }
}
