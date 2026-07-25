using UnityEngine;
using ProjectAstra.Core.Combat;

namespace ProjectAstra.Core.UI.UnitInfo
{
    // One of the five gear slots on the GEAR tab.
    public sealed class GearSlotVM
    {
        public int Index;               // 1..5, shown as the slot number
        public bool IsEmpty;
        public bool IsEquipped;
        public string Name;
        public Sprite Icon;

        public bool IsWeapon;
        public WeaponType WeaponType;
        public string TypeBadge;        // "BOW", "STAFF", …
        public int Mt, Hit, Crt, RngMin, RngMax;   // weapon chips

        public bool ShowUses;
        public int CurrentUses, MaxUses;

        public string Description;      // footer text when this slot is selected

        public string RangeText => RngMin == RngMax ? RngMin.ToString() : RngMin + "-" + RngMax;
        public string UsesText => CurrentUses.ToString("D2") + " / " + MaxUses.ToString("D2");
    }

    // GEAR tab model — the five inventory slots.
    public sealed class UnitGearModel
    {
        public GearSlotVM[] Slots;
    }
}
