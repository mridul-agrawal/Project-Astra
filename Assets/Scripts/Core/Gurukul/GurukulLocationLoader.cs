using UnityEngine;
using ProjectAstra.Core.Animation;
using ProjectAstra.Core.Units;

namespace ProjectAstra.Core.Gurukul
{
    // Puts a room on screen: its art, its collision, the people standing in it, and the protagonist
    // wherever she is meant to arrive.
    //
    // One path for both the opening of a visit and every doorway after it, so a room is built the
    // same way however she got there — which is what stops a door round trip from duplicating a
    // character or forgetting a relocation.
    public sealed class GurukulLocationLoader : MonoBehaviour
    {
        [SerializeField] private GurukulLocationHost locationHost;
        [SerializeField] private GurukulLocationCatalog locationCatalog;
        [SerializeField] private UnitDatabase unitDatabase;

        [Tooltip("Everyone except the protagonist is built under here, so a room change can clear the whole cast at once.")]
        [SerializeField] private Transform castRoot;

        public GurukulActor Player { get; private set; }

        public bool Load(string locationId, Vector2 playerSpawn, Facing playerFacing, string houseIdentity)
        {
            GurukulLocation location = locationCatalog != null ? locationCatalog.Get(locationId) : null;
            if (location == null)
            {
                Debug.LogError($"[GurukulLocationLoader] No location '{locationId}' in the catalog.");
                return false;
            }

            ClearCast();
            GurukulLocationService.Load(location);
            locationHost.Show(location);

            GurukulProgressService.Instance?.State.EnterLocation(locationId, houseIdentity);
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

        public GurukulActor CreatePlayer(string unitId, Vector2 spawn, Facing facing, Transform parent)
        {
            Player = BuildActor(unitId, spawn, facing, parent, conversationId: null, solid: false);
            return Player;
        }

        // Only the people the visit puts in this room. Their placement comes through the progress
        // service, so an objective that has since moved someone wins over the authored baseline.
        private void SpawnCast(string locationId)
        {
            GurukulVisit visit = GurukulProgressService.Instance?.Visit;
            if (visit == null) return;

            foreach (GurukulCharacterPlacement authored in visit.CharacterPlacements)
            {
                if (!GurukulProgressService.Instance.TryGetPlacement(authored.characterId, out var placement)) continue;
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
            string identity = GurukulProgressService.Instance?.State.HouseIdentity;
            foreach (GurukulHouseIdentity tagged in locationHost.GetComponentsInChildren<GurukulHouseIdentity>(true))
                tagged.ApplyIdentity(identity);
        }

        private GurukulActor BuildActor(string unitId, Vector2 position, Facing facing, Transform parent,
            string conversationId, bool solid)
        {
            if (unitDatabase == null || !unitDatabase.TryResolve(unitId, out UnitDefinition definition))
            {
                Debug.LogError($"[GurukulLocationLoader] No UnitDefinition for '{unitId}'.");
                return null;
            }
            return GurukulActorFactory.Create(definition, parent, position, facing, conversationId, solid);
        }
    }
}
