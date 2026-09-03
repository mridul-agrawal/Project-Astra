namespace ProjectAstra.Core.Hub.Events
{
    // Decides whether an event may start: never twice if it is one-time, never over a running one.
    public class EventQueueGuard
    {
        private readonly HubEventLedger ledger;
        private string running;

        public EventQueueGuard(HubEventLedger ledger)
        {
            this.ledger = ledger;
        }

        public bool IsBusy => !string.IsNullOrEmpty(running);
        public string RunningEventId => running;

        public bool CanStart(string eventId, bool oneTime)
        {
            if (string.IsNullOrEmpty(eventId)) return false;
            if (IsBusy) return false;
            return !oneTime || ledger == null || !ledger.HasCompletedEvent(eventId);
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
            if (oneTime) ledger?.MarkEventCompleted(eventId);
        }

        public bool HasCompleted(string eventId) => ledger != null && ledger.HasCompletedEvent(eventId);
    }
}
