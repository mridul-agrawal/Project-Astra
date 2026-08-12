using System.Collections.Generic;
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
            UnitInstance unitInstance = unit != null ? unit.UnitInstance : null;
            UnitDefinition unitDefinition = unitInstance != null ? unitInstance.Definition : null;
            bool showExp = unit != null && unit.faction != Faction.Enemy;
            bool stressed = unitInstance != null && unitInstance.StressTier >= 1;

            return new UnitSummaryModel
            {
                UnitName  = (unitDefinition != null ? unitDefinition.UnitName : unit != null ? unit.name : "").ToUpper(),
                Portrait  = PickPortrait(unitDefinition, unitInstance),
                ClassName = unitInstance != null && unitInstance.CurrentClass != null ? unitInstance.CurrentClass.ClassName : "",
                Level     = unitInstance != null ? unitInstance.Level : 0,
                CurrentHP = unitInstance != null ? unitInstance.CurrentHP : (unit != null ? unit.currentHP : 0),
                MaxHP     = unitInstance != null ? unitInstance.MaxHP : (unit != null ? unit.maxHP : 0),
                ShowExp   = showExp,
                ExpText   = ExpText(unitInstance),
                ExpFraction = unitInstance != null ? Mathf.Clamp01((float)unitInstance.CurrentEXP / UnitInstance.ExpPerLevel) : 0f,
                ShowStatus = stressed,
                StatusText = stressed ? "STRESSED" : "",
                Statuses = BuildStatuses(stressed),
                IsActed  = unit != null && unit.hasActed && unit.faction != Faction.Enemy,
                IsEnemy  = unit != null && unit.faction == Faction.Enemy,
            };
        }

        // Stress is the only status the game tracks today. The §2 chip row holds four, so the
        // rest fill in on their own once there is a status system to read.
        private static List<UnitStatusKind> BuildStatuses(bool stressed)
        {
            var statuses = new List<UnitStatusKind>();
            if (stressed) statuses.Add(UnitStatusKind.Stressed);
            return statuses;
        }

        // "MAX" at the promoted cap; "--" for an unpromoted unit stuck at level 20.
        private static string ExpText(UnitInstance unitInstance)
        {
            if (unitInstance == null) return "";
            if (unitInstance.IsAtLevelCap) return "MAX";
            bool atUnpromotedCap = unitInstance.CurrentClass != null && !unitInstance.CurrentClass.IsPromoted
                && unitInstance.Level >= UnitInstance.PromotedLevelCap;
            if (atUnpromotedCap) return "--";
            return unitInstance.CurrentEXP + " / " + UnitInstance.ExpPerLevel;
        }

        // HP-appropriate portrait variant (lifted from the old panel).
        private static Sprite PickPortrait(UnitDefinition unitDefinition, UnitInstance unitInstance)
        {
            if (unitDefinition == null) return null;
            bool isDead = unitInstance != null && unitInstance.IsDead;
            var hpT = unitInstance != null ? unitInstance.HPThreshold : HPThreshold.Normal;
            if (isDead && unitDefinition.DeceasedPortrait != null) return unitDefinition.DeceasedPortrait;
            if (hpT == HPThreshold.Critical && unitDefinition.CriticalPortrait != null) return unitDefinition.CriticalPortrait;
            if (hpT == HPThreshold.Injured && unitDefinition.WoundedPortrait != null) return unitDefinition.WoundedPortrait;
            return unitDefinition.Portrait;
        }
    }
}
