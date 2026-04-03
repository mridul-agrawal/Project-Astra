using NUnit.Framework;
using ProjectAstra.Core;

namespace ProjectAstra.Core.Tests
{
    [TestFixture]
    public class BattlePhaseManagerTests
    {
        [Test]
        public void InitialPhase_IsPlayerPhase()
        {
            var manager = new BattlePhaseManager(hasAllies: false);
            Assert.AreEqual(BattlePhase.PlayerPhase, manager.CurrentPhase);
        }

        [Test]
        public void AdvancePhase_WithAllies_CyclesAllThreePhases()
        {
            var manager = new BattlePhaseManager(hasAllies: true);

            Assert.AreEqual(BattlePhase.PlayerPhase, manager.CurrentPhase);

            manager.AdvancePhase();
            Assert.AreEqual(BattlePhase.EnemyPhase, manager.CurrentPhase);

            manager.AdvancePhase();
            Assert.AreEqual(BattlePhase.AlliedPhase, manager.CurrentPhase);

            manager.AdvancePhase();
            Assert.AreEqual(BattlePhase.PlayerPhase, manager.CurrentPhase);
        }

        [Test]
        public void AdvancePhase_WithoutAllies_SkipsAlliedPhase()
        {
            var manager = new BattlePhaseManager(hasAllies: false);

            Assert.AreEqual(BattlePhase.PlayerPhase, manager.CurrentPhase);

            manager.AdvancePhase();
            Assert.AreEqual(BattlePhase.EnemyPhase, manager.CurrentPhase);

            manager.AdvancePhase();
            Assert.AreEqual(BattlePhase.PlayerPhase, manager.CurrentPhase);
        }

        [Test]
        public void OnPhaseChanged_FiresWithNewPhase()
        {
            var manager = new BattlePhaseManager(hasAllies: false);
            BattlePhase? received = null;
            manager.OnPhaseChanged += phase => received = phase;

            manager.AdvancePhase();

            Assert.AreEqual(BattlePhase.EnemyPhase, received);
        }

        [Test]
        public void Reset_ReturnsToPlayerPhase()
        {
            var manager = new BattlePhaseManager(hasAllies: false);
            manager.AdvancePhase(); // now EnemyPhase

            manager.Reset();

            Assert.AreEqual(BattlePhase.PlayerPhase, manager.CurrentPhase);
        }

        [Test]
        public void SetHasAllies_ChangesPhaseRouting_MidBattle()
        {
            var manager = new BattlePhaseManager(hasAllies: false);

            manager.AdvancePhase(); // EnemyPhase
            manager.SetHasAllies(true);
            manager.AdvancePhase(); // AlliedPhase (now has allies)

            Assert.AreEqual(BattlePhase.AlliedPhase, manager.CurrentPhase);
        }

        [Test]
        public void MultipleCycles_WithAllies_RemainConsistent()
        {
            var manager = new BattlePhaseManager(hasAllies: true);

            for (int cycle = 0; cycle < 3; cycle++)
            {
                Assert.AreEqual(BattlePhase.PlayerPhase, manager.CurrentPhase,
                    $"Cycle {cycle}: expected PlayerPhase at start");

                manager.AdvancePhase();
                Assert.AreEqual(BattlePhase.EnemyPhase, manager.CurrentPhase);

                manager.AdvancePhase();
                Assert.AreEqual(BattlePhase.AlliedPhase, manager.CurrentPhase);

                manager.AdvancePhase();
            }
        }
    }
}
