namespace ProjectAstra.Core.Input
{
    // Makes the interact button a discrete activation instead of a repeating one. Holding it must
    // never fire twice, and after anything consumes a press — a conversation opening, a menu
    // confirming, control coming back from an event — the button has to be let go before the world
    // will listen again.
    //
    // Polled rather than event-driven: Confirm has no release binding in the input asset, and
    // adding one would mean editing a shared asset for a rule only the hub cares about.
    public class InteractLatch
    {
        private bool armed = true;

        public bool IsArmed => armed;

        // Call once per frame with whether the button is down. True exactly once per press.
        public bool Consume(bool held)
        {
            if (!held)
            {
                armed = true;
                return false;
            }

            if (!armed) return false;
            armed = false;
            return true;
        }

        // Blocks the current press from counting. Used when control returns to exploration, so a
        // button still held from closing a dialogue box can't immediately trigger whatever the
        // player happens to be standing in front of.
        public void Suppress() => armed = false;
    }
}
