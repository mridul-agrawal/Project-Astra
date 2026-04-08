using NUnit.Framework;
using ProjectAstra.Core;

namespace ProjectAstra.Core.Tests
{
    [TestFixture]
    public class StatUtilsTests
    {
        [Test]
        public void RollGrowth_BelowRate_ReturnsTrue()
        {
            Assert.IsTrue(StatUtils.RollGrowth(50, 49));
        }

        [Test]
        public void RollGrowth_AtRate_ReturnsFalse()
        {
            Assert.IsFalse(StatUtils.RollGrowth(50, 50));
        }

        [Test]
        public void RollGrowth_ZeroGrowth_AlwaysFalse()
        {
            Assert.IsFalse(StatUtils.RollGrowth(0, 0));
        }

        [Test]
        public void EffectiveGrowth_SumsPersonalAndClass()
        {
            Assert.AreEqual(65, StatUtils.EffectiveGrowth(50, 15));
        }

        [Test]
        public void AttackSpeed_NoWeightPenalty()
        {
            Assert.AreEqual(10, StatUtils.AttackSpeed(10, 5, 8));
        }

        [Test]
        public void AttackSpeed_WeightExceedsCon()
        {
            Assert.AreEqual(7, StatUtils.AttackSpeed(10, 8, 5));
        }

        [Test]
        public void AttackSpeed_NeverNegative()
        {
            Assert.AreEqual(0, StatUtils.AttackSpeed(3, 20, 1));
        }

        [Test]
        public void AttackSpeed_NoWeapon_EqualsSpeed()
        {
            Assert.AreEqual(12, StatUtils.AttackSpeed(12, 0, 5));
        }

        [Test]
        public void WeightPenalty_WeaponLighterThanCon_Zero()
        {
            Assert.AreEqual(0, StatUtils.WeightPenalty(5, 8));
        }

        [Test]
        public void WeightPenalty_WeaponHeavierThanCon()
        {
            Assert.AreEqual(3, StatUtils.WeightPenalty(8, 5));
        }

        [Test]
        public void HPThreshold_Above50_Normal()
        {
            Assert.AreEqual(HPThreshold.Normal, StatUtils.CalculateHPThreshold(51, 100));
        }

        [Test]
        public void HPThreshold_At50_Injured()
        {
            Assert.AreEqual(HPThreshold.Injured, StatUtils.CalculateHPThreshold(50, 100));
        }

        [Test]
        public void HPThreshold_At31_Injured()
        {
            Assert.AreEqual(HPThreshold.Injured, StatUtils.CalculateHPThreshold(31, 100));
        }

        [Test]
        public void HPThreshold_At30_Critical()
        {
            Assert.AreEqual(HPThreshold.Critical, StatUtils.CalculateHPThreshold(30, 100));
        }

        [Test]
        public void HPThreshold_ZeroHP_Critical()
        {
            Assert.AreEqual(HPThreshold.Critical, StatUtils.CalculateHPThreshold(0, 30));
        }

        [Test]
        public void HPThreshold_ZeroMaxHP_Critical()
        {
            Assert.AreEqual(HPThreshold.Critical, StatUtils.CalculateHPThreshold(0, 0));
        }

        [Test]
        public void HPThreshold_LowMaxHP_IntegerArithmetic()
        {
            // 12 max HP, 30% = 3.6, floor to 3. HP=4 should be Injured, HP=3 should be Critical.
            Assert.AreEqual(HPThreshold.Injured, StatUtils.CalculateHPThreshold(4, 12));
            Assert.AreEqual(HPThreshold.Critical, StatUtils.CalculateHPThreshold(3, 12));
        }

        [Test]
        public void NiyatiSymbol_Above1Point2_LotusFull()
        {
            Assert.AreEqual(NiyatiSymbol.LotusFull, StatUtils.CalculateNiyatiSymbol(13, 10));
        }

        [Test]
        public void NiyatiSymbol_At1Point2_LotusHalf()
        {
            Assert.AreEqual(NiyatiSymbol.LotusHalf, StatUtils.CalculateNiyatiSymbol(12, 10));
        }

        [Test]
        public void NiyatiSymbol_At0Point8_LotusHalf()
        {
            Assert.AreEqual(NiyatiSymbol.LotusHalf, StatUtils.CalculateNiyatiSymbol(8, 10));
        }

        [Test]
        public void NiyatiSymbol_Below0Point8_LotusWithered()
        {
            Assert.AreEqual(NiyatiSymbol.LotusWithered, StatUtils.CalculateNiyatiSymbol(7, 10));
        }

        [Test]
        public void NiyatiSymbol_ZeroBase_ReturnsHalf()
        {
            Assert.AreEqual(NiyatiSymbol.LotusHalf, StatUtils.CalculateNiyatiSymbol(5, 0));
        }

        [Test]
        public void CanRescue_ConDifferenceWithin10_True()
        {
            Assert.IsTrue(StatUtils.CanRescue(1, 11));
        }

        [Test]
        public void CanRescue_ConDifferenceBeyond10_False()
        {
            Assert.IsFalse(StatUtils.CanRescue(1, 12));
        }

        [Test]
        public void CanRescue_EqualCon_True()
        {
            Assert.IsTrue(StatUtils.CanRescue(5, 5));
        }

        [Test]
        public void LevelUpGains_ConNeverGrows()
        {
            var growths = StatArray.From(50, 50, 50, 50, 50, 50, 50, 100, 50);
            var mods = StatArray.Create();
            var stats = StatArray.From(20, 5, 5, 5, 5, 5, 5, 7, 5);
            var caps = StatArray.From(60, 30, 30, 30, 30, 30, 30, 20, 30);

            var gains = StatUtils.ComputeLevelUpGains(growths, mods, stats, caps, 2, AlwaysSucceed);
            Assert.AreEqual(0, gains[StatIndex.Con]);
        }

        [Test]
        public void LevelUpGains_HPGains2PerRoll()
        {
            var growths = StatArray.From(100, 0, 0, 0, 0, 0, 0, 0, 0);
            var mods = StatArray.Create();
            var stats = StatArray.From(20, 0, 0, 0, 0, 0, 0, 0, 0);
            var caps = StatArray.From(60, 30, 30, 30, 30, 30, 30, 20, 30);

            var gains = StatUtils.ComputeLevelUpGains(growths, mods, stats, caps, 2, AlwaysSucceed);
            Assert.AreEqual(2, gains[StatIndex.HP]);
        }

        [Test]
        public void LevelUpGains_AtCap_NoGain()
        {
            var growths = StatArray.From(0, 100, 0, 0, 0, 0, 0, 0, 0);
            var mods = StatArray.Create();
            var stats = StatArray.From(20, 30, 0, 0, 0, 0, 0, 0, 0);
            var caps = StatArray.From(60, 30, 30, 30, 30, 30, 30, 20, 30);

            var gains = StatUtils.ComputeLevelUpGains(growths, mods, stats, caps, 2, AlwaysSucceed);
            Assert.AreEqual(0, gains[StatIndex.Str]);
        }

        [Test]
        public void LevelUpGains_ClassModifierBoostsGrowth()
        {
            var growths = StatArray.From(0, 40, 0, 0, 0, 0, 0, 0, 0);
            var mods = StatArray.From(0, 20, 0, 0, 0, 0, 0, 0, 0);
            var stats = StatArray.From(20, 5, 0, 0, 0, 0, 0, 0, 0);
            var caps = StatArray.From(60, 30, 30, 30, 30, 30, 30, 20, 30);

            // Roll value 55: fails at 40 personal, succeeds at 60 effective (40+20)
            var gains = StatUtils.ComputeLevelUpGains(growths, mods, stats, caps, 2,
                (rate, _) => 55 < rate);
            Assert.AreEqual(1, gains[StatIndex.Str]);
        }

        [Test]
        public void ClampStatsToCaps_ClampsExcessValues()
        {
            var stats = StatArray.From(70, 35, 10, 5, 5, 5, 5, 5, 5);
            var caps = StatArray.From(60, 30, 30, 30, 30, 30, 30, 20, 30);

            StatUtils.ClampStatsToCaps(ref stats, caps);
            Assert.AreEqual(60, stats[StatIndex.HP]);
            Assert.AreEqual(30, stats[StatIndex.Str]);
            Assert.AreEqual(10, stats[StatIndex.Mag]);
        }

        private static bool AlwaysSucceed(int rate, int unused) => true;
    }
}
