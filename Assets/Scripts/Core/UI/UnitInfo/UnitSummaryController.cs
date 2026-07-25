using UnityEngine;
using ProjectAstra.Core.Stats;
using ProjectAstra.Core.Units;

namespace ProjectAstra.Core.UI.UnitInfo
{
    // Builds the left-column summary model from a unit and renders it.
    public sealed class UnitSummaryController
    {
        private readonly UnitSummaryView view;

        public UnitSummaryController(UnitSummaryView view) { this.view = view; }

        public void Render(TestUnit unit) => view?.Render(BuildModel(unit));

        private UnitSummaryModel BuildModel(TestUnit unit)
        {
            var inst = unit != null ? unit.UnitInstance : null;
            var def = inst != null ? inst.Definition : null;
            bool showExp = unit != null && unit.faction != Faction.Enemy;
            bool stressed = inst != null && inst.StressTier >= 1;

            return new UnitSummaryModel
            {
                UnitName  = (def != null ? def.UnitName : unit != null ? unit.name : "").ToUpper(),
                Portrait  = PickPortrait(def, inst),
                ClassName = inst != null && inst.CurrentClass != null ? inst.CurrentClass.ClassName : "",
                Level     = inst != null ? inst.Level : 0,
                CurrentHP = inst != null ? inst.CurrentHP : (unit != null ? unit.currentHP : 0),
                MaxHP     = inst != null ? inst.MaxHP : (unit != null ? unit.maxHP : 0),
                ShowExp   = showExp,
                ExpText   = ExpText(inst),
                ExpFraction = inst != null ? Mathf.Clamp01((float)inst.CurrentEXP / UnitInstance.ExpPerLevel) : 0f,
                ShowStatus = stressed,
                StatusText = stressed ? "STRESSED" : "",
            };
        }

        // "MAX" at the promoted cap; "--" for an unpromoted unit stuck at level 20.
        private static string ExpText(UnitInstance inst)
        {
            if (inst == null) return "";
            if (inst.IsAtLevelCap) return "MAX";
            bool atUnpromotedCap = inst.CurrentClass != null && !inst.CurrentClass.IsPromoted
                && inst.Level >= UnitInstance.PromotedLevelCap;
            if (atUnpromotedCap) return "--";
            return inst.CurrentEXP + " / " + UnitInstance.ExpPerLevel;
        }

        // HP-appropriate portrait variant (lifted from the old panel).
        private static Sprite PickPortrait(UnitDefinition def, UnitInstance inst)
        {
            if (def == null) return null;
            bool isDead = inst != null && inst.IsDead;
            var hpT = inst != null ? inst.HPThreshold : HPThreshold.Normal;
            if (isDead && def.DeceasedPortrait != null) return def.DeceasedPortrait;
            if (hpT == HPThreshold.Critical && def.CriticalPortrait != null) return def.CriticalPortrait;
            if (hpT == HPThreshold.Injured && def.WoundedPortrait != null) return def.WoundedPortrait;
            return def.Portrait;
        }
    }
}
