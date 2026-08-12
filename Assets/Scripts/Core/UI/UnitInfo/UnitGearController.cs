using UnityEngine;
using ProjectAstra.Core.Combat;
using ProjectAstra.Core.Units;

namespace ProjectAstra.Core.UI.UnitInfo
{
    // Builds the GEAR tab model (five inventory slots) and renders it. Keeps the model
    // so the composition root can pull the selected slot's footer text.
    public sealed class UnitGearController
    {
        // §9 tier colours. S is not in the spec's E-A list, so it shares A's gold.
        static readonly Color RankE = new Color32(0x8b, 0x93, 0xa0, 0xff);
        static readonly Color RankD = new Color32(0x79, 0xb5, 0x79, 0xff);
        static readonly Color RankC = new Color32(0x6f, 0x9f, 0xdc, 0xff);
        static readonly Color RankB = new Color32(0xa0, 0x5a, 0xc9, 0xff);
        static readonly Color RankA = new Color32(0xe8, 0xb3, 0x4b, 0xff);
        static readonly Color Gold  = new Color32(0xe8, 0xb3, 0x4b, 0xff);

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
            return new UnitInfoFooterModel
            {
                Icon = s.Icon,
                Title = s.IsEmpty ? "EMPTY SLOT" : s.Name,
                Description = s.Description,
                Detail = s.Detail,
            };
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
            if (item.IsEmpty)
                return new GearSlotVM
                {
                    Index = index, IsEmpty = true,
                    Description = ItemDescriber.Summary(item), Detail = ItemDescriber.Detail(item),
                };

            bool isWeapon = item.kind == ItemKind.Weapon;
            var w = item.weapon;
            bool isEquipped = isWeapon && !equipped.IsEmpty && w.name == equipped.name;

            return new GearSlotVM
            {
                Index = index, Name = item.DisplayName, IsWeapon = isWeapon,
                WeaponType = isWeapon ? w.weaponType : default,
                TypeBadge = isWeapon ? w.weaponType.ToString().ToUpper() : item.consumable.type.ToString().ToUpper(),
                Mt = w.might, Hit = w.hit, Crt = w.crit, RngMin = w.minRange, RngMax = w.maxRange,
                Weight = w.weight,
                Grade = isWeapon ? GradeOf(w) : "",
                GradeColor = isWeapon ? GradeColorOf(w) : Color.white,
                GradeIsPersonal = isWeapon && w.characterLocked,
                EffectText = isWeapon ? "" : ItemDescriber.Effect(item.consumable),
                ShowUses = item.MaxUses > 0, CurrentUses = item.CurrentUses, MaxUses = item.MaxUses,
                IsEquipped = isEquipped,
                Description = ItemDescriber.Summary(item), Detail = ItemDescriber.Detail(item),
            };
        }

        // A character-locked weapon reads as Prf; everything else shows the rank it demands.
        private static string GradeOf(WeaponData w) =>
            w.characterLocked ? "Prf" : w.minRank.ToString();

        private static Color GradeColorOf(WeaponData w)
        {
            if (w.characterLocked) return Gold;

            return w.minRank switch
            {
                WeaponRank.E => RankE,
                WeaponRank.D => RankD,
                WeaponRank.C => RankC,
                WeaponRank.B => RankB,
                _            => RankA,
            };
        }
    }
}
