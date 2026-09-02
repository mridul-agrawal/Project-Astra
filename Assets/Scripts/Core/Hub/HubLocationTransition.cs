using System.Collections;
using UnityEngine;
using ProjectAstra.Core.Animation;
using ProjectAstra.Core.Scenes;

namespace ProjectAstra.Core.Hub
{
    // Takes her through a doorway: lock, fade, swap the room, put her down on the other side, fade
    // back, hand control over.
    //
    // Rooms are swapped inside the one hub scene rather than loaded as scenes. SceneLoader only
    // loads in single mode, so a doorway routed through it would tear down the whole hub — and with
    // it every relocation and completed conversation the visit had accumulated.
    public sealed class HubLocationTransition : MonoBehaviour
    {
        // A doorway is shorter than a scene load, and silent — the transition whoosh belongs to
        // a change of place, not to every door in a courtyard.
        private const float DoorFadeSeconds = 0.2f;

        [SerializeField] private HubLocationLoader loader;

        public bool IsTransitioning => HubControlGate.Instance.IsHandoverInFlight;

        public bool TryUse(HubDoor door)
        {
            if (!HubControlGate.Instance.TryBeginHandover()) return false;

            StartCoroutine(Travel(door));
            return true;
        }

        private IEnumerator Travel(HubDoor door)
        {
            // Read before the swap: once the room changes, where she came from is gone.
            RememberWayBack(door);
            ResolveDestination(door, out string locationId, out Vector2 spawn, out Facing facing);

            yield return FadeThrough(() => loader.Load(locationId, spawn, facing, door.houseIdentityId));

            // Ending the handover re-arms the buttons, so the press that opened the door can't
            // immediately open the one she just arrived next to.
            HubControlGate.Instance.EndHandover();
        }

        // The one blackout the whole game uses. Without it — pressing Play on this scene alone, so
        // no boot services — the room still swaps, just without the fade.
        private static IEnumerator FadeThrough(System.Action swapRoom)
        {
            ScreenFader fader = ScreenFader.Instance;
            if (fader == null)
            {
                swapRoom();
                yield break;
            }

            yield return fader.Cover(swapRoom, DoorFadeSeconds, playSound: false);
        }

        // She stands at the door facing it, so coming back out means standing in the same place
        // facing the other way.
        private void RememberWayBack(HubDoor door)
        {
            HubRuntimeState state = HubProgressService.Instance?.State;
            if (state == null || loader.Player == null) return;
            if (door.ReturnsToPreviousRoom) return;

            state.RememberReturn(state.CurrentLocationId, loader.Player.Position,
                Cardinal.Opposite(loader.Player.Facing));
        }

        // An exit with no authored destination goes back through the door she arrived by, which is
        // what lets all six student houses share one interior and still each lead home.
        private void ResolveDestination(HubDoor door, out string locationId, out Vector2 spawn, out Facing facing)
        {
            if (!door.ReturnsToPreviousRoom)
            {
                locationId = door.targetLocationId;
                spawn = door.targetSpawn;
                facing = door.targetFacing;
                return;
            }

            HubRuntimeState state = HubProgressService.Instance?.State;
            if (state != null && state.TryGetReturn(out locationId, out spawn, out facing)) return;

            Debug.LogError($"[HubLocationTransition] Door '{door.doorId}' leads back the way she came, but nothing recorded how she got here.");
            locationId = state?.CurrentLocationId;
            spawn = loader.Player != null ? loader.Player.Position : Vector2.zero;
            facing = Facing.South;
        }
    }
}
