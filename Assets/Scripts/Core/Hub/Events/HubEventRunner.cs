using System;
using System.Collections;
using UnityEngine;
using ProjectAstra.Core.Animation;
using ProjectAstra.Core.Dialogue;
using ProjectAstra.Core.State;

namespace ProjectAstra.Core.Hub.Events
{
    // Plays an authored sequence: locks control, works through the actions in order, then hands
    // control back only once every final position and state is settled.
    //
    // Movement here goes through the same HubMover the player uses, so a character walking a
    // route respects the same walls she does and arrives looking the same way she would.
    [DefaultExecutionOrder(40)]
    public sealed class HubEventRunner : MonoBehaviour
    {
        private const float StuckTimeoutSeconds = 3f;

        // A frame that moves less than a hundredth of a pixel counts as no progress at all.
        private const float MinProgressSqr = 1e-8f;

        [Tooltip("Turns a conversation id into the script to play.")]
        [SerializeField] private DialogueScriptCatalog scriptCatalog;
        [SerializeField] private HubEventDatabase eventDatabase;
        [SerializeField] private HubCameraController cameraRig;

        private EventQueueGuard guard;

        // Raised for each RaiseFlag action, and when an event finishes, so progression can react.
        public event Action<string> FlagRaised;
        public event Action<string> EventFinished;
        public event Action<string> DepartureRequested;

        public bool IsRunning => guard != null && guard.IsBusy;

        // Rebuilt per visit, because the guard reads the visit's completed-event list. An
        // objective's effect can ask for an event by name, and this is the thing that plays one.
        public void BindToVisit()
        {
            guard = new EventQueueGuard(HubProgressService.Instance?.State);

            ObjectiveSequenceRunner objectives = HubProgressService.Instance?.Objectives;
            if (objectives == null) return;

            objectives.EventRequested -= OnEventRequested;
            objectives.EventRequested += OnEventRequested;
        }

        private void OnEventRequested(string eventId) => TryPlay(eventId);

        public bool TryPlay(string eventId)
        {
            HubEventData authored = eventDatabase != null ? eventDatabase.Get(eventId) : null;
            if (authored == null)
            {
                Debug.LogError($"[HubEventData] No event with id '{eventId}'.");
                return false;
            }
            return TryPlay(authored);
        }

        public bool TryPlay(HubEventData authored)
        {
            if (guard == null) BindToVisit();
            if (!guard.TryBegin(authored.EventId, authored.OneTime)) return false;
            if (!EnterSequenceState())
            {
                guard.Finish(authored.EventId, oneTime: false);
                return false;
            }

            StartCoroutine(Play(authored));
            return true;
        }

        // A sequence plays in the world that is already loaded, so it never brings a scene of its
        // own — unlike a cutscene, which is its own place.
        private static bool EnterSequenceState()
        {
            GameStateManager states = GameStateManager.Instance;
            if (states == null) return true;
            if (states.CurrentState == GameState.ScriptedSequence) return true;
            return states.RequestTransition(GameState.ScriptedSequence, nameof(HubEventRunner));
        }

        private IEnumerator Play(HubEventData authored)
        {
            bool departed = false;

            // Borrowed, not given away. An event is free to point the camera at whoever is
            // speaking; the player must not be left following them once it ends.
            Transform cameraTargetBefore = cameraRig != null ? cameraRig.Target : null;

            foreach (HubEventAction action in authored.Actions)
            {
                yield return Perform(action);

                // Judged on whether the game actually left, not on having asked. A departure the
                // gate refuses — an objective still unfinished — has to fall through to the
                // hand-back below, or the sequence ends with nothing on screen and no input.
                if (action.kind != HubEventActionKind.Depart) continue;
                if (!HasLeftTheHub()) continue;
                departed = true;
                break;
            }

            guard.Finish(authored.EventId, authored.OneTime);
            EventFinished?.Invoke(authored.EventId);

            // An event that leaves for battle must not flicker back through exploration on the way,
            // and has no camera to hand back — the hub is going away.
            if (departed) yield break;

            RestoreCameraTarget(cameraTargetBefore);
            GameStateManager.Instance?.RequestTransition(GameState.HubExploration, nameof(HubEventRunner));
        }

        private static bool HasLeftTheHub() =>
            GameStateManager.Instance != null &&
            GameStateManager.Instance.CurrentState != GameState.ScriptedSequence;

        private IEnumerator Perform(HubEventAction action)
        {
            switch (action.kind)
            {
                case HubEventActionKind.PlayConversation: yield return PlayConversation(action); break;
                case HubEventActionKind.WalkCharacter: yield return Walk(action); break;
                case HubEventActionKind.Wait: yield return new WaitForSeconds(action.seconds); break;
                default: Apply(action); break;
            }
        }

        // Instant actions. Everything that only rearranges state lands in one frame, so a run of
        // them is over before the next visible beat.
        private void Apply(HubEventAction action)
        {
            HubRuntimeState state = HubProgressService.Instance?.State;

            switch (action.kind)
            {
                case HubEventActionKind.SetFacing:
                    HubWorld.FindActor(action.targetId)?.SetFacing(action.facing);
                    break;

                case HubEventActionKind.RelocateCharacter:
                    state?.Relocate(action.targetId, action.valueId, action.position, action.facing);
                    RelocateHere(action);
                    break;

                case HubEventActionKind.SetCharacterPresent:
                    HubActor actor = HubWorld.FindActor(action.targetId);
                    if (actor != null) actor.gameObject.SetActive(action.flag);
                    break;

                case HubEventActionKind.SetInteractableState:
                    state?.SetInteractableState(action.targetId, action.state);
                    break;

                case HubEventActionKind.SetGate:
                    state?.SetGate(action.targetId, action.flag);
                    break;

                case HubEventActionKind.RaiseFlag:
                    FlagRaised?.Invoke(action.valueId);
                    break;

                case HubEventActionKind.FocusCamera:
                    FocusCamera(action);
                    break;

                case HubEventActionKind.Depart:
                    DepartureRequested?.Invoke(action.valueId);
                    break;
            }
        }

        // Someone relocated into a different room simply vanishes from this one — they are rebuilt
        // where they now stand next time that room loads.
        private static void RelocateHere(HubEventAction action)
        {
            HubActor actor = HubWorld.FindActor(action.targetId);
            if (actor == null) return;

            string here = HubProgressService.Instance?.State.CurrentLocationId;
            if (!string.IsNullOrEmpty(action.valueId) && action.valueId != here)
            {
                Destroy(actor.gameObject);
                return;
            }
            actor.Place(action.position, action.facing);
        }

        private void FocusCamera(HubEventAction action)
        {
            if (cameraRig == null) return;

            HubActor actor = HubWorld.FindActor(action.targetId);
            if (actor != null) cameraRig.Follow(actor.transform);
        }

        // Put back whoever the camera was on before the sequence borrowed it. A target that has
        // since been destroyed — a character the event relocated out of the room — is left alone
        // rather than followed into nothing.
        private void RestoreCameraTarget(Transform previous)
        {
            if (cameraRig == null || previous == null) return;
            cameraRig.Follow(previous);
        }

        // The sequence keeps hold of the moment while a conversation plays inside it: the service
        // takes Dialogue and, when the script ends, hands the state back here.
        private IEnumerator PlayConversation(HubEventAction action)
        {
            DialogueScript script = scriptCatalog != null ? scriptCatalog.Get(action.valueId) : null;
            if (script == null)
            {
                Debug.LogError($"[HubEventData] No dialogue script with id '{action.valueId}'. " +
                               "Add it to the Dialogue Script Catalog.");
                yield break;
            }

            bool finished = false;
            DialogueService.Instance.Play(script, DialogueTriggeringContext.Conversation,
                () => finished = true, HubProgressService.Instance?.State, RaiseFlag);

            while (!finished) yield return null;
        }

        private void RaiseFlag(string flagId) => FlagRaised?.Invoke(flagId);

        private IEnumerator Walk(HubEventAction action)
        {
            HubActor actor = HubWorld.FindActor(action.targetId);
            if (actor == null || action.route == null) yield break;

            // A standing character is a solid obstacle, which includes being an obstacle to itself
            // — its own footprint sits in the collision map right where it is about to step. So it
            // stops being solid while it walks and settles back to solid on arrival.
            actor.SetSolid(false);

            float speed = action.seconds > 0f ? action.seconds : HubMover.DefaultSpeedTilesPerSecond;
            foreach (Vector2 corner in action.route)
                yield return WalkToCorner(actor, corner, speed);

            actor.SetSolid(true);
        }

        private IEnumerator WalkToCorner(HubActor actor, Vector2 corner, float speed)
        {
            Vector2 lastProgress = actor.Position;
            float stalledFor = 0f;

            while (!CardinalRouteFollower.HasArrived(actor.Position, corner))
            {
                Facing? step = CardinalRouteFollower.NextStep(actor.Position, corner);
                if (step == null) break;

                actor.SetFacing(step.Value);
                Vector2 next = HubMover.Move(HubLocationService.Instance.Collision,
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
                        Debug.LogError($"[HubEventData] '{actor.CharacterId}' stopped at {actor.Position} on the way " +
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
