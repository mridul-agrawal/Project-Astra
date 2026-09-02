namespace ProjectAstra.Core.Hub
{
    // Stops the interaction prompt strobing when she stands right on the edge of a target's reach,
    // where a few pixels of drift flips it in and out every frame.
    //
    // Asymmetric on purpose: a target is picked up the instant it becomes valid, so the prompt feels
    // immediate, but it is only let go after it has stayed invalid for a moment. Swapping straight
    // from one target to another is instant too — that reads as intent, not as flicker.
    public class PromptHysteresis
    {
        public const float DefaultReleaseDelay = 0.12f;

        private readonly float releaseDelay;
        private string current;
        private float invalidFor;

        public PromptHysteresis(float releaseDelay = DefaultReleaseDelay)
        {
            this.releaseDelay = releaseDelay;
        }

        public string Current => current;

        // Feed the freshly resolved target each frame — null when nothing is in reach. Returns the
        // target the prompt should actually be showing.
        public string Tick(string resolved, float deltaTime)
        {
            if (!string.IsNullOrEmpty(resolved))
            {
                current = resolved;
                invalidFor = 0f;
                return current;
            }

            if (string.IsNullOrEmpty(current)) return null;

            invalidFor += deltaTime;
            if (invalidFor >= releaseDelay) current = null;
            return current;
        }

        // Drops the prompt at once, with no grace period — for entering a conversation, changing
        // room, or anything else that should clear the screen immediately.
        public void Clear()
        {
            current = null;
            invalidFor = 0f;
        }
    }
}
