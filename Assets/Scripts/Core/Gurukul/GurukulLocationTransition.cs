using System.Collections;
using UnityEngine;
using ProjectAstra.Core.Animation;

namespace ProjectAstra.Core.Gurukul
{
    // Takes her through a doorway: lock, fade, swap the room, put her down on the other side, fade
    // back, hand control over.
    //
    // Rooms are swapped inside the one hub scene rather than loaded as scenes. SceneLoader only
    // loads in single mode, so a doorway routed through it would tear down the whole hub — and with
    // it every relocation and completed conversation the visit had accumulated.
    public sealed class GurukulLocationTransition : MonoBehaviour
    {
        [SerializeField] private GurukulInputRouter router;
        [SerializeField] private GurukulLocationLoader loader;
        [SerializeField] private GurukulScreenFade fade;

        public bool IsTransitioning => router.States.IsHandoverInFlight;

        public bool TryUse(GurukulDoor door)
        {
            if (!router.States.TryBeginHandover()) return false;

            StartCoroutine(Travel(door));
            return true;
        }

        private IEnumerator Travel(GurukulDoor door)
        {
            // Read before the swap: once the room changes, where she came from is gone.
            RememberWayBack(door);
            ResolveDestination(door, out string locationId, out Vector2 spawn, out Facing facing);

            yield return fade.Cover(() => loader.Load(locationId, spawn, facing, door.houseIdentityId));

            // Ending the handover re-arms the buttons, so the press that opened the door can't
            // immediately open the one she just arrived next to.
            router.States.EndHandover();
        }

        // She stands at the door facing it, so coming back out means standing in the same place
        // facing the other way.
        private void RememberWayBack(GurukulDoor door)
        {
            GurukulRuntimeState state = GurukulProgressService.Instance?.State;
            if (state == null || loader.Player == null) return;
            if (door.ReturnsToPreviousRoom) return;

            state.RememberReturn(state.CurrentLocationId, loader.Player.Position,
                Cardinal.Opposite(loader.Player.Facing));
        }

        // An exit with no authored destination goes back through the door she arrived by, which is
        // what lets all six student houses share one interior and still each lead home.
        private void ResolveDestination(GurukulDoor door, out string locationId, out Vector2 spawn, out Facing facing)
        {
            if (!door.ReturnsToPreviousRoom)
            {
                locationId = door.targetLocationId;
                spawn = door.targetSpawn;
                facing = door.targetFacing;
                return;
            }

            GurukulRuntimeState state = GurukulProgressService.Instance?.State;
            if (state != null && state.TryGetReturn(out locationId, out spawn, out facing)) return;

            Debug.LogError($"[GurukulLocationTransition] Door '{door.doorId}' leads back the way she came, but nothing recorded how she got here.");
            locationId = state?.CurrentLocationId;
            spawn = loader.Player != null ? loader.Player.Position : Vector2.zero;
            facing = Facing.South;
        }
    }
}
