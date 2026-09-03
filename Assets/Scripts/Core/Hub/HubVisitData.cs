using System;
using UnityEngine;
using ProjectAstra.Core.Animation;

namespace ProjectAstra.Core.Hub
{
    // Append only — authored departures store the mode as an int.
    public enum HubDepartureMode
    {
        // The closing scripted event runs straight into the battle; the player never gets control back.
        Automatic,
        // The player walks to an authored departure target and confirms.
        ConfirmedInteraction
    }

    // Where a character stands for this visit, and what talking to them opens.
    [Serializable]
    public struct HubCharacterPlacement
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
    public struct HubInteractableOverride
    {
        public string interactableId;
        public HubInteractableState state;
        public string conversationId;
    }

    [Serializable]
    public struct HubDeparture
    {
        [Tooltip("MapData.mapId of the battle this visit leads to. Always authored — never inferred from the current map number.")]
        public string destinationMapId;

        public HubDepartureMode mode;

        [Tooltip("Confirmed mode only: the interactable that offers Depart once every objective is done.")]
        public string departureTargetId;
    }

    // One authored visit: where she starts, who stands where, what to do, which battle follows.
    [CreateAssetMenu(fileName = "HubVisitData", menuName = "Project Astra/Hub/Visit Data")]
    public class HubVisitData : ScriptableObject
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
        [SerializeField] private HubCharacterPlacement[] characterPlacements = Array.Empty<HubCharacterPlacement>();
        [SerializeField] private HubInteractableOverride[] interactableOverrides = Array.Empty<HubInteractableOverride>();

        [Tooltip("Gates that start open. Everything not listed starts closed.")]
        [SerializeField] private string[] openGates = Array.Empty<string>();

        [Tooltip("Names the authored environment dressing for this visit, e.g. the post-flood state.")]
        [SerializeField] private string environmentSet;

        [Header("Progression")]
        [Tooltip("The quest this visit runs, by id, looked up in the QuestCatalog.")]
        [SerializeField] private string questId;


        [SerializeField] private HubDeparture departure;

        public string VisitId => visitId;
        public string DisplayName => displayName;
        public string StartLocationId => startLocationId;
        public Vector2 PlayerSpawn => playerSpawn;
        public Facing PlayerFacing => playerFacing;
        public string OpeningEventId => openingEventId;
        public HubCharacterPlacement[] CharacterPlacements => characterPlacements;
        public HubInteractableOverride[] InteractableOverrides => interactableOverrides;
        public string[] OpenGates => openGates;
        public string EnvironmentSet => environmentSet;
        public string QuestId => questId;
        public HubDeparture Departure => departure;
    }
}
