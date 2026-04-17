using System;
using UnityEngine;

namespace ProjectAstra.Core
{
    public class UnitInstance
    {
        const int MaxNiyatiStoryDelta = 4;

        // SS-11 / UC-02 — shared level + EXP constants.
        public const int ExpPerLevel = 100;
        public const int PromotedLevelCap = 20;

        public UnitDefinition Definition { get; }
        public ClassDefinition CurrentClass { get; private set; }
        public int Level { get; private set; }
        public StatArray Stats { get; private set; }

        public int CurrentHP { get; private set; }
        public int MaxHP => Stats[StatIndex.HP];
        public HPThreshold HPThreshold { get; private set; }
        public int CurrentEXP { get; private set; }

        // USE-04 — stress / war erosion tier. 0 = no stress.
        public int StressTier { get; private set; }

        public bool IsDead => CurrentHP == 0;
        public bool IsAtLevelCap => CurrentClass != null && CurrentClass.IsPromoted && Level >= PromotedLevelCap;

        public int BaseNiyati { get; private set; }
        public int NiyatiStoryDelta { get; private set; }
        public NiyatiSymbol NiyatiSymbol { get; private set; }
        public bool ShadowSuppressed { get; set; }

        public bool WasCriticalDuringChapter { get; private set; }
        public bool WasHPBelow5DuringChapter { get; private set; }
        public bool PostSurvivalFlag { get; private set; }

        public int MovementOffset { get; private set; }
        public int EffectiveMovement => Mathf.Max(1, CurrentClass.MovementRange + MovementOffset);
        public MovementType MovementType => CurrentClass.MovementType;

        public event Action<HPThreshold, HPThreshold> OnHPThresholdChanged;
        public event Action<NiyatiSymbol, NiyatiSymbol> OnNiyatiSymbolChanged;

        public UnitInstance(UnitDefinition definition)
        {
            Definition = definition;
            CurrentClass = definition.DefaultClass;
            Level = definition.BaseLevel;
            Stats = definition.BaseStats;
            CurrentHP = MaxHP;
            BaseNiyati = Stats[StatIndex.Niyati];
            RecalculateHPThreshold();
            RecalculateNiyatiSymbol();
        }

        public UnitInstance(UnitDefinition definition, ClassDefinition classOverride, int level, StatArray stats)
        {
            Definition = definition;
            CurrentClass = classOverride;
            Level = level;
            Stats = stats;
            CurrentHP = MaxHP;
            BaseNiyati = stats[StatIndex.Niyati];
            RecalculateHPThreshold();
            RecalculateNiyatiSymbol();
        }

        #region HP

        public void ApplyDamage(int amount)
        {
            if (amount <= 0) return;
            SetCurrentHP(CurrentHP - amount);
        }

        public void ApplyHealing(int amount)
        {
            if (amount <= 0) return;
            SetCurrentHP(CurrentHP + amount);
        }

        public void SetCurrentHP(int value)
        {
            CurrentHP = Mathf.Clamp(value, 0, MaxHP);
            if (CurrentHP <= 4) WasHPBelow5DuringChapter = true;
            RecalculateHPThreshold();
        }

        private void RecalculateHPThreshold()
        {
            var previous = HPThreshold;
            HPThreshold = StatUtils.CalculateHPThreshold(CurrentHP, MaxHP);

            if (HPThreshold == HPThreshold.Critical)
                WasCriticalDuringChapter = true;

            if (HPThreshold != previous)
                OnHPThresholdChanged?.Invoke(previous, HPThreshold);
        }

        #endregion

        #region Stat boosters

        public void ApplyStatBoost(StatIndex stat, int amount)
        {
            if (amount <= 0) return;
            var updated = Stats;
            updated[stat] += amount;
            if (CurrentClass != null)
                StatUtils.ClampStatsToCaps(ref updated, CurrentClass.StatCaps);
            Stats = updated;

            if (stat == StatIndex.HP)
                RecalculateHPThreshold();
        }

        #endregion

        #region Level-up

        public StatArray ApplyLevelUp(Func<int, int, bool> rollFunction)
        {
            var gains = StatUtils.ComputeLevelUpGains(
                Definition.PersonalGrowths,
                CurrentClass.StatGrowthModifiers,
                Stats,
                CurrentClass.StatCaps,
                CurrentClass.HPGainOnLevelUp,
                rollFunction);

            var updated = Stats;
            for (int i = 0; i < StatArray.Length; i++)
            {
                var idx = (StatIndex)i;
                updated[idx] += gains[idx];
            }
            Stats = updated;
            Level++;

            if (CurrentHP > MaxHP)
                CurrentHP = MaxHP;

            RecalculateHPThreshold();
            return gains;
        }

        #endregion

        #region Promotion

        public void Promote(ClassDefinition newClass)
        {
            CurrentClass = newClass;

            var updated = Stats;
            var bonuses = newClass.PromotionBonuses;
            var caps = newClass.StatCaps;

            for (int i = 0; i < StatArray.Length; i++)
            {
                var idx = (StatIndex)i;
                updated[idx] += bonuses[idx];
            }

            StatUtils.ClampStatsToCaps(ref updated, caps);
            Stats = updated;

            if (CurrentHP > MaxHP)
                CurrentHP = MaxHP;

            RecalculateHPThreshold();
        }

        #endregion

        #region Niyati

        public void ApplyNiyatiStoryDelta(int delta)
        {
            int newCumulative = NiyatiStoryDelta + delta;
            if (Mathf.Abs(newCumulative) > MaxNiyatiStoryDelta) return;

            NiyatiStoryDelta = newCumulative;

            var updated = Stats;
            updated[StatIndex.Niyati] = Mathf.Clamp(
                updated[StatIndex.Niyati] + delta,
                0,
                CurrentClass.StatCaps[StatIndex.Niyati]);
            Stats = updated;

            RecalculateNiyatiSymbol();
        }

        private void RecalculateNiyatiSymbol()
        {
            var previous = NiyatiSymbol;
            NiyatiSymbol = StatUtils.CalculateNiyatiSymbol(Stats[StatIndex.Niyati], BaseNiyati);

            if (NiyatiSymbol != previous)
                OnNiyatiSymbolChanged?.Invoke(previous, NiyatiSymbol);
        }

        #endregion

        #region Chapter lifecycle

        public void OnChapterStart()
        {
            WasCriticalDuringChapter = false;
            WasHPBelow5DuringChapter = false;
            PostSurvivalFlag = false;
            ShadowSuppressed = false;
        }

        public void OnChapterEnd()
        {
            PostSurvivalFlag = WasHPBelow5DuringChapter;
        }

        #endregion

        #region Movement modifiers

        public void SetMovementOffset(int offset)
        {
            MovementOffset = offset;
        }

        public void ResetMovementOffset()
        {
            MovementOffset = 0;
        }

        #endregion

        #region Stress (USE-04)

        public void SetStressTier(int tier)
        {
            StressTier = Mathf.Max(0, tier);
        }

        #endregion

        #region EXP (SS-11)

        // Adds raw EXP. Returns true if a level-up threshold was crossed; caller is
        // expected to invoke ApplyLevelUp() and subtract ExpPerLevel as appropriate.
        // No-op if the unit is at the promoted level cap — caps EXP at ExpPerLevel.
        public bool AddExp(int amount)
        {
            if (amount <= 0) return false;
            if (IsAtLevelCap)
            {
                CurrentEXP = Mathf.Min(CurrentEXP + amount, ExpPerLevel);
                return false;
            }
            CurrentEXP += amount;
            return CurrentEXP >= ExpPerLevel;
        }

        public void ConsumeExpForLevelUp()
        {
            CurrentEXP = Mathf.Max(0, CurrentEXP - ExpPerLevel);
        }

        #endregion
    }
}
