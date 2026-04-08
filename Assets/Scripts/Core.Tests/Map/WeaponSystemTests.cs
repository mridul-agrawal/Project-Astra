using NUnit.Framework;
using ProjectAstra.Core;

namespace ProjectAstra.Core.Tests
{
    [TestFixture]
    public class WeaponSystemTests
    {
        #region Effectiveness (WS-06)

        [Test]
        public void Effectiveness_BowVsFlying_TripleMight()
        {
            var bow = WeaponData.IronBow;
            int effective = CombatEngine.ComputeEffectiveMight(bow.might, bow, ClassType.Flying);
            Assert.AreEqual(bow.might * 3, effective);
        }

        [Test]
        public void Effectiveness_BowVsInfantry_NormalMight()
        {
            var bow = WeaponData.IronBow;
            int normal = CombatEngine.ComputeEffectiveMight(bow.might, bow, ClassType.Infantry);
            Assert.AreEqual(bow.might, normal);
        }

        [Test]
        public void Effectiveness_SwordNoTargets_NormalMight()
        {
            var sword = WeaponData.IronSword;
            Assert.AreEqual(sword.might, CombatEngine.ComputeEffectiveMight(sword.might, sword, ClassType.Flying));
        }

        [Test]
        public void IsEffectiveAgainst_MatchingTarget_True()
        {
            var bow = WeaponData.IronBow;
            Assert.IsTrue(bow.IsEffectiveAgainst(ClassType.Flying));
        }

        [Test]
        public void IsEffectiveAgainst_NoTargets_False()
        {
            var sword = WeaponData.IronSword;
            Assert.IsFalse(sword.IsEffectiveAgainst(ClassType.Flying));
        }

        #endregion

        #region Brave Weapons (WS-08)

        [Test]
        public void BraveWeapon_TwoHitsBeforeCounter()
        {
            var atkWeapon = WeaponData.BraveSword;
            var defWeapon = WeaponData.IronLance;

            // High con to offset brave sword weight, equal speed to prevent doubles
            var atk = MakeCombatant(20, 8, 8, 5, 10, 5, con: 14, weapon: atkWeapon);
            var def = MakeCombatant(30, 8, 8, 5, 5, 3, con: 14, weapon: defWeapon);

            // 3 hits: brave1(0,0,99), brave2(0,0,99), counter(0,0,99)
            var rng = new FixedRng(0, 0, 99, 0, 0, 99, 0, 0, 99);
            var result = CombatRound.Resolve(atk, def, 0, 0, 0, 0, rng);

            Assert.AreEqual(3, result.Hits.Count);
            Assert.AreEqual("Attacker", result.Hits[0].Attacker);
            Assert.AreEqual("Attacker", result.Hits[1].Attacker);
            Assert.AreEqual("Defender", result.Hits[2].Attacker);
        }

        [Test]
        public void BraveWeapon_DefenderNotBrave_OnlyOneCounter()
        {
            var atkWeapon = WeaponData.IronSword;
            var defWeapon = WeaponData.BraveSword;

            // Equal effective AS to prevent any doubles
            var atk = MakeCombatant(20, 8, 8, 5, 10, 5, con: 14, weapon: atkWeapon);
            var def = MakeCombatant(30, 8, 8, 5, 5, 3, con: 14, weapon: defWeapon);

            var rng = new FixedRng(0, 0, 99, 0, 0, 99);
            var result = CombatRound.Resolve(atk, def, 0, 0, 0, 0, rng);

            Assert.AreEqual(2, result.Hits.Count);
            Assert.AreEqual("Attacker", result.Hits[0].Attacker);
            Assert.AreEqual("Defender", result.Hits[1].Attacker);
        }

        #endregion

        #region Durability (WS-04)

        [Test]
        public void ConsumeDurability_Decrements()
        {
            var weapon = WeaponData.IronSword;
            weapon.ConsumeDurability();
            Assert.AreEqual(44, weapon.currentUses);
        }

        [Test]
        public void ConsumeDurability_Indestructible_NoChange()
        {
            var weapon = WeaponData.IronSword;
            weapon.indestructible = true;
            weapon.ConsumeDurability();
            Assert.AreEqual(45, weapon.currentUses);
        }

        [Test]
        public void IsBroken_AtZeroUses_True()
        {
            var weapon = WeaponData.IronSword;
            weapon.currentUses = 0;
            Assert.IsTrue(weapon.IsBroken);
        }

        [Test]
        public void IsBroken_Indestructible_AlwaysFalse()
        {
            var weapon = WeaponData.IronSword;
            weapon.currentUses = 0;
            weapon.indestructible = true;
            Assert.IsFalse(weapon.IsBroken);
        }

        [Test]
        public void IsBroken_HasUses_False()
        {
            Assert.IsFalse(WeaponData.IronSword.IsBroken);
        }

        #endregion

        #region Weapon Ranks (WS-03)

        [Test]
        public void WeaponRankTracker_InitialRank()
        {
            var tracker = new WeaponRankTracker();
            tracker.InitializeRank(WeaponType.Sword, WeaponRank.E);
            Assert.AreEqual(WeaponRank.E, tracker.GetRank(WeaponType.Sword));
        }

        [Test]
        public void WeaponRankTracker_EToD_After1Wexp()
        {
            var tracker = new WeaponRankTracker();
            tracker.InitializeRank(WeaponType.Sword, WeaponRank.E);
            tracker.AddWexp(WeaponType.Sword);
            Assert.AreEqual(WeaponRank.D, tracker.GetRank(WeaponType.Sword));
        }

        [Test]
        public void WeaponRankTracker_DToC_After40Wexp()
        {
            var tracker = new WeaponRankTracker();
            tracker.InitializeRank(WeaponType.Sword, WeaponRank.D);
            for (int i = 0; i < 39; i++) tracker.AddWexp(WeaponType.Sword);
            Assert.AreEqual(WeaponRank.D, tracker.GetRank(WeaponType.Sword));
            tracker.AddWexp(WeaponType.Sword);
            Assert.AreEqual(WeaponRank.C, tracker.GetRank(WeaponType.Sword));
        }

        [Test]
        public void WeaponRankTracker_CanEquip_RankMet()
        {
            var tracker = new WeaponRankTracker();
            tracker.InitializeRank(WeaponType.Sword, WeaponRank.B);
            Assert.IsTrue(tracker.CanEquip(WeaponData.SilverSword));
        }

        [Test]
        public void WeaponRankTracker_CannotEquip_RankTooLow()
        {
            var tracker = new WeaponRankTracker();
            tracker.InitializeRank(WeaponType.Sword, WeaponRank.C);
            Assert.IsFalse(tracker.CanEquip(WeaponData.SilverSword));
        }

        [Test]
        public void WeaponRankTracker_CannotEquip_NoAccess()
        {
            var tracker = new WeaponRankTracker();
            tracker.InitializeRank(WeaponType.Lance, WeaponRank.S);
            Assert.IsFalse(tracker.CanEquip(WeaponData.IronSword));
        }

        [Test]
        public void WeaponRankTracker_RankUpEvent_Fires()
        {
            var tracker = new WeaponRankTracker();
            tracker.InitializeRank(WeaponType.Sword, WeaponRank.E);
            WeaponRank? firedRank = null;
            tracker.OnRankUp += (type, rank) => firedRank = rank;
            tracker.AddWexp(WeaponType.Sword);
            Assert.AreEqual(WeaponRank.D, firedRank);
        }

        [Test]
        public void WeaponRankTracker_SRank_NoFurtherGrowth()
        {
            var tracker = new WeaponRankTracker();
            tracker.InitializeRank(WeaponType.Sword, WeaponRank.S);
            tracker.AddWexp(WeaponType.Sword, 100);
            Assert.AreEqual(WeaponRank.S, tracker.GetRank(WeaponType.Sword));
        }

        #endregion

        #region Combat Round with Triangle (integration)

        [Test]
        public void CombatRound_LanceVsSword_DefenderHasAdvantage()
        {
            var atk = MakeCombatant(20, 8, 8, 5, 10, 5, weapon: WeaponData.IronSword);
            var def = MakeCombatant(20, 8, 8, 5, 5, 3, weapon: WeaponData.IronLance);

            var rng = new FixedRng(0, 0, 99, 0, 0, 99);
            var result = CombatRound.Resolve(atk, def, 0, 0, 0, 0, rng);

            Assert.AreEqual(-1, result.TriangleAdvantage);
        }

        [Test]
        public void CombatRound_EffectiveWeapon_FlagsResult()
        {
            var atk = MakeCombatant(20, 8, 8, 5, 10, 5, weapon: WeaponData.IronBow);
            var def = MakeCombatant(20, 5, 5, 3, 5, 3, weapon: WeaponData.None);

            var rng = new FixedRng(0, 0, 99);
            var result = CombatRound.Resolve(atk, def, 0, 0, 0, 0, rng,
                defenderClassType: ClassType.Flying);

            Assert.IsTrue(result.AttackerEffective);
        }

        #endregion

        private static CombatantData MakeCombatant(int hp, int str, int spd, int def,
            int skl = 5, int niyati = 3, int mag = 0, int res = 2, int con = 7,
            WeaponData weapon = default)
        {
            var stats = StatArray.From(hp, str, mag, skl, spd, def, res, con, niyati);
            return CombatantData.FromStats(stats, hp, hp, weapon, 1);
        }
    }
}
