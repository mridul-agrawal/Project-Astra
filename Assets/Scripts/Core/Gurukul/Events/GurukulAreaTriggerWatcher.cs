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
        [SerializeField] private GurukulEventCatalog catalog;
        [SerializeField] private GurukulLocationLoader loader;

        private void Update()
        {
            if (!CanTrigger()) return;

            Vector2 here = loader.Player.Position;
            string room = GurukulProgressService.Instance?.State.CurrentLocationId;

            foreach (GurukulEvent authored in catalog.All)
            {
                if (!IsWaitingHere(authored, room, here)) continue;
                if (events.TryPlay(authored)) return;
            }
        }

        private bool CanTrigger() =>
            catalog != null && loader != null && loader.Player != null &&
            router != null && router.States.AcceptsMovement && !events.IsRunning;

        private static bool IsWaitingHere(GurukulEvent authored, string room, Vector2 position) =>
            authored != null &&
            authored.Trigger == GurukulEventTrigger.AreaEntered &&
            authored.TriggerLocationId == room &&
            authored.TriggerArea.Contains(position);
    }
}
