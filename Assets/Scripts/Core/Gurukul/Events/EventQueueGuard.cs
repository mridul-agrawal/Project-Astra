namespace ProjectAstra.Core.Gurukul.Events
{
    // Decides whether an event is allowed to start.
    //
    // Two rules, both from the spec: a one-time event never runs twice, and nothing interrupts an
    // event already in progress. Keeping them here rather than scattered through the runner means
    // both are one place and both are testable.
    public class EventQueueGuard
    {
        private readonly GurukulRuntimeState state;
        private string running;

        public EventQueueGuard(GurukulRuntimeState state)
        {
            this.state = state;
        }

        public bool IsBusy => !string.IsNullOrEmpty(running);
        public string RunningEventId => running;

        public bool CanStart(string eventId, bool oneTime)
        {
            if (string.IsNullOrEmpty(eventId)) return false;
            if (IsBusy) return false;
            return !oneTime || state == null || !state.HasCompletedEvent(eventId);
        }

        public bool TryBegin(string eventId, bool oneTime)
        {
            if (!CanStart(eventId, oneTime)) return false;
            running = eventId;
            return true;
        }

        // A one-time event is only written off once it has actually reached its end, so an event cut
        // short by a reload isn't wrongly remembered as done.
        public void Finish(string eventId, bool oneTime)
        {
            if (running != eventId) return;
            running = null;
            if (oneTime) state?.MarkEventCompleted(eventId);
        }

        public bool HasCompleted(string eventId) => state != null && state.HasCompletedEvent(eventId);
    }
}
