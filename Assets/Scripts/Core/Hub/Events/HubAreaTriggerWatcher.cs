using UnityEngine;

namespace ProjectAstra.Core.Hub.Events
{
    // Watches for her walking into a patch of ground an event is waiting on.
    [DefaultExecutionOrder(45)]
    public sealed class HubAreaTriggerWatcher : MonoBehaviour
    {
        [SerializeField] private HubEventRunner events;
        [SerializeField] private HubEventDatabase eventDatabase;
        [SerializeField] private HubLocationLoader loader;

        private void Update()
        {
            if (!CanTrigger()) return;

            Vector2 here = loader.Player.Position;
            string room = HubProgressService.Instance?.State.CurrentLocationId;

            foreach (HubEventData authored in eventDatabase.All)
            {
                if (!IsWaitingHere(authored, room, here)) continue;
                if (events.TryPlay(authored)) return;
            }
        }

        private bool CanTrigger() =>
            eventDatabase != null && loader != null && loader.Player != null &&
            HubControlGate.Instance != null && HubControlGate.Instance.AcceptsMovement && !events.IsRunning;

        private static bool IsWaitingHere(HubEventData authored, string room, Vector2 position) =>
            authored != null &&
            authored.Trigger == HubEventTrigger.AreaEntered &&
            authored.TriggerLocationId == room &&
            authored.TriggerArea.Contains(position);
    }
}
