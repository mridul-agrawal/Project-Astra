using System;
using UnityEngine;
using ProjectAstra.Core.Animation;

namespace ProjectAstra.Core.Hub
{
    // Append only — authored effects store the kind as an int.
    public enum HubEffectKind
    {
        SetGate,
        SetInteractableState,
        RelocateCharacter,
        SetCharacterConversation,
        FireEvent
    }

    // How available an authored interactable is right now. Append only.
    public enum HubInteractableState
    {
        Inactive,
        Available,
        Gated,
        Exhausted
    }

    // One world change an objective applies when it completes.
    [Serializable]
    public class HubEffect
    {
        public HubEffectKind kind;

        [Tooltip("What the effect acts on: a gate name, an interactable id, or a character's unitId.")]
        public string targetId;

        [Header("SetGate")]
        public bool open;

        [Header("SetInteractableState")]
        public HubInteractableState state;

        [Header("RelocateCharacter")]
        public string locationId;
        public Vector2 position;
        public Facing facing;

        [Header("SetCharacterConversation / FireEvent")]
        [Tooltip("The conversation the character switches to, or the event to fire.")]
        public string valueId;
    }
}
