using ProjectAstra.Core.Combat;
using ProjectAstra.Core.Units;

namespace ProjectAstra.Core.UI.UnitInfo
{
    // Builds the GEAR tab model (five inventory slots) and renders it. Keeps the model
    // so the composition root can pull the selected slot's footer text.
    public sealed class UnitGearController
    {
        private readonly UnitGearView view;

        public UnitGearModel Model { get; private set; }
        public int SlotCount => Model != null && Model.Slots != null ? Model.Slots.Length : 0;

        public UnitGearController(UnitGearView view) { this.view = view; }

        public void Render(TestUnit unit)
        {
            Model = BuildModel(unit);
            view?.Render(Model);
        }

        public UnitInfoFooterModel FooterFor(int index)
        {
            if (Model == null || Model.Slots == null || index < 0 || index >= Model.Slots.Length) return null;
            var s = Model.Slots[index];
            return new UnitInfoFooterModel { Icon = s.Icon, Title = s.IsEmpty ? "" : s.Name, Description = s.Description };
        }

        private UnitGearModel BuildModel(TestUnit unit)
        {
            var inv = unit != null ? unit.Inventory : null;
            var equipped = unit != null ? unit.equippedWeapon : WeaponData.None;
            var slots = new GearSlotVM[UnitInventory.Capacity];
            for (int i = 0; i < UnitInventory.Capacity; i++)
            {
                var item = inv != null ? inv.GetSlot(i) : InventoryItem.None;
                slots[i] = BuildSlot(i + 1, item, equipped);
            }
            return new UnitGearModel { Slots = slots };
        }

        private static GearSlotVM BuildSlot(int index, InventoryItem item, WeaponData equipped)
        {
            if (item.IsEmpty) return new GearSlotVM { Index = index, IsEmpty = true };

            bool isWeapon = item.kind == ItemKind.Weapon;
            var w = item.weapon;
            bool isEquipped = isWeapon && !equipped.IsEmpty && w.name == equipped.name;

            return new GearSlotVM
            {
                Index = index, Name = item.DisplayName, IsWeapon = isWeapon,
                WeaponType = isWeapon ? w.weaponType : default,
                TypeBadge = isWeapon ? w.weaponType.ToString().ToUpper() : item.consumable.type.ToString().ToUpper(),
                Mt = w.might, Hit = w.hit, Crt = w.crit, RngMin = w.minRange, RngMax = w.maxRange,
                ShowUses = item.MaxUses > 0, CurrentUses = item.CurrentUses, MaxUses = item.MaxUses,
                IsEquipped = isEquipped, Description = ItemDescriber.Describe(item),
            };
        }
    }
}
