using System;
using UnityEngine;
using ProjectAstra.Core.Animation;

namespace ProjectAstra.Core.Gurukul
{
    // Append only — authored departures store the mode as an int.
    public enum GurukulDepartureMode
    {
        // The closing scripted event runs straight into the battle; the player never gets control back.
        Automatic,
        // The player walks to an authored departure target and confirms.
        ConfirmedInteraction
    }

    // Where a character stands for this visit and what talking to them opens. Placement belongs to
    // the visit, not the character, so the same cast can be arranged differently every time.
    [Serializable]
    public struct GurukulCharacterPlacement
    {
        [Tooltip("UnitDefinition.unitId — the character's identity, portraits and map animation come from there.")]
        public string characterId;

        public string locationId;

        [Tooltip("Foot position in tiles, within the location.")]
        public Vector2 position;

        public Facing facing;

        [Tooltip("Conversation opened by talking to them. Leave empty and they are present but not interactable.")]
        public string conversationId;
    }

    // An interactable that starts this visit in something other than its authored default.
    [Serializable]
    public struct GurukulInteractableOverride
    {
        public string interactableId;
        public GurukulInteractableState state;
        public string conversationId;
    }

    [Serializable]
    public struct GurukulDeparture
    {
        [Tooltip("MapData.mapId of the battle this visit leads to. Always authored — never inferred from the current map number.")]
        public string destinationMapId;

        public GurukulDepartureMode mode;

        [Tooltip("Confirmed mode only: the interactable that offers Depart once every objective is done.")]
        public string departureTargetId;
    }

    // One authored visit to the Gurukul: where the player starts, where everyone is standing, what
    // has to be done, and which battle it leads to. A visit is a complete starting state, not a
    // continuation — loading one replaces whatever the previous visit left behind.
    [CreateAssetMenu(fileName = "GurukulVisitData", menuName = "Project Astra/Gurukul/Visit Data")]
    public class GurukulVisitData : ScriptableObject
    {
        [SerializeField] private string visitId;
        [SerializeField] private string displayName;

        [Header("Opening")]
        [SerializeField] private string startLocationId;
        [SerializeField] private Vector2 playerSpawn;
        [SerializeField] private Facing playerFacing = Facing.South;

        [Tooltip("Event that runs before the player gets control. Leave empty to start in free exploration.")]
        [SerializeField] private string openingEventId;

        [Header("Baseline world state")]
        [SerializeField] private GurukulCharacterPlacement[] characterPlacements = Array.Empty<GurukulCharacterPlacement>();
        [SerializeField] private GurukulInteractableOverride[] interactableOverrides = Array.Empty<GurukulInteractableOverride>();

        [Tooltip("Gates that start open. Everything not listed starts closed.")]
        [SerializeField] private string[] openGates = Array.Empty<string>();

        [Tooltip("Names the authored environment dressing for this visit, e.g. the post-flood state.")]
        [SerializeField] private string environmentSet;

        [Header("Progression")]
        [Tooltip("Worked through in order. The next one activates only once the current one completes and its effects are applied.")]
        [SerializeField] private GurukulObjectiveData[] objectives = Array.Empty<GurukulObjectiveData>();

        [SerializeField] private GurukulDeparture departure;

        public string VisitId => visitId;
        public string DisplayName => displayName;
        public string StartLocationId => startLocationId;
        public Vector2 PlayerSpawn => playerSpawn;
        public Facing PlayerFacing => playerFacing;
        public string OpeningEventId => openingEventId;
        public GurukulCharacterPlacement[] CharacterPlacements => characterPlacements;
        public GurukulInteractableOverride[] InteractableOverrides => interactableOverrides;
        public string[] OpenGates => openGates;
        public string EnvironmentSet => environmentSet;
        public GurukulObjectiveData[] Objectives => objectives;
        public GurukulDeparture Departure => departure;
    }
}
