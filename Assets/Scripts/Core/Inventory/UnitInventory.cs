using System;
using System.Collections.Generic;
using UnityEngine;

namespace ProjectAstra.Core
{
    /// <summary>
    /// Five-slot inventory attached to each unit. Slot 0..4 maps directly to display
    /// positions 1..5. The equipped weapon is always the first equippable weapon found
    /// when scanning slots from index 0 upward, so reorders implicitly re-equip.
    /// </summary>
    [DisallowMultipleComponent]
    public class UnitInventory : MonoBehaviour
    {
        public const int Capacity = 5;

        [SerializeField] private InventoryItem[] _slots = new InventoryItem[Capacity];

        private TestUnit _ownerCache;

        public event Action OnInventoryChanged;
        public event Action<InventoryItem> OnItemDestroyed;

        public TestUnit Owner => _ownerCache ??= GetComponent<TestUnit>();

        private void Awake()
        {
            EnsureSlotsAllocated();
        }

        private void OnValidate()
        {
            EnsureSlotsAllocated();
        }

        private void EnsureSlotsAllocated()
        {
            if (_slots == null || _slots.Length != Capacity)
                _slots = new InventoryItem[Capacity];
        }

        #region Read API

        public InventoryItem GetSlot(int index)
        {
            if (index < 0 || index >= Capacity) return InventoryItem.None;
            return _slots[index];
        }

        public int OccupiedCount
        {
            get
            {
                int count = 0;
                for (int i = 0; i < Capacity; i++)
                    if (!_slots[i].IsEmpty) count++;
                return count;
            }
        }

        public bool IsFull => OccupiedCount >= Capacity;
        public bool IsEmpty => OccupiedCount == 0;

        public int FirstEmptySlot()
        {
            for (int i = 0; i < Capacity; i++)
                if (_slots[i].IsEmpty) return i;
            return -1;
        }

        public int EquippedWeaponSlot
        {
            get
            {
                var owner = Owner;
                for (int i = 0; i < Capacity; i++)
                {
                    var slot = _slots[i];
                    if (slot.kind != ItemKind.Weapon) continue;
                    if (EquipResolver.CanEquip(owner, slot.weapon)) return i;
                }
                return -1;
            }
        }

        public WeaponData GetEquippedWeapon()
        {
            int slot = EquippedWeaponSlot;
            return slot >= 0 ? _slots[slot].weapon : WeaponData.None;
        }

        public bool IsUnarmed => EquippedWeaponSlot < 0;

        #endregion

        #region Write API

        public bool TryAddItem(InventoryItem item, out int slotIndex)
        {
            slotIndex = -1;
            if (item.IsEmpty) return false;

            int empty = FirstEmptySlot();
            if (empty < 0) return false;

            _slots[empty] = item;
            slotIndex = empty;
            RaiseChanged();
            return true;
        }

        public void DiscardSlot(int index)
        {
            if (index < 0 || index >= Capacity) return;
            if (_slots[index].IsEmpty) return;
            _slots[index] = InventoryItem.None;
            RaiseChanged();
        }

        public void SetSlot(int index, InventoryItem item)
        {
            if (index < 0 || index >= Capacity) return;
            _slots[index] = item;
            RaiseChanged();
        }

        public void SwapSlots(int a, int b)
        {
            if (a == b) return;
            if (a < 0 || a >= Capacity || b < 0 || b >= Capacity) return;
            (_slots[a], _slots[b]) = (_slots[b], _slots[a]);
            RaiseChanged();
        }

        public void EquipFromSlot(int slot)
        {
            if (slot <= 0 || slot >= Capacity) return;
            if (_slots[slot].kind != ItemKind.Weapon) return;
            if (!EquipResolver.CanEquip(Owner, _slots[slot].weapon)) return;
            SwapSlots(0, slot);
        }

        #endregion

        #region Combat / use hooks

        public void ConsumeEquippedWeaponUses(int rounds)
        {
            if (rounds <= 0) return;
            int slot = EquippedWeaponSlot;
            if (slot < 0) return;

            var item = _slots[slot];
            for (int i = 0; i < rounds; i++) item.weapon.ConsumeDurability();

            if (item.weapon.IsBroken && !item.weapon.indestructible)
            {
                _slots[slot] = InventoryItem.None;
                RaiseDestroyed(item);
                RaiseChanged();
            }
            else
            {
                _slots[slot] = item;
                RaiseChanged();
            }
        }

        public bool TryUseStaff(TestUnit target, out int amountHealed, out string failReason)
        {
            amountHealed = 0;
            int slot = EquippedWeaponSlot;
            if (slot < 0)
            {
                failReason = "No weapon equipped.";
                return false;
            }

            var item = _slots[slot];
            if (item.weapon.weaponType != WeaponType.Staff || item.weapon.staffEffect == StaffEffect.None)
            {
                failReason = "Equipped weapon is not a healing staff.";
                return false;
            }

            var weapon = item.weapon;
            if (!StaffEffects.ApplyHeal(Owner, target, ref weapon, out amountHealed, out failReason))
                return false;

            item.weapon = weapon;
            if (item.weapon.IsBroken && !item.weapon.indestructible)
            {
                _slots[slot] = InventoryItem.None;
                RaiseDestroyed(item);
            }
            else
            {
                _slots[slot] = item;
            }

            RaiseChanged();
            return true;
        }

        public bool TryUseFortify(
            List<TestUnit> allUnits,
            out List<(TestUnit unit, int amount)> healed, out string failReason)
        {
            healed = null;
            int slot = EquippedWeaponSlot;
            if (slot < 0)
            {
                failReason = "No weapon equipped.";
                return false;
            }

            var item = _slots[slot];
            if (item.weapon.staffEffect != StaffEffect.AreaOfEffect)
            {
                failReason = "Equipped weapon is not a Fortify staff.";
                return false;
            }

            var weapon = item.weapon;
            if (!StaffEffects.ApplyFortify(Owner, allUnits, ref weapon, out healed, out failReason))
                return false;

            item.weapon = weapon;
            if (item.weapon.IsBroken && !item.weapon.indestructible)
            {
                _slots[slot] = InventoryItem.None;
                RaiseDestroyed(item);
            }
            else
            {
                _slots[slot] = item;
            }

            RaiseChanged();
            return true;
        }

        public bool TryUseConsumable(int slot, out string failReason)
        {
            failReason = null;
            if (slot < 0 || slot >= Capacity)
            {
                failReason = "Invalid slot.";
                return false;
            }

            var item = _slots[slot];
            if (item.kind != ItemKind.Consumable)
            {
                failReason = "Not a consumable.";
                return false;
            }
            if (item.consumable.IsDepleted)
            {
                failReason = "Item is depleted.";
                return false;
            }

            if (!ConsumableEffects.Apply(item.consumable, Owner, out failReason))
                return false;

            item.consumable.ConsumeUse();
            if (item.consumable.IsDepleted)
            {
                _slots[slot] = InventoryItem.None;
                RaiseDestroyed(item);
            }
            else
            {
                _slots[slot] = item;
            }

            RaiseChanged();
            return true;
        }

        #endregion

        private void RaiseChanged()
        {
            OnInventoryChanged?.Invoke();
        }

        private void RaiseDestroyed(InventoryItem item)
        {
            OnItemDestroyed?.Invoke(item);
        }
    }
}
