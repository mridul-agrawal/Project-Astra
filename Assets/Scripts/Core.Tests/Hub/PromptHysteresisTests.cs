using NUnit.Framework;
using ProjectAstra.Core.Hub;

namespace ProjectAstra.Core.Tests.Hub
{
    [TestFixture]
    public class PromptHysteresisTests
    {
        private const float Delay = 0.1f;

        private PromptHysteresis<string> prompt;

        [SetUp]
        public void SetUp() => prompt = new PromptHysteresis<string>(Delay);

        [Test]
        public void ATargetComingIntoReach_ShowsAtOnce()
        {
            Assert.AreEqual("kaal", prompt.Tick("kaal", 0.016f));
        }

        // The case the spec names: standing on the edge of a target's reach, drifting in and out by
        // a pixel or two, must not strobe the prompt.
        [Test]
        public void AMomentaryDropOut_KeepsThePromptUp()
        {
            prompt.Tick("kaal", 0.016f);

            Assert.AreEqual("kaal", prompt.Tick(null, 0.016f));
            Assert.AreEqual("kaal", prompt.Tick(null, 0.016f));
        }

        [Test]
        public void StayingOutOfReach_EventuallyClearsIt()
        {
            prompt.Tick("kaal", 0.016f);
            prompt.Tick(null, Delay);

            Assert.IsNull(prompt.Current);
        }

        [Test]
        public void ComingBackIntoReach_ResetsTheGracePeriod()
        {
            prompt.Tick("kaal", 0.016f);
            prompt.Tick(null, Delay * 0.9f);
            prompt.Tick("kaal", 0.016f);
            prompt.Tick(null, Delay * 0.9f);

            Assert.AreEqual("kaal", prompt.Current, "Each frame back in reach should start the wait over.");
        }

        // Turning from one target to another is intent, not flicker, so it should not wait.
        [Test]
        public void SwappingStraightToAnotherTarget_IsImmediate()
        {
            prompt.Tick("kaal", 0.016f);

            Assert.AreEqual("shelf", prompt.Tick("shelf", 0.016f));
        }

        [Test]
        public void Clear_DropsItWithNoGracePeriod()
        {
            prompt.Tick("kaal", 0.016f);
            prompt.Clear();

            Assert.IsNull(prompt.Current);
        }

        [Test]
        public void NothingEverInReach_StaysEmpty()
        {
            Assert.IsNull(prompt.Tick(null, 0.016f));
        }
    }
}
