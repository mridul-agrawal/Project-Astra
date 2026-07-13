using UnityEngine;

namespace ProjectAstra.Core
{
    // Initializes the supply convoy at runtime with a small starter loadout, authored as item
    // assets in the inspector. Attach to any persistent scene object — CursorSceneSetup creates
    // this automatically. No-op if a SupplyConvoy is already active.
    public class ConvoyBootstrap : MonoBehaviour
    {
        [Tooltip("Items deposited into the starter convoy on load.")]
        [SerializeField, HubRef] private ItemDefinition[] starterItems;

        private void Awake()
        {
            if (Convoy.Current is SupplyConvoy) return;

            var convoy = new SupplyConvoy();
            if (starterItems != null)
                foreach (ItemDefinition item in starterItems)
                    if (item != null) convoy.TryDeposit(item.ToInventoryItem());
            Convoy.Current = convoy;
        }
    }
}
