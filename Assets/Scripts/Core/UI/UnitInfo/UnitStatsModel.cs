using UnityEngine;
using ProjectAstra.Core.Combat;

namespace ProjectAstra.Core.UI.UnitInfo
{
    // Visual grouping of the stat rows, per the reference (ATTACK / GUARD / BODY headers).
    public enum StatGroup { Attack, Guard, Body }

    // One stat row: value out of its class cap, drawn as a fill bar.
    public sealed class StatRowVM
    {
        public string Label;
        public int Value;
        public int Cap;
        public Sprite Icon;
        public StatGroup Group;
        public string Description;      // footer text when this row is selected
        public float Fraction => Cap > 0 ? Mathf.Clamp01((float)Value / Cap) : 0f;
    }

    // The equipped-weapon panel at the top of the STATS tab (raw weapon stats, not derived).
    public sealed class EquippedWeaponVM
    {
        public bool HasWeapon;
        public string Name;
        public WeaponType WeaponType;
        public Sprite Sigil;
        public int Mt, Hit, Crt, RngMin, RngMax;
        public string RangeText => RngMin == RngMax ? RngMin.ToString() : RngMin + "-" + RngMax;
    }

    // STATS tab model — the weapon panel + the nine stat rows in fixed order (STR…MOVE).
    public sealed class UnitStatsModel
    {
        public EquippedWeaponVM Weapon;
        public StatRowVM[] Rows;
    }
}
