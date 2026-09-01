using UnityEngine;
using ProjectAstra.Core.Flow;
using ProjectAstra.Core.Gurukul.Events;

using ProjectAstra.Core.Dialogue;

namespace ProjectAstra.Core.Gurukul
{
    // Brings up the visit the campaign is on: loads its progression, builds its opening room, and
    // puts the cast where the visit says they stand. Runs early so everything exists before the
    // camera and the input router look for it.
    //
    // Same shape as MapBootstrapper, including the fallback for pressing Play straight into this
    // scene. The room itself is built by GurukulLocationLoader, which every doorway uses too, so
    // arriving at a room always means the same thing.
    [DefaultExecutionOrder(-100)]
    public sealed class GurukulBootstrapper : MonoBehaviour
    {
        [Header("Scene")]
        [SerializeField] private GurukulLocationLoader loader;
        [SerializeField] private GurukulCameraRig cameraRig;
        [SerializeField] private GurukulInputRouter router;
        [SerializeField] private GurukulInteractionDriver interactionDriver;
        [SerializeField] private GurukulEventRunner events;
        [SerializeField] private GurukulVisitDirector director;
        [SerializeField] private Transform playerRoot;

        [Header("Data")]
        [Tooltip("The character the player walks around as. Constant across every visit.")]
        [SerializeField] private string protagonistUnitId = "aranya";

        [Tooltip("Fallback visit for pressing Play directly in this scene, when the campaign isn't running.")]
        [SerializeField] private GurukulVisitData fallbackVisit;

        private void Start()
        {
            GurukulWorld.Clear();

            GurukulVisitData visit = ResolveVisit();
            if (visit == null)
            {
                Debug.LogError("[GurukulBootstrapper] No visit to load — assign a fallback visit or start from the campaign.");
                return;
            }

            GurukulProgressService.Load(visit);
            OpenVisit(visit);
        }

        // The campaign's visit wins, starting the campaign at its first hub step if something
        // skipped straight here. The serialized fallback is only for a scene with no GameFlow alive.
        private GurukulVisitData ResolveVisit()
        {
            GameFlow flow = GameFlow.Instance;
            GurukulVisitData campaignVisit = flow != null ? flow.EnsureHubStepStarted() : null;
            return campaignVisit != null ? campaignVisit : fallbackVisit;
        }

        private void OpenVisit(GurukulVisitData visit)
        {
            // She is built before the room so the loader has someone to place, and left non-solid —
            // the only thing her footprint could collide with is itself.
            GurukulActor player = loader.CreatePlayer(protagonistUnitId, visit.PlayerSpawn, visit.PlayerFacing, playerRoot);
            if (player == null) return;

            player.gameObject.AddComponent<GurukulPlayerController>();
            if (cameraRig != null) cameraRig.Follow(player.transform);
            if (interactionDriver != null) interactionDriver.Bind(router, player);

            if (!loader.Load(visit.StartLocationId, visit.PlayerSpawn, visit.PlayerFacing, houseIdentity: null)) return;

            events.BindToVisit();
            if (director != null) director.BindVisitMemory(GurukulProgressService.Instance.State);
            GurukulProgressService.Instance.Objectives.EventRequested += director.PlayEvent;
            GurukulProgressService.Instance.Objectives.Begin();

            // Runs before she is given control, so a visit can open mid-scene rather than on a
            // player standing still waiting for something to happen.
            if (!string.IsNullOrEmpty(visit.OpeningEventId)) events.TryPlay(visit.OpeningEventId);
        }
    }
}
