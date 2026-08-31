using UnityEngine;

namespace ProjectAstra.Core.Gurukul.Events
{
    // Watches for her walking into a patch of ground an event is waiting on.
    //
    // Checked only during free exploration, so an event can't set itself off while another one is
    // already moving her through the same spot.
    [DefaultExecutionOrder(45)]
    public sealed class GurukulAreaTriggerWatcher : MonoBehaviour
    {
        [SerializeField] private GurukulInputRouter router;
        [SerializeField] private GurukulEventRunner events;
        [SerializeField] private GurukulEventDatabase eventDatabase;
        [SerializeField] private GurukulLocationLoader loader;

        private void Update()
        {
            if (!CanTrigger()) return;

            Vector2 here = loader.Player.Position;
            string room = GurukulProgressService.Instance?.State.CurrentLocationId;

            foreach (GurukulEventData authored in eventDatabase.All)
            {
                if (!IsWaitingHere(authored, room, here)) continue;
                if (events.TryPlay(authored)) return;
            }
        }

        private bool CanTrigger() =>
            eventDatabase != null && loader != null && loader.Player != null &&
            router != null && router.Gate.AcceptsMovement && !events.IsRunning;

        private static bool IsWaitingHere(GurukulEventData authored, string room, Vector2 position) =>
            authored != null &&
            authored.Trigger == GurukulEventTrigger.AreaEntered &&
            authored.TriggerLocationId == room &&
            authored.TriggerArea.Contains(position);
    }
}
