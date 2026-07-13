using NUnit.Framework;
using ProjectAstra.Core.Combat;
using ProjectAstra.Core.Stats;

namespace ProjectAstra.Core.Tests.Combat
{
    // Pins the contract between ScriptedCombatRng's outcome queue and CombatRound's
    // roll pattern (two hit rolls per swing, crit roll only after a landed hit).
    // This is the mechanism Map 1's deterministic tutorial combat rides on.
    [TestFixture]
    public class ScriptedCombatRngTests
    {
        [Test]
        public void QueuedHitThenMiss_AttackerLands_CounterWhiffs()
        {
            var atk = MakeCombatant(hp: 20, str: 10, spd: 8, def: 5, weapon: TestItems.IronSword);
            var def = MakeCombatant(hp: 20, str: 10, spd: 8, def: 5, weapon: TestItems.IronSword);

            var result = Resolve(atk, def, SwingOutcome.Hit, SwingOutcome.Miss);

            Assert.AreEqual(2, result.Hits.Count);
            Assert.IsTrue(result.Hits[0].Hit);
            Assert.IsFalse(result.Hits[0].Crit);
            Assert.IsFalse(result.Hits[1].Hit);
        }

        [Test]
        public void QueuedCrit_LandsWithTripleDamage()
        {
            int plainDamage = SoloSwing(SwingOutcome.Hit).Damage;
            var critHit = SoloSwing(SwingOutcome.Crit);

            Assert.IsTrue(critHit.Hit);
            Assert.IsTrue(critHit.Crit);
            Assert.Greater(plainDamage, 0);
            Assert.AreEqual(plainDamage * 3, critHit.Damage);
        }

        [Test]
        public void ExhaustedQueue_EverySwingHitsWithoutCrit()
        {
            var atk = MakeCombatant(hp: 50, str: 8, spd: 14, def: 5, weapon: TestItems.IronSword);
            var def = MakeCombatant(hp: 50, str: 8, spd: 8, def: 5, weapon: TestItems.IronSword);

            // Empty queue across attack, counter, and double — the off-script default.
            var result = Resolve(atk, def);

            Assert.AreEqual(3, result.Hits.Count);
            foreach (var hit in result.Hits)
            {
                Assert.IsTrue(hit.Hit);
                Assert.IsFalse(hit.Crit);
            }
        }

        [Test]
        public void MissedSwingSkipsCritRoll_NextSwingStaysAligned()
        {
            var atk = MakeCombatant(hp: 20, str: 10, spd: 8, def: 5, weapon: TestItems.IronSword);
            var def = MakeCombatant(hp: 20, str: 10, spd: 8, def: 5, weapon: TestItems.IronSword);

            // A miss consumes two rolls, a hit three; a desynced state machine
            // would bleed the miss into the counter's rolls.
            var result = Resolve(atk, def, SwingOutcome.Miss, SwingOutcome.Hit);

            Assert.AreEqual(2, result.Hits.Count);
            Assert.IsFalse(result.Hits[0].Hit);
            Assert.IsTrue(result.Hits[1].Hit);
            Assert.IsFalse(result.Hits[1].Crit);
        }

        [Test]
        public void QueueOutcomes_ReplacesLeftoversFromPriorCombat()
        {
            var rng = new ScriptedCombatRng();
            var atk = MakeCombatant(hp: 20, str: 10, spd: 8, def: 5, weapon: TestItems.IronSword);
            var unarmed = MakeCombatant(hp: 20, str: 10, spd: 8, def: 5);

            rng.QueueOutcomes(SwingOutcome.Miss, SwingOutcome.Miss);
            var first = CombatRound.Resolve(atk, unarmed, 0, 0, 0, 0, rng);
            rng.QueueOutcomes(SwingOutcome.Hit);
            var second = CombatRound.Resolve(atk, unarmed, 0, 0, 0, 0, rng);

            Assert.IsFalse(first.Hits[0].Hit);
            Assert.IsTrue(second.Hits[0].Hit);
        }

        // --- Helpers ---

        private static CombatResult Resolve(CombatantData atk, CombatantData def, params SwingOutcome[] outcomes)
        {
            var rng = new ScriptedCombatRng();
            rng.QueueOutcomes(outcomes);
            return CombatRound.Resolve(atk, def, 0, 0, 0, 0, rng);
        }

        // One forced swing against an unarmed target (no counter, no double).
        // skl 10 keeps the attacker's crit rate above zero — a 0% crit can't be forced.
        private static HitResult SoloSwing(SwingOutcome outcome)
        {
            var atk = MakeCombatant(hp: 20, str: 10, spd: 8, def: 5, skl: 10, weapon: TestItems.IronSword);
            var unarmed = MakeCombatant(hp: 50, str: 10, spd: 8, def: 5);
            return Resolve(atk, unarmed, outcome).Hits[0];
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
