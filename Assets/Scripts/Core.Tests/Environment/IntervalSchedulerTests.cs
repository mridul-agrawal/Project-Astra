using NUnit.Framework;
using UnityEngine;
using ProjectAstra.Core.Environment;

namespace ProjectAstra.Core.Tests.Environment
{
    [TestFixture]
    public class IntervalSchedulerTests
    {
        [Test]
        public void DoesNotFireBeforeTheInterval()
        {
            Random.InitState(1);
            var s = new IntervalScheduler();
            s.Reset(2f, 2f);                         // fixed 2-second interval
            Assert.IsFalse(s.Tick(1f, 2f, 2f));      // 1s elapsed — not yet
        }

        [Test]
        public void FiresWhenTheIntervalElapses()
        {
            var s = new IntervalScheduler();
            s.Reset(2f, 2f);
            s.Tick(1f, 2f, 2f);
            Assert.IsTrue(s.Tick(1.5f, 2f, 2f));     // crosses 2s → fires
        }

        [Test]
        public void RepeatsAfterFiring()
        {
            var s = new IntervalScheduler();
            s.Reset(1f, 1f);
            Assert.IsTrue(s.Tick(1f, 1f, 1f));       // first fire
            Assert.IsFalse(s.Tick(0.5f, 1f, 1f));    // rescheduled — not yet
            Assert.IsTrue(s.Tick(0.6f, 1f, 1f));     // fires again
        }

        [Test]
        public void RescheduledIntervalStaysWithinRange()
        {
            Random.InitState(12345);
            var s = new IntervalScheduler();
            s.Reset(3f, 5f);
            // Fire, then a tick just past the minimum must not always fire (interval is 3..5).
            // Drive it long enough to guarantee a fire, then confirm it doesn't immediately re-fire under the min.
            s.Tick(10f, 3f, 5f);                     // definitely fires, reschedules to [3,5]
            Assert.IsFalse(s.Tick(2.9f, 3f, 5f));    // 2.9 < min 3 → cannot have fired yet
        }
    }
}
