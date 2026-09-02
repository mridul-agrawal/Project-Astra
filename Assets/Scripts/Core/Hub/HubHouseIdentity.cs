using UnityEngine;

namespace ProjectAstra.Core.Hub
{
    // Marks something in a shared interior as belonging to one particular house.
    //
    // The six student houses use one room, so the name board, the bedding and whatever else differs
    // all live in that room together and only the right set is switched on. Anything without this
    // component is part of every house.
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
