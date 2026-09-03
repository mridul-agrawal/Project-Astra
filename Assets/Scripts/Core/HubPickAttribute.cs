using UnityEngine;

namespace ProjectAstra.Core
{
    // The kinds of name the hub refers to things by. Each one is a dropdown a designer picks from
    // rather than a string they have to remember and spell.
    public enum HubIdKind
    {
        Conversation,
        Location,
        Character,
        Interactable,
        Event,
        Quest,
        Objective,
        Visit,
        Door,
        Gate,
        Signal,
        Map
    }

    // Marks a string field as one of those names. Nothing is typed; the editor offers what exists.
    public class HubPickAttribute : PropertyAttribute
    {
        public readonly HubIdKind Kind;

        public HubPickAttribute(HubIdKind kind) => Kind = kind;
    }
}
