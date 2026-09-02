using System;
using UnityEngine;
using ProjectAstra.Core.Animation;

namespace ProjectAstra.Core.Hub.Events
{
    // What sets an event off. Append only.
    public enum HubEventTrigger
    {
        // Runs as the visit opens, before she is given control.
        VisitLoad,

        // Runs when she walks into an authored patch of ground.
        AreaEntered,

        // Fired by an objective's effect or another event.
        Called
    }

    // The things an event can do. Append only.
    public enum HubEventActionKind
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
    public class HubEventAction
    {
        public HubEventActionKind kind;

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
        public HubInteractableState state;
    }

    // An authored sequence: characters walking, lines being spoken, the world being rearranged.
    [CreateAssetMenu(fileName = "HubEventData", menuName = "Project Astra/Hub/Event Data")]
    public class HubEventData : ScriptableObject
    {
        [SerializeField] private string eventId;
        [SerializeField] private HubEventTrigger trigger = HubEventTrigger.Called;

        [Tooltip("AreaEntered: the patch of ground that sets it off, in tiles.")]
        [SerializeField] private Rect triggerArea;

        [Tooltip("AreaEntered: which room the patch is in.")]
        [SerializeField] private string triggerLocationId;

        [Tooltip("Off for something that can happen again — a repeatable barks. On for anything with consequences.")]
        [SerializeField] private bool oneTime = true;

        [SerializeField] private HubEventAction[] actions = Array.Empty<HubEventAction>();

        public string EventId => eventId;
        public HubEventTrigger Trigger => trigger;
        public Rect TriggerArea => triggerArea;
        public string TriggerLocationId => triggerLocationId;
        public bool OneTime => oneTime;
        public HubEventAction[] Actions => actions;

        internal static HubEventData CreateForTest(string eventId, HubEventTrigger trigger,
            HubEventAction[] actions, bool oneTime = true)
        {
            var authored = CreateInstance<HubEventData>();
            authored.eventId = eventId;
            authored.trigger = trigger;
            authored.actions = actions;
            authored.oneTime = oneTime;
            return authored;
        }
    }
}
