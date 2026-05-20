using UnityEngine;
using ProjectAstra.Core.Combat;

namespace ProjectAstra.Core
{
    // Initializes the supply convoy at runtime with a small starter loadout.
    // Attach to any persistent scene object — CursorSceneSetup creates this
    // automatically. No-op if a SupplyConvoy is already active.
    public class ConvoyBootstrap : MonoBehaviour
    {
        private void Awake()
        {
            if (Convoy.Current is SupplyConvoy) return;

            var convoy = new SupplyConvoy();
            convoy.TryDeposit(InventoryItem.FromWeapon(WeaponData.IronLance));
            convoy.TryDeposit(InventoryItem.FromWeapon(WeaponData.Fire));
            convoy.TryDeposit(InventoryItem.FromConsumable(ConsumableData.Vulnerary));
            Convoy.Current = convoy;
        }
    }
}
