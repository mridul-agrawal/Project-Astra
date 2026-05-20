using System;
using ProjectAstra.Core.UI.Overlays;
using ProjectAstra.Core.Units;

namespace ProjectAstra.Core.Cursor
{
    // Runs an inventory-mutating action and surfaces a toast for any item
    // that breaks or depletes during the mutation. Both CombatExecutor and
    // StaffExecutor route durability consumption through this so the
    // "Sword broke!" toast logic lives in exactly one place.
    public static class ItemBreakToaster
    {
        public static void WithBreakAnnouncements(TestUnit unit, ToastNotificationUI toastUI, Action mutate)
        {
            void OnDestroyed(InventoryItem item)
            {
                if (toastUI == null) return;
                string message = item.kind == ItemKind.Weapon
                    ? $"{item.weapon.name} broke!"
                    : $"{item.consumable.name} depleted";
                toastUI.Show(message);
            }

            unit.Inventory.OnItemDestroyed += OnDestroyed;
            try { mutate(); }
            finally { unit.Inventory.OnItemDestroyed -= OnDestroyed; }
        }
    }
}
