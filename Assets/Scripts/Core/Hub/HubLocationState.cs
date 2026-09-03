using System;
using UnityEngine;
using ProjectAstra.Core.Animation;

namespace ProjectAstra.Core.Hub
{
    // Which room she is in, whose house it is, and the way back out of it.
    [Serializable]
    public class HubLocationState
    {
        [SerializeField] private string currentLocationId;

        // Which of the six student houses she is inside. They share one interior, so the name
        // board, the occupant and the way out all hang off this rather than off the room.
        [SerializeField] private string houseIdentity;

        [SerializeField] private string returnLocationId;
        [SerializeField] private Vector2 returnSpawn;
        [SerializeField] private Facing returnFacing;
        [SerializeField] private bool hasReturn;

        public string CurrentLocationId => currentLocationId;
        public string HouseIdentity => houseIdentity;

        public void EnterLocation(string locationId, string identity)
        {
            currentLocationId = locationId;
            // An empty identity leaves the old one alone, so walking around inside a house doesn't
            // forget whose house it is.
            if (!string.IsNullOrEmpty(identity)) houseIdentity = identity;
        }

        public void RememberReturn(string locationId, Vector2 spawn, Facing facing)
        {
            returnLocationId = locationId;
            returnSpawn = spawn;
            returnFacing = facing;
            hasReturn = true;
        }

        public bool TryGetReturn(out string locationId, out Vector2 spawn, out Facing facing)
        {
            locationId = returnLocationId;
            spawn = returnSpawn;
            facing = returnFacing;
            return hasReturn;
        }
    }
}
