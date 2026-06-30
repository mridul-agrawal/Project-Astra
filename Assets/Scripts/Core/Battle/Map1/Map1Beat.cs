namespace ProjectAstra.Core.Battle.Map1
{
    public enum Map1Beat { Defend, Pursue, Done }

    // Pure beat state for Map 1: Defend (hold the bridge) -> Pursue (slay the champion)
    // -> Done. Event-driven rather than turn-indexed, so off-script pacing (a dawdling
    // player, an early kill) still lands in the right beat.
    public class Map1BeatMachine
    {
        public Map1Beat Current { get; private set; } = Map1Beat.Defend;

        // True when this death just opened the Pursue beat.
        public bool NotifyRaiderDied()
        {
            if (Current != Map1Beat.Defend) return false;
            Current = Map1Beat.Pursue;
            return true;
        }

        public bool NotifyBossDied()
        {
            if (Current == Map1Beat.Done) return false;
            Current = Map1Beat.Done;
            return true;
        }
    }
}
