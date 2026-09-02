using UnityEngine;

namespace ProjectAstra.Core.Hub
{
    // Marks something in the shared student-house interior as belonging to one house.
    public sealed class HubHouseIdentity : MonoBehaviour
    {
        [Tooltip("Matches the houseIdentityId on the door she came in through.")]
        [SerializeField] private string houseIdentityId;

        public string HouseIdentityId => houseIdentityId;

        public void ApplyIdentity(string currentIdentity)
        {
            gameObject.SetActive(string.IsNullOrEmpty(houseIdentityId) || houseIdentityId == currentIdentity);
        }
    }
}
