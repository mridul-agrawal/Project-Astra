using NUnit.Framework;
using ProjectAstra.Core.Input;

namespace ProjectAstra.Core.Tests.Input
{
    [TestFixture]
    public class InteractLatchTests
    {
        private InteractLatch latch;

        [SetUp]
        public void SetUp() => latch = new InteractLatch();

        [Test]
        public void APress_FiresOnce()
        {
            Assert.IsTrue(latch.Consume(true));
        }

        [Test]
        public void HoldingTheButton_NeverFiresTwice()
        {
            latch.Consume(true);

            Assert.IsFalse(latch.Consume(true));
            Assert.IsFalse(latch.Consume(true));
        }

        [Test]
        public void ReleasingThenPressingAgain_FiresAgain()
        {
            latch.Consume(true);
            latch.Consume(false);

            Assert.IsTrue(latch.Consume(true));
        }

        [Test]
        public void NotHolding_NeverFires()
        {
            Assert.IsFalse(latch.Consume(false));
        }

        // The case the spec calls out: hold confirm through a dialogue's last line and exploration
        // resumes with the button still down. Nothing nearby may activate until it is let go.
        [Test]
        public void SuppressedWhileHeld_WaitsForAReleaseBeforeFiringAgain()
        {
            latch.Suppress();

            Assert.IsFalse(latch.Consume(true), "A button already down when control returned must not count.");
            Assert.IsFalse(latch.Consume(true));

            latch.Consume(false);
            Assert.IsTrue(latch.Consume(true));
        }

        [Test]
        public void StartsArmed_SoTheFirstPressOfAVisitCounts()
        {
            Assert.IsTrue(latch.IsArmed);
        }
    }
}
