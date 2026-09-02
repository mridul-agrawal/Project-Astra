using NUnit.Framework;
using ProjectAstra.Core.Hub;
using ProjectAstra.Core.Hub.Events;

namespace ProjectAstra.Core.Tests.Hub
{
    [TestFixture]
    public class EventQueueGuardTests
    {
        private HubRuntimeState state;
        private EventQueueGuard guard;

        [SetUp]
        public void SetUp()
        {
            state = new HubRuntimeState("hub1");
            guard = new EventQueueGuard(state);
        }

        [Test]
        public void AnEventCanStartWhenNothingElseIsRunning()
        {
            Assert.IsTrue(guard.TryBegin("training_scene", oneTime: true));
            Assert.IsTrue(guard.IsBusy);
        }

        // Two triggers landing on the same frame must not start two events on top of each other.
        [Test]
        public void ASecondEvent_CannotInterruptTheOneRunning()
        {
            guard.TryBegin("training_scene", oneTime: true);

            Assert.IsFalse(guard.TryBegin("another_scene", oneTime: true));
            Assert.AreEqual("training_scene", guard.RunningEventId);
        }

        [Test]
        public void FinishingAnEvent_FreesTheQueue()
        {
            guard.TryBegin("training_scene", oneTime: true);
            guard.Finish("training_scene", oneTime: true);

            Assert.IsFalse(guard.IsBusy);
            Assert.IsTrue(guard.TryBegin("another_scene", oneTime: true));
        }

        // Walking back into an area trigger after its scene has played must not replay it.
        [Test]
        public void AOneTimeEvent_NeverRunsTwice()
        {
            guard.TryBegin("training_scene", oneTime: true);
            guard.Finish("training_scene", oneTime: true);

            Assert.IsFalse(guard.TryBegin("training_scene", oneTime: true));
            Assert.IsTrue(guard.HasCompleted("training_scene"));
        }

        [Test]
        public void ARepeatableEvent_CanRunAgain()
        {
            guard.TryBegin("bark", oneTime: false);
            guard.Finish("bark", oneTime: false);

            Assert.IsTrue(guard.TryBegin("bark", oneTime: false));
            Assert.IsFalse(guard.HasCompleted("bark"));
        }

        // Finishing something that isn't the running event must not free the queue — otherwise a
        // stray call would let a second event start on top of the real one.
        [Test]
        public void FinishingSomethingElse_DoesNotFreeTheQueue()
        {
            guard.TryBegin("training_scene", oneTime: true);
            guard.Finish("some_other_event", oneTime: true);

            Assert.IsTrue(guard.IsBusy);
            Assert.AreEqual("training_scene", guard.RunningEventId);
        }

        [Test]
        public void AnEventWithNoId_CannotStart()
        {
            Assert.IsFalse(guard.TryBegin("", oneTime: true));
        }

        // An event already completed in this visit is refused before it ever reaches the runner.
        [Test]
        public void AnEventAlreadyDoneThisVisit_IsRefusedUpFront()
        {
            state.MarkEventCompleted("training_scene");

            Assert.IsFalse(guard.CanStart("training_scene", oneTime: true));
        }
    }
}
