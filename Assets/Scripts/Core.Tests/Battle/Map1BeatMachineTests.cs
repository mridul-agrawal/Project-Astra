using NUnit.Framework;
using ProjectAstra.Core.Battle.Map1;

namespace ProjectAstra.Core.Tests.Battle
{
    [TestFixture]
    public class Map1BeatMachineTests
    {
        [Test]
        public void StartsInDefend()
        {
            Assert.AreEqual(Map1Beat.Defend, new Map1BeatMachine().Current);
        }

        [Test]
        public void RaiderDeath_OpensPursue_OnceOnly()
        {
            var beats = new Map1BeatMachine();

            Assert.IsTrue(beats.NotifyRaiderDied());
            Assert.AreEqual(Map1Beat.Pursue, beats.Current);
            Assert.IsFalse(beats.NotifyRaiderDied(), "a second raider death must not re-trigger the beat");
        }

        [Test]
        public void BossDeath_EndsTheMap()
        {
            var beats = new Map1BeatMachine();
            beats.NotifyRaiderDied();

            Assert.IsTrue(beats.NotifyBossDied());
            Assert.AreEqual(Map1Beat.Done, beats.Current);
            Assert.IsFalse(beats.NotifyBossDied());
        }

        [Test]
        public void BossDeathDuringDefend_StillEndsTheMap()
        {
            var beats = new Map1BeatMachine();

            Assert.IsTrue(beats.NotifyBossDied(), "victory is victory even off-script");
            Assert.AreEqual(Map1Beat.Done, beats.Current);
            Assert.IsFalse(beats.NotifyRaiderDied(), "no beat reopens after Done");
        }
    }
}
