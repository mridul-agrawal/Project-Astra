using System;
using UnityEngine;
using ProjectAstra.Core.Animation;

namespace ProjectAstra.Core.Gurukul.Events
{
    // What sets an event off. Append only.
    public enum GurukulEventTrigger
    {
        // Runs as the visit opens, before she is given control.
        VisitLoad,

        // Runs when she walks into an authored patch of ground.
        AreaEntered,

        // Fired by an objective's effect or another event.
        Called
    }

    // The things an event can do. Append only.
    //
    // Between them these cover the spec's action list: locking control and hiding prompts are
    // handled by the event state itself rather than being actions, and an objective update is a
    // raised flag, since that is how the objective system already listens.
    public enum GurukulEventActionKind
    {
        PlayConversation,
        SetFacing,
        WalkCharacter,
        RelocateCharacter,
        SetCharacterPresent,
        SetInteractableState,
        SetGate,
        RaiseFlag,
        FocusCamera,
        Wait,
        Depart
    }

    [Serializable]
    public class GurukulEventAction
    {
        public GurukulEventActionKind kind;

        [Tooltip("Who or what this acts on: a character's unitId, or an interactable's id.")]
        public string targetId;

        [Tooltip("A conversation id, a flag id, a gate name, or a location id, depending on the kind.")]
        public string valueId;

        public Vector2 position;
        public Facing facing;

        [Tooltip("WalkCharacter: the corners to walk, in order. Each leg is horizontal then vertical — no diagonals.")]
        public Vector2[] route = Array.Empty<Vector2>();

        [Tooltip("Wait: seconds. WalkCharacter: tiles per second, or 0 for the normal walking speed.")]
        public float seconds;

        public bool flag;
        public GurukulInteractableState state;
    }

    // An authored sequence: characters walking, lines being spoken, the world being rearranged.
    //
    // Its own asset rather than living inside a visit, because the same event is often wanted in
    // more than one and a visit that owned them inline would be unmergeable.
    [CreateAssetMenu(fileName = "GurukulEvent", menuName = "Project Astra/Gurukul/Event")]
    public class GurukulEvent : ScriptableObject
    {
        [SerializeField] private string eventId;
        [SerializeField] private GurukulEventTrigger trigger = GurukulEventTrigger.Called;

        [Tooltip("AreaEntered: the patch of ground that sets it off, in tiles.")]
        [SerializeField] private Rect triggerArea;

        [Tooltip("AreaEntered: which room the patch is in.")]
        [SerializeField] private string triggerLocationId;

        [Tooltip("Off for something that can happen again — a repeatable barks. On for anything with consequences.")]
        [SerializeField] private bool oneTime = true;

        [SerializeField] private GurukulEventAction[] actions = Array.Empty<GurukulEventAction>();

        public string EventId => eventId;
        public GurukulEventTrigger Trigger => trigger;
        public Rect TriggerArea => triggerArea;
        public string TriggerLocationId => triggerLocationId;
        public bool OneTime => oneTime;
        public GurukulEventAction[] Actions => actions;

        internal static GurukulEvent CreateForTest(string eventId, GurukulEventTrigger trigger,
            GurukulEventAction[] actions, bool oneTime = true)
        {
            var authored = CreateInstance<GurukulEvent>();
            authored.eventId = eventId;
            authored.trigger = trigger;
            authored.actions = actions;
            authored.oneTime = oneTime;
            return authored;
        }
    }
}
