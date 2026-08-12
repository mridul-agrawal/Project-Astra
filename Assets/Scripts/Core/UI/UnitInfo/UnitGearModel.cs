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
        public int Mt, Hit, Crt, RngMin, RngMax, Weight;   // §9 chip row: RNG · WT · MT · HIT · CRT

        public bool ShowUses;
        public int CurrentUses, MaxUses;

        public string Grade;            // "Prf" for a personal weapon, otherwise its rank letter
        public Color GradeColor;
        public bool GradeIsPersonal;    // Prf takes a gold fill and a dark letter
        public string EffectText;       // consumables expand to an effect chip instead of stats

        public string Description;      // footer text when this slot is selected
        public string Detail;           // §7 line two

        public string RangeText => RngMin == RngMax ? RngMin.ToString() : RngMin + "-" + RngMax;
        public string UsesText => CurrentUses.ToString("D2") + " / " + MaxUses.ToString("D2");
        public float UsesFraction => MaxUses > 0 ? (float)CurrentUses / MaxUses : 0f;
    }

    // GEAR tab model — the five inventory slots.
    public sealed class UnitGearModel
    {
        public GearSlotVM[] Slots;
    }
}
