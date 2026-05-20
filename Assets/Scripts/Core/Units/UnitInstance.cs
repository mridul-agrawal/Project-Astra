using System;
using UnityEngine;
using ProjectAstra.Core.Pathfinding;
using ProjectAstra.Core.Stats;

namespace ProjectAstra.Core.Units
{
    // Runtime state of a unit during a chapter — current stats, HP, level,
    // EXP, niyati, stress tier, and movement modifiers. UnitDefinition holds
    // the authored "who the unit is"; UnitInstance holds "what's happened to
    // them this run".
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

        // SS-11. Promoted units carry +20 to represent the 20 unpromoted
        // levels gained pre-promotion, so they gain less EXP against low-
        // level enemies in the FE GBA formula.
        public int EffectiveLevel => CurrentClass != null && CurrentClass.IsPromoted ? Level + 20 : Level;

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
            : this(definition, definition.DefaultClass, definition.BaseLevel, definition.BaseStats) { }

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

        // --- HP ---

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

        // --- Stat boosters ---

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

        // --- Level-up ---

        public StatArray ApplyLevelUp(Func<int, bool> rollFunction)
        {
            var gains = StatUtils.ComputeLevelUpGains(
                Definition.PersonalGrowths,
                CurrentClass.StatGrowthModifiers,
                Stats,
                CurrentClass.StatCaps,
                CurrentClass.HPGainOnLevelUp,
                rollFunction);

            var updated = Stats;
            AddStatsInPlace(ref updated, gains);
            Stats = updated;

            Level++;
            ClampHPToMaxAndRefreshThreshold();
            return gains;
        }

        // --- Promotion ---

        public void Promote(ClassDefinition newClass)
        {
            CurrentClass = newClass;

            var updated = Stats;
            AddStatsInPlace(ref updated, newClass.PromotionBonuses);
            StatUtils.ClampStatsToCaps(ref updated, newClass.StatCaps);
            Stats = updated;

            ClampHPToMaxAndRefreshThreshold();
        }

        // --- Niyati ---

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

        // --- Chapter lifecycle ---

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

        // --- Movement modifiers ---

        public void SetMovementOffset(int offset) => MovementOffset = offset;
        public void ResetMovementOffset() => MovementOffset = 0;

        // --- Stress (USE-04) ---

        public void SetStressTier(int tier) => StressTier = Mathf.Max(0, tier);

        // --- EXP (SS-11) ---

        // Adds raw EXP. Returns true if a level-up threshold was crossed; the
        // caller is expected to invoke ApplyLevelUp() and subtract ExpPerLevel
        // as appropriate. At the promoted level cap, EXP is held at ExpPerLevel
        // and the method returns false (the level-up never fires).
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

        // --- Private helpers ---

        private void RecalculateHPThreshold()
        {
            var previous = HPThreshold;
            HPThreshold = StatUtils.CalculateHPThreshold(CurrentHP, MaxHP);

            if (HPThreshold == HPThreshold.Critical)
                WasCriticalDuringChapter = true;

            if (HPThreshold != previous)
                OnHPThresholdChanged?.Invoke(previous, HPThreshold);
        }

        private void RecalculateNiyatiSymbol()
        {
            var previous = NiyatiSymbol;
            NiyatiSymbol = StatUtils.CalculateNiyatiSymbol(Stats[StatIndex.Niyati], BaseNiyati);

            if (NiyatiSymbol != previous)
                OnNiyatiSymbolChanged?.Invoke(previous, NiyatiSymbol);
        }

        private void ClampHPToMaxAndRefreshThreshold()
        {
            if (CurrentHP > MaxHP) CurrentHP = MaxHP;
            RecalculateHPThreshold();
        }

        private static void AddStatsInPlace(ref StatArray target, StatArray delta)
        {
            for (int i = 0; i < StatArray.Length; i++)
            {
                var idx = (StatIndex)i;
                target[idx] += delta[idx];
            }
        }
    }
}
