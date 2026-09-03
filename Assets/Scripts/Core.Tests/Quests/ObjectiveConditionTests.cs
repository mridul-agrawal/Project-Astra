using NUnit.Framework;
using ProjectAstra.Core.Quests;

namespace ProjectAstra.Core.Tests.Quests
{
    [TestFixture]
    public class ObjectiveConditionTests
    {
        // Talk

        [Test]
        public void TalkMatchesAFinishedConversation()
        {
            var condition = new TalkCondition();
            condition.Configure(TalkCondition.With("talk_kaal", "kaal"));

            Assert.IsTrue(condition.Matches(GameplaySignal.Conversation("talk_kaal"), out string target));
            Assert.AreEqual("talk_kaal", target);
        }

        [Test]
        public void TalkIgnoresAnInspection()
        {
            var condition = new TalkCondition();
            condition.Configure(TalkCondition.With("talk_kaal", "kaal"));

            Assert.IsFalse(condition.Matches(GameplaySignal.Inspection("talk_kaal"), out _));
        }

        [Test]
        public void TalkCountsOncePerConversation()
        {
            var condition = new TalkCondition();
            condition.Configure(
                TalkCondition.With("a", "one"),
                TalkCondition.With("b", "two"),
                TalkCondition.With("c", "three"));

            Assert.AreEqual(3, condition.RequiredCount);
        }

        [Test]
        public void TalkWithNoCharacterHasNoMarker()
        {
            var condition = new TalkCondition();
            condition.Configure(TalkCondition.With("talk_noticeboard"));

            Assert.IsNull(condition.MarkerFor("talk_noticeboard"));
        }

        // Inspect

        [Test]
        public void InspectMatchesTheObjectThatWasLookedAt()
        {
            var condition = new InspectCondition();
            condition.Configure("blackboard");

            Assert.IsTrue(condition.Matches(GameplaySignal.Inspection("blackboard"), out string target));
            Assert.AreEqual("blackboard", target);
        }

        [Test]
        public void InspectMarksTheObjectItself()
        {
            var condition = new InspectCondition();
            condition.Configure("blackboard");

            Assert.AreEqual("blackboard", condition.MarkerFor("blackboard"));
        }

        [Test]
        public void InspectIgnoresAConversation()
        {
            var condition = new InspectCondition();
            condition.Configure("blackboard");

            Assert.IsFalse(condition.Matches(GameplaySignal.Conversation("blackboard"), out _));
        }

        // Signal

        [Test]
        public void SignalMatchesARaisedFlag()
        {
            var condition = new SignalCondition();
            condition.Configure("quiz_passed");

            Assert.IsTrue(condition.Matches(GameplaySignal.Signal("quiz_passed"), out string target));
            Assert.AreEqual("quiz_passed", target);
        }

        [Test]
        public void SignalHasNothingToPointAt()
        {
            var condition = new SignalCondition();
            condition.Configure("quiz_passed");

            Assert.IsNull(condition.MarkerFor("quiz_passed"));
        }

        // Immediate

        [Test]
        public void ImmediateIsSatisfiedFromTheStart()
        {
            Assert.IsTrue(new ImmediateCondition().CreateTracker().IsSatisfied);
        }

        [Test]
        public void ImmediateMatchesNothing()
        {
            Assert.IsFalse(new ImmediateCondition().Matches(GameplaySignal.Signal("anything"), out _));
        }

        // Authoring checks

        [Test]
        public void AConditionWithNoTargetsIsAContentError()
        {
            Assert.IsFalse(new TalkCondition().IsAuthoredCorrectly(out string problem));
            Assert.IsNotNull(problem);
        }

        [Test]
        public void AnImmediateConditionWithNoTargetsIsFine()
        {
            Assert.IsTrue(new ImmediateCondition().IsAuthoredCorrectly(out _));
        }
    }
}
