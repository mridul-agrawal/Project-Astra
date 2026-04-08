using NUnit.Framework;
using ProjectAstra.Core;

namespace ProjectAstra.Core.Tests
{
    [TestFixture]
    public class CombatEngineTests
    {
        #region Hit Rate (CC-04)

        [Test]
        public void AttackerHit_Formula()
        {
            // Skl=10, Niyati=5, WeaponHit=90, WTBonus=15
            Assert.AreEqual(10 * 2 + 5 + 90 + 15, CombatEngine.ComputeAttackerHit(10, 5, 90, 15));
        }

        [Test]
        public void AttackerHit_MinimumZero()
        {
            Assert.AreEqual(0, CombatEngine.ComputeAttackerHit(0, 0, 0, -50));
        }

        #endregion

        #region Avoid (CC-05)

        [Test]
        public void Avoid_Formula()
        {
            // AS=12, Niyati=5, TerrainAvo=10
            Assert.AreEqual(12 * 2 + 5 + 10, CombatEngine.ComputeAvoid(12, 5, 10));
        }

        [Test]
        public void Avoid_MinimumZero()
        {
            Assert.AreEqual(0, CombatEngine.ComputeAvoid(0, 0, 0));
        }

        #endregion

        #region Displayed Hit

        [Test]
        public void DisplayedHit_Clamped0To100()
        {
            Assert.AreEqual(0, CombatEngine.ComputeDisplayedHit(10, 50));
            Assert.AreEqual(100, CombatEngine.ComputeDisplayedHit(150, 20));
            Assert.AreEqual(60, CombatEngine.ComputeDisplayedHit(80, 20));
        }

        #endregion

        #region Damage (CC-06)

        [Test]
        public void PhysicalDamage_Formula()
        {
            // Str=10, Might=5, WTBonus=1, Def=6, TerrainDef=2
            Assert.AreEqual(10 + 5 + 1 - 6 - 2, CombatEngine.ComputePhysicalDamage(10, 5, 1, 6, 2));
        }

        [Test]
        public void PhysicalDamage_MinimumZero()
        {
            Assert.AreEqual(0, CombatEngine.ComputePhysicalDamage(1, 1, 0, 20, 5));
        }

        [Test]
        public void MagicalDamage_NoTerrainDefense()
        {
            // Mag=12, Might=5, MagTriBonus=0, Res=8
            Assert.AreEqual(12 + 5 - 8, CombatEngine.ComputeMagicalDamage(12, 5, 0, 8));
        }

        [Test]
        public void MagicalDamage_MinimumZero()
        {
            Assert.AreEqual(0, CombatEngine.ComputeMagicalDamage(1, 1, 0, 20));
        }

        #endregion

        #region Critical (CC-07)

        [Test]
        public void CritRate_Formula()
        {
            // Skl=14, WeaponCrit=5, ClassCrit=0, DefNiyati=3
            Assert.AreEqual(14 / 2 + 5 + 0 - 3, CombatEngine.ComputeCritRate(14, 5, 0, 3));
        }

        [Test]
        public void CritRate_MinimumZero()
        {
            Assert.AreEqual(0, CombatEngine.ComputeCritRate(2, 0, 0, 20));
        }

        [Test]
        public void CritMultiplier_TriplesDamage()
        {
            Assert.AreEqual(30, CombatEngine.ApplyCritMultiplier(10));
            Assert.AreEqual(0, CombatEngine.ApplyCritMultiplier(0));
        }

        [Test]
        public void IsCrit_BelowRate_True()
        {
            Assert.IsTrue(CombatEngine.IsCrit(10, 9));
        }

        [Test]
        public void IsCrit_AtRate_False()
        {
            Assert.IsFalse(CombatEngine.IsCrit(10, 10));
        }

        [Test]
        public void IsCrit_ZeroRate_AlwaysFalse()
        {
            Assert.IsFalse(CombatEngine.IsCrit(0, 0));
        }

        #endregion

        #region Double Attack (CC-08)

        [Test]
        public void DoubleAttack_DiffExactly4_True()
        {
            Assert.IsTrue(CombatEngine.CanDoubleAttack(14, 10));
        }

        [Test]
        public void DoubleAttack_Diff3_False()
        {
            Assert.IsFalse(CombatEngine.CanDoubleAttack(13, 10));
        }

        [Test]
        public void DoubleAttack_EqualAS_False()
        {
            Assert.IsFalse(CombatEngine.CanDoubleAttack(10, 10));
        }

        #endregion

        #region True Hit (CC-10)

        [Test]
        public void TrueHit_Average_Formula()
        {
            Assert.AreEqual((30 + 70 + 1) / 2, CombatEngine.RollTrueHit(30, 70));
        }

        [Test]
        public void TrueHit_BothZero()
        {
            Assert.AreEqual(0, CombatEngine.RollTrueHit(0, 0));
        }

        [Test]
        public void TrueHit_BothMax()
        {
            Assert.AreEqual(99, CombatEngine.RollTrueHit(99, 99));
        }

        [Test]
        public void IsHit_100Percent_AlwaysHits()
        {
            Assert.IsTrue(CombatEngine.IsHit(100, 99));
        }

        [Test]
        public void IsHit_0Percent_AlwaysMisses()
        {
            Assert.IsFalse(CombatEngine.IsHit(0, 0));
        }

        [Test]
        public void IsHit_DisplayedGreaterThanRoll_Hits()
        {
            Assert.IsTrue(CombatEngine.IsHit(70, 50));
        }

        [Test]
        public void IsHit_DisplayedEqualToRoll_Misses()
        {
            Assert.IsFalse(CombatEngine.IsHit(50, 50));
        }

        #endregion

        #region Combat Round Resolution (CC-09)

        [Test]
        public void Round_AttackerKillsDefender_StopsEarly()
        {
            var atk = MakeCombatant(hp: 20, str: 15, spd: 10, def: 5, skl: 10, niyati: 5, weapon: WeaponData.IronSword);
            var def = MakeCombatant(hp: 5, str: 8, spd: 6, def: 3, skl: 5, niyati: 3, weapon: WeaponData.IronLance);

            // RNG: always hit (0,0 → TrueHit=0), no crit (99)
            var rng = new FixedRng(0, 0, 99);
            var result = CombatRound.Resolve(atk, def, 0, 0, 0, 0, rng);

            Assert.IsTrue(result.DefenderDied);
            Assert.IsFalse(result.AttackerDied);
            Assert.AreEqual(1, result.Hits.Count);
        }

        [Test]
        public void Round_DefenderCounterattacks()
        {
            var atk = MakeCombatant(hp: 20, str: 8, spd: 8, def: 5, skl: 10, niyati: 5, weapon: WeaponData.IronSword);
            var def = MakeCombatant(hp: 20, str: 8, spd: 8, def: 5, skl: 5, niyati: 3, weapon: WeaponData.IronLance);

            // RNG for 2 hits: hit1(0,0,99), hit2(0,0,99)
            var rng = new FixedRng(0, 0, 99, 0, 0, 99);
            var result = CombatRound.Resolve(atk, def, 0, 0, 0, 0, rng);

            Assert.AreEqual(2, result.Hits.Count);
            Assert.AreEqual("Attacker", result.Hits[0].Attacker);
            Assert.AreEqual("Defender", result.Hits[1].Attacker);
        }

        [Test]
        public void Round_AttackerDoubles_WithHighAS()
        {
            var atk = MakeCombatant(hp: 20, str: 8, spd: 14, def: 5, skl: 10, niyati: 5, weapon: WeaponData.IronSword);
            var def = MakeCombatant(hp: 30, str: 8, spd: 8, def: 5, skl: 5, niyati: 3, weapon: WeaponData.IronLance);

            // 3 hits: atk(0,0,99), def counter(0,0,99), atk double(0,0,99)
            var rng = new FixedRng(0, 0, 99, 0, 0, 99, 0, 0, 99);
            var result = CombatRound.Resolve(atk, def, 0, 0, 0, 0, rng);

            Assert.AreEqual(3, result.Hits.Count);
            Assert.AreEqual("Attacker", result.Hits[2].Attacker);
        }

        [Test]
        public void Round_NoCounter_DefenderUnarmed()
        {
            var atk = MakeCombatant(hp: 20, str: 8, spd: 8, def: 5, skl: 10, niyati: 5, weapon: WeaponData.IronSword);
            var def = MakeCombatant(hp: 20, str: 8, spd: 8, def: 5, skl: 5, niyati: 3, weapon: WeaponData.None);

            var rng = new FixedRng(0, 0, 99);
            var result = CombatRound.Resolve(atk, def, 0, 0, 0, 0, rng);

            Assert.AreEqual(1, result.Hits.Count);
        }

        [Test]
        public void Round_Miss_NoDamage()
        {
            var atk = MakeCombatant(hp: 20, str: 8, spd: 8, def: 5, skl: 10, niyati: 5, weapon: WeaponData.IronSword);
            var def = MakeCombatant(hp: 20, str: 8, spd: 8, def: 5, skl: 5, niyati: 3, weapon: WeaponData.IronLance);

            // RNG: both 99 → TrueHit=99, likely miss for moderate hit rates
            var rng = new FixedRng(99, 99, 99, 99, 99, 99);
            var result = CombatRound.Resolve(atk, def, 0, 0, 0, 0, rng);

            foreach (var hit in result.Hits)
                if (!hit.Hit)
                    Assert.AreEqual(0, hit.Damage);
        }

        [Test]
        public void Round_CritTriples_Damage()
        {
            var atk = MakeCombatant(hp: 20, str: 10, spd: 8, def: 5, skl: 20, niyati: 5, weapon: WeaponData.IronSword);
            var def = MakeCombatant(hp: 100, str: 5, spd: 5, def: 5, skl: 5, niyati: 0, weapon: WeaponData.IronLance);

            // Hit succeeds (0,0), crit succeeds (0), defender counter hit(0,0), no crit(99)
            var rng = new FixedRng(0, 0, 0, 0, 0, 99);
            var result = CombatRound.Resolve(atk, def, 0, 0, 0, 0, rng);

            Assert.IsTrue(result.Hits[0].Crit);
            int baseDmg = CombatEngine.ComputePhysicalDamage(10, 5, 0, 5, 0);
            Assert.AreEqual(baseDmg * 3, result.Hits[0].Damage);
        }

        private static CombatantData MakeCombatant(int hp, int str, int spd, int def,
            int skl = 5, int niyati = 3, int mag = 0, int res = 2, int con = 7,
            WeaponData weapon = default)
        {
            var stats = StatArray.From(hp, str, mag, skl, spd, def, res, con, niyati);
            return CombatantData.FromStats(stats, hp, hp, weapon, 1);
        }

        #endregion
    }
}
