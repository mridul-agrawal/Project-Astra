using UnityEngine;

namespace ProjectAstra.Core
{
    // Base type for an authored item asset (a weapon or a consumable). Concrete
    // subclasses "bake" themselves into the runtime InventoryItem that an
    // inventory actually carries. The asset stays immutable design data; the
    // baked copy owns the mutable per-battle state (durability / uses), which is
    // why the runtime side is still a struct.
    public abstract class ItemDefinition : ScriptableObject
    {
        [Tooltip("Name shown in-game and copied onto the runtime item. Must be non-empty or the baked item reads as empty.")]
        [SerializeField] private string _displayName;

        public string DisplayName => _displayName;

        // Builds the runtime slot value for this item, with uses reset to full.
        public abstract InventoryItem ToInventoryItem();
    }
}
