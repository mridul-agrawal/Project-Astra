using System;
using System.Collections;
using UnityEngine;
using ProjectAstra.Core.Animation;
using ProjectAstra.Core.Gurukul.Conversation;

namespace ProjectAstra.Core.Gurukul.Events
{
    // Plays an authored sequence: locks control, works through the actions in order, then hands
    // control back only once every final position and state is settled.
    //
    // Movement here goes through the same GurukulMover the player uses, so a character walking a
    // route respects the same walls she does and arrives looking the same way she would.
    [DefaultExecutionOrder(40)]
    public sealed class GurukulEventRunner : MonoBehaviour
    {
        private const float StuckTimeoutSeconds = 3f;

        // A frame that moves less than a hundredth of a pixel counts as no progress at all.
        private const float MinProgressSqr = 1e-8f;

        [SerializeField] private GurukulInputRouter router;
        [SerializeField] private GurukulConversationPlayer conversations;
        [SerializeField] private GurukulEventCatalog catalog;
        [SerializeField] private GurukulCameraRig cameraRig;

        private EventQueueGuard guard;

        // Raised for each RaiseFlag action, and when an event finishes, so progression can react.
        public event Action<string> FlagRaised;
        public event Action<string> EventFinished;
        public event Action<string> DepartureRequested;

        public bool IsRunning => guard != null && guard.IsBusy;

        private void Awake()
        {
            if (router == null) router = FindFirstObjectByType<GurukulInputRouter>();
            if (conversations == null) conversations = FindFirstObjectByType<GurukulConversationPlayer>();
        }

        // Rebuilt per visit, because the guard reads the visit's completed-event list.
        public void BindToVisit() => guard = new EventQueueGuard(GurukulProgressService.Instance?.State);

        public bool TryPlay(string eventId)
        {
            GurukulEvent authored = catalog != null ? catalog.Get(eventId) : null;
            if (authored == null)
            {
                Debug.LogError($"[GurukulEvent] No event with id '{eventId}'.");
                return false;
            }
            return TryPlay(authored);
        }

        public bool TryPlay(GurukulEvent authored)
        {
            if (guard == null) BindToVisit();
            if (!guard.TryBegin(authored.EventId, authored.OneTime)) return false;
            if (!router.States.TryTransition(GurukulSubState.ScriptedEvent))
            {
                guard.Finish(authored.EventId, oneTime: false);
                return false;
            }

            StartCoroutine(Play(authored));
            return true;
        }

        private IEnumerator Play(GurukulEvent authored)
        {
            bool departed = false;

            foreach (GurukulEventAction action in authored.Actions)
            {
                if (action.kind == GurukulEventActionKind.Depart) departed = true;
                yield return Perform(action);
                if (departed) break;
            }

            guard.Finish(authored.EventId, authored.OneTime);
            EventFinished?.Invoke(authored.EventId);

            // An event that leaves for battle must not flicker back through exploration on the way.
            if (departed) yield break;
            router.States.TryTransition(GurukulSubState.FreeExploration);
        }

        private IEnumerator Perform(GurukulEventAction action)
        {
            switch (action.kind)
            {
                case GurukulEventActionKind.PlayConversation: yield return PlayConversation(action); break;
                case GurukulEventActionKind.WalkCharacter: yield return Walk(action); break;
                case GurukulEventActionKind.Wait: yield return new WaitForSeconds(action.seconds); break;
                default: Apply(action); break;
            }
        }

        // Instant actions. Everything that only rearranges state lands in one frame, so a run of
        // them is over before the next visible beat.
        private void Apply(GurukulEventAction action)
        {
            GurukulRuntimeState state = GurukulProgressService.Instance?.State;

            switch (action.kind)
            {
                case GurukulEventActionKind.SetFacing:
                    GurukulWorld.FindActor(action.targetId)?.SetFacing(action.facing);
                    break;

                case GurukulEventActionKind.RelocateCharacter:
                    state?.Relocate(action.targetId, action.valueId, action.position, action.facing);
                    RelocateHere(action);
                    break;

                case GurukulEventActionKind.SetCharacterPresent:
                    GurukulActor actor = GurukulWorld.FindActor(action.targetId);
                    if (actor != null) actor.gameObject.SetActive(action.flag);
                    break;

                case GurukulEventActionKind.SetInteractableState:
                    state?.SetInteractableState(action.targetId, action.state);
                    break;

                case GurukulEventActionKind.SetGate:
                    state?.SetGate(action.targetId, action.flag);
                    break;

                case GurukulEventActionKind.RaiseFlag:
                    FlagRaised?.Invoke(action.valueId);
                    break;

                case GurukulEventActionKind.FocusCamera:
                    FocusCamera(action);
                    break;

                case GurukulEventActionKind.Depart:
                    DepartureRequested?.Invoke(action.valueId);
                    break;
            }
        }

        // Someone relocated into a different room simply vanishes from this one — they are rebuilt
        // where they now stand next time that room loads.
        private static void RelocateHere(GurukulEventAction action)
        {
            GurukulActor actor = GurukulWorld.FindActor(action.targetId);
            if (actor == null) return;

            string here = GurukulProgressService.Instance?.State.CurrentLocationId;
            if (!string.IsNullOrEmpty(action.valueId) && action.valueId != here)
            {
                Destroy(actor.gameObject);
                return;
            }
            actor.Place(action.position, action.facing);
        }

        private void FocusCamera(GurukulEventAction action)
        {
            if (cameraRig == null) return;

            GurukulActor actor = GurukulWorld.FindActor(action.targetId);
            if (actor != null) cameraRig.Follow(actor.transform);
        }

        private IEnumerator PlayConversation(GurukulEventAction action)
        {
            if (!conversations.TryStart(action.valueId)) yield break;
            while (conversations.IsRunning) yield return null;

            // The conversation hands control back to exploration when it ends; the event still owns
            // it, so take it back before the next action runs.
            router.States.TryTransition(GurukulSubState.ScriptedEvent);
        }

        private IEnumerator Walk(GurukulEventAction action)
        {
            GurukulActor actor = GurukulWorld.FindActor(action.targetId);
            if (actor == null || action.route == null) yield break;

            // A standing character is a solid obstacle, which includes being an obstacle to itself
            // — its own footprint sits in the collision map right where it is about to step. So it
            // stops being solid while it walks and settles back to solid on arrival.
            actor.SetSolid(false);

            float speed = action.seconds > 0f ? action.seconds : GurukulMover.DefaultSpeedTilesPerSecond;
            foreach (Vector2 corner in action.route)
                yield return WalkToCorner(actor, corner, speed);

            actor.SetSolid(true);
        }

        private IEnumerator WalkToCorner(GurukulActor actor, Vector2 corner, float speed)
        {
            Vector2 lastProgress = actor.Position;
            float stalledFor = 0f;

            while (!CardinalRouteFollower.HasArrived(actor.Position, corner))
            {
                Facing? step = CardinalRouteFollower.NextStep(actor.Position, corner);
                if (step == null) break;

                actor.SetFacing(step.Value);
                Vector2 next = GurukulMover.Move(GurukulLocationService.Instance.Collision,
                    actor.Position, actor.FootprintOffset, step, Time.deltaTime, out _, speed);
                actor.SetPosition(CardinalRouteFollower.ClampToCorner(actor.Position, next, corner, step.Value));

                // Judged over time rather than per frame. A single frame that moves nobody is
                // normal — the editor hands out a zero delta whenever it isn't focused — but a
                // route the world no longer allows would hang the event and lock the player out,
                // so it gives up loudly once nothing has moved for a while.
                if ((actor.Position - lastProgress).sqrMagnitude > MinProgressSqr)
                {
                    lastProgress = actor.Position;
                    stalledFor = 0f;
                }
                else
                {
                    stalledFor += Time.deltaTime;
                    if (stalledFor > StuckTimeoutSeconds)
                    {
                        Debug.LogError($"[GurukulEvent] '{actor.CharacterId}' stopped at {actor.Position} on the way " +
                                       $"to {corner} — something is in the way.");
                        yield break;
                    }
                }
                yield return null;
            }

            actor.SetPosition(corner);
        }
    }
}
