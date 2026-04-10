using UnityEngine;

namespace ProjectAstra.Core
{
    /// <summary>
    /// Initializes the supply convoy at runtime. Attach to any persistent scene object.
    /// CursorSceneSetup creates this automatically.
    /// </summary>
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
