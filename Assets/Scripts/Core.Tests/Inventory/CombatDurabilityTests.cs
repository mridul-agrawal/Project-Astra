using NUnit.Framework;
using ProjectAstra.Core;

namespace ProjectAstra.Core.Tests.Inventory
{
    /// <summary>
    /// Verifies CombatRound now reports AttackerFired/DefenderFired so the inventory
    /// system can decrement durability after each combat round.
    /// </summary>
    [TestFixture]
    public class CombatDurabilityTests
    {
        [Test]
        public void Resolve_AttackerWithWeapon_AttackerFiredTrue()
        {
            var atk = MakeCombatant(20, 8, 8, 5, 10, 5, weapon: WeaponData.IronSword);
            var def = MakeCombatant(20, 8, 8, 5, 5, 3, weapon: WeaponData.IronLance);

            var rng = new FixedRng(0, 0, 99, 0, 0, 99);
            var result = CombatRound.Resolve(atk, def, 0, 0, 0, 0, rng);

            Assert.IsTrue(result.AttackerFired);
        }

        [Test]
        public void Resolve_DefenderInRangeWithWeapon_DefenderFiredTrue()
        {
            var atk = MakeCombatant(20, 8, 8, 5, 10, 5, weapon: WeaponData.IronSword);
            var def = MakeCombatant(20, 8, 8, 5, 5, 3, weapon: WeaponData.IronLance);

            var rng = new FixedRng(0, 0, 99, 0, 0, 99);
            var result = CombatRound.Resolve(atk, def, 0, 0, 0, 0, rng);

            Assert.IsTrue(result.DefenderFired);
        }

        [Test]
        public void Resolve_DefenderUnarmed_DefenderFiredFalse()
        {
            var atk = MakeCombatant(20, 8, 8, 5, 10, 5, weapon: WeaponData.IronSword);
            var def = MakeCombatant(20, 8, 8, 5, 5, 3, weapon: WeaponData.None);

            var rng = new FixedRng(0, 0, 99);
            var result = CombatRound.Resolve(atk, def, 0, 0, 0, 0, rng);

            Assert.IsFalse(result.DefenderFired);
        }

        [Test]
        public void Resolve_DefenderOutOfRange_DefenderFiredFalse()
        {
            // Bow attacker at distance 2 vs sword defender (range 1) — defender cannot counter.
            var bow = WeaponData.IronBow;
            var sword = WeaponData.IronSword;

            var atkStats = StatArray.From(20, 8, 0, 5, 10, 5, 2, 7, 3);
            var defStats = StatArray.From(20, 8, 0, 5, 5, 3, 2, 7, 3);

            var atk = CombatantData.FromStats(atkStats, 20, 20, bow, distance: 2);
            var def = CombatantData.FromStats(defStats, 20, 20, sword, distance: 2);

            var rng = new FixedRng(0, 0, 99);
            var result = CombatRound.Resolve(atk, def, 0, 0, 0, 0, rng);

            Assert.IsFalse(result.DefenderFired);
        }

        private static CombatantData MakeCombatant(int hp, int str, int spd, int def,
            int skl = 5, int niyati = 3, int mag = 0, int res = 2, int con = 7,
            WeaponData weapon = default)
        {
            var stats = StatArray.From(hp, str, mag, skl, spd, def, res, con, niyati);
            return CombatantData.FromStats(stats, hp, hp, weapon, 1);
        }
    }
}
