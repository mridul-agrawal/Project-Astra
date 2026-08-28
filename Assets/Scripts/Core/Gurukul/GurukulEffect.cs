using System;
using UnityEngine;
using ProjectAstra.Core.Animation;

namespace ProjectAstra.Core.Gurukul
{
    // Append only — authored effects store the kind as an int.
    public enum GurukulEffectKind
    {
        SetGate,
        SetInteractableState,
        RelocateCharacter,
        SetCharacterConversation,
        FireEvent
    }

    // How available an authored interactable is right now. Append only.
    public enum GurukulInteractableState
    {
        Inactive,
        Available,
        Gated,
        Exhausted
    }

    // One world change an objective applies when it completes. Effects write into the visit's
    // runtime state rather than touching the scene, so the same list replays identically whether
    // the player is standing outside or is three rooms away inside a house.
    //
    // One class covers every kind, so most fields are blank for any given effect — the drawer shows
    // only the ones that kind uses, the way CampaignStepDrawer does.
    [Serializable]
    public class GurukulEffect
    {
        public GurukulEffectKind kind;

        [Tooltip("What the effect acts on: a gate name, an interactable id, or a character's unitId.")]
        public string targetId;

        [Header("SetGate")]
        public bool open;

        [Header("SetInteractableState")]
        public GurukulInteractableState state;

        [Header("RelocateCharacter")]
        public string locationId;
        public Vector2 position;
        public Facing facing;

        [Header("SetCharacterConversation / FireEvent")]
        [Tooltip("The conversation the character switches to, or the event to fire.")]
        public string valueId;
    }
}
