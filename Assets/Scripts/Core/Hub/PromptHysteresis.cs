namespace ProjectAstra.Core.Hub
{
    // Stops the prompt strobing when she stands right on the edge of a target's reach.
    public class PromptHysteresis<T> where T : class
    {
        public const float DefaultReleaseDelay = 0.12f;

        private readonly float releaseDelay;
        private T current;
        private float invalidFor;

        public PromptHysteresis(float releaseDelay = DefaultReleaseDelay)
        {
            this.releaseDelay = releaseDelay;
        }

        public T Current => current;

        // Feed the freshly resolved target each frame — null when nothing is in reach. Returns the
        // target the prompt should actually be showing.
        public T Tick(T resolved, float deltaTime)
        {
            if (resolved != null)
            {
                current = resolved;
                invalidFor = 0f;
                return current;
            }

            if (current == null) return null;

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
