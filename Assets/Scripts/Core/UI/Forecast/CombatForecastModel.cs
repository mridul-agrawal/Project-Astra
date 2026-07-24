using UnityEngine;
using ProjectAstra.Core.Combat;
using ProjectAstra.Core.Units;

namespace ProjectAstra.Core.UI.Forecast
{
    // Presentation data for the Combat Forecast panel — one SideModel per combatant.
    public sealed class CombatForecastModel
    {
        public SideModel AttackerSideModel;
        public SideModel DefenderSideModel;
        public TriangleAdvantage TriangleAdvantage;
        public bool AttackerEffective;
        public bool DefenderEffective;
    }

    // One combatant's forecast display data.
    public sealed class SideModel
    {
        public string UnitName;
        public Sprite Portrait;
        public bool FlipPortrait;
        public Faction Faction;
        public string WeaponName;
        public WeaponType WeaponType;
        public bool HasWeapon;
        public int Atk;
        public int Hit;
        public int Crit;
        public int CurrentHP;
        public int MaxHP;
        public bool CanDouble;
        public bool ShowOffense;
        public float HpFraction => MaxHP > 0 ? (float)CurrentHP / MaxHP : 0f;
    }
}
