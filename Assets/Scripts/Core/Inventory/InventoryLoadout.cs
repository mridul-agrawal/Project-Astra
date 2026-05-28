using UnityEngine;

namespace ProjectAstra.Core
{
    // A pre-configured starting kit. Units point at one of these (as a class
    // default on the UnitDefinition, or a per-map override) and the spawner
    // bakes it into the unit's live inventory.
    [CreateAssetMenu(menuName = "Project Astra/Items/Inventory Loadout", fileName = "NewLoadout")]
    public class InventoryLoadout : ScriptableObject
    {
        [Tooltip("Starting items in slot order (first 5 used). The first wieldable weapon becomes the equipped weapon.")]
        [SerializeField] private ItemDefinition[] _items;

        public ItemDefinition[] Items => _items;

        // Seeds a unit's inventory with fresh runtime copies of each item, so
        // durability ticks independently per unit. Null entries are skipped;
        // items past inventory capacity are ignored.
        public void Apply(UnitInventory inventory)
        {
            if (inventory == null || _items == null) return;

            int slot = 0;
            foreach (ItemDefinition item in _items)
            {
                if (slot >= UnitInventory.Capacity) break;
                if (item == null) continue;
                inventory.SetSlot(slot, item.ToInventoryItem());
                slot++;
            }
        }
    }
}
