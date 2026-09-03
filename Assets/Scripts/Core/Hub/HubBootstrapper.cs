using UnityEngine;
using ProjectAstra.Core.Flow;
using ProjectAstra.Core.Hub.Events;
using ProjectAstra.Core.Hub.Interaction;
using ProjectAstra.Core.Dialogue;
using ProjectAstra.Core.Quests;

namespace ProjectAstra.Core.Hub
{
    // Brings up the visit the campaign is on: its progression, its opening room, and its cast.
    [DefaultExecutionOrder(-100)]
    public sealed class HubBootstrapper : MonoBehaviour
    {
        [Header("Scene")]
        [SerializeField] private HubLocationLoader loader;
        [SerializeField] private HubCameraController cameraRig;
        [SerializeField] private HubEventRunner events;
        [SerializeField] private Transform playerRoot;

        [Header("Data")]
        [Tooltip("The character the player walks around as. Constant across every visit.")]
        [SerializeField] private string protagonistUnitId = "aranya";

        [Tooltip("Fallback visit for pressing Play directly in this scene, when the campaign isn't running.")]
        [SerializeField] private HubVisitData fallbackVisit;

        [Tooltip("Turns a conversation id into a script, for interactables the loader builds at runtime.")]
        [SerializeField] private DialogueScriptCatalog scriptCatalog;

        private void Start()
        {
            HubWorld.Clear();
            HubControlGate.Begin();
            HubInteractionCatalog.Bind(scriptCatalog);

            HubLaunchRequest.Request asked = HubLaunchRequest.Take();
            HubVisitData visit = ResolveVisit(asked);
            if (visit == null)
            {
                Debug.LogError("[HubBootstrapper] No visit to load — assign a fallback visit or start from the campaign.");
                return;
            }

            HubVisitService.Load(visit);
            OpenVisit(visit, asked);
        }

        // Someone testing a particular visit wins over everything, then the campaign, starting it
        // at its first hub step if something skipped straight here. The serialized fallback is only
        // for a scene with no GameFlow alive.
        private HubVisitData ResolveVisit(HubLaunchRequest.Request asked)
        {
            GameFlow flow = GameFlow.Instance;

            if (asked.IsSomething)
            {
                HubVisitData wanted = flow != null ? flow.VisitNamed(asked.VisitId) : null;
                if (wanted != null) return wanted;

                Debug.LogWarning($"[HubBootstrapper] Asked to start '{asked.VisitId}', which no visit is.");
            }

            HubVisitData campaignVisit = flow != null ? flow.EnsureHubStepStarted() : null;
            return campaignVisit != null ? campaignVisit : fallbackVisit;
        }

        private void OpenVisit(HubVisitData visit, HubLaunchRequest.Request asked)
        {
            // She is built before the room so the loader has someone to place, and left non-solid —
            // the only thing her footprint could collide with is itself.
            Vector2 spawn = asked.HasSpawn ? asked.Spawn : visit.PlayerSpawn;

            HubActor player = loader.CreatePlayer(protagonistUnitId, spawn, visit.PlayerFacing, playerRoot);
            if (player == null) return;

            player.gameObject.AddComponent<HubPlayerController>();
            if (cameraRig != null) cameraRig.Follow(player.transform);

            if (!loader.Load(visit.StartLocationId, spawn, visit.PlayerFacing, houseIdentity: null)) return;

            events.BindToVisit();
            QuestManager.Instance?.BeginQuestAt(visit.QuestId, asked.Stage);

            // Runs before she is given control, so a visit can open mid-scene rather than on a
            // player standing still waiting for something to happen.
            if (!string.IsNullOrEmpty(visit.OpeningEventId)) events.TryPlay(visit.OpeningEventId);
        }
    }
}
