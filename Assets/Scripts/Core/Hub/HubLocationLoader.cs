using UnityEngine;
using ProjectAstra.Core.Animation;
using ProjectAstra.Core.Units;

namespace ProjectAstra.Core.Hub
{
    // Puts a room on screen: its art, its collision, the people standing in it, and the protagonist
    // wherever she is meant to arrive.
    //
    // One path for both the opening of a visit and every doorway after it, so a room is built the
    // same way however she got there — which is what stops a door round trip from duplicating a
    // character or forgetting a relocation.
    public sealed class HubLocationLoader : MonoBehaviour
    {
        [SerializeField] private HubLocationHost locationHost;
        [SerializeField] private HubLocationDatabase locationDatabase;
        [SerializeField] private UnitDatabase unitDatabase;

        [Tooltip("Everyone except the protagonist is built under here, so a room change can clear the whole cast at once.")]
        [SerializeField] private Transform castRoot;

        public HubActor Player { get; private set; }

        public bool Load(string locationId, Vector2 playerSpawn, Facing playerFacing, string houseIdentity)
        {
            HubLocationData location = locationDatabase != null ? locationDatabase.Get(locationId) : null;
            if (location == null)
            {
                Debug.LogError($"[HubLocationLoader] No location '{locationId}' in the database.");
                return false;
            }

            ClearCast();
            HubLocationService.Load(location);
            locationHost.Show(location);

            HubProgressService.Instance?.State.EnterLocation(locationId, houseIdentity);
            ApplyHouseIdentity();

            PlacePlayer(playerSpawn, playerFacing);
            SpawnCast(locationId);
            return true;
        }

        // The protagonist outlives a room change — she is the one thing carried from one to the
        // next, so she is built once and moved rather than rebuilt.
        private void PlacePlayer(Vector2 spawn, Facing facing)
        {
            if (Player == null) return;
            Player.Place(spawn, facing);
        }

        public HubActor CreatePlayer(string unitId, Vector2 spawn, Facing facing, Transform parent)
        {
            Player = BuildActor(unitId, spawn, facing, parent, conversationId: null, solid: false);
            return Player;
        }

        // Only the people the visit puts in this room. Their placement comes through the progress
        // service, so an objective that has since moved someone wins over the authored baseline.
        private void SpawnCast(string locationId)
        {
            HubVisitData visit = HubProgressService.Instance?.Visit;
            if (visit == null) return;

            foreach (HubCharacterPlacement authored in visit.CharacterPlacements)
            {
                if (!HubProgressService.Instance.TryGetPlacement(authored.characterId, out var placement)) continue;
                if (placement.locationId != locationId) continue;
                BuildActor(placement.characterId, placement.position, placement.facing, castRoot,
                    placement.conversationId, solid: true);
            }
        }

        private void ClearCast()
        {
            if (castRoot == null) return;
            for (int i = castRoot.childCount - 1; i >= 0; i--)
                Destroy(castRoot.GetChild(i).gameObject);
        }

        // The shared student-house interior holds every house's furniture at once; this switches on
        // only the pieces belonging to the house she actually walked into.
        private void ApplyHouseIdentity()
        {
            string identity = HubProgressService.Instance?.State.HouseIdentity;
            foreach (HubHouseIdentity tagged in locationHost.GetComponentsInChildren<HubHouseIdentity>(true))
                tagged.ApplyIdentity(identity);
        }

        private HubActor BuildActor(string unitId, Vector2 position, Facing facing, Transform parent,
            string conversationId, bool solid)
        {
            if (unitDatabase == null || !unitDatabase.TryResolve(unitId, out UnitDefinition definition))
            {
                Debug.LogError($"[HubLocationLoader] No UnitDefinition for '{unitId}'.");
                return null;
            }
            return HubActorFactory.Create(definition, parent, position, facing, conversationId, solid);
        }
    }
}
