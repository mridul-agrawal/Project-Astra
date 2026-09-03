using System.Collections.Generic;
using NUnit.Framework;
using ProjectAstra.Core.Quests;

namespace ProjectAstra.Core.Tests.Quests
{
    [TestFixture]
    public class ObjectiveTrackerTests
    {
        private static TalkCondition FiveStudents()
        {
            var condition = new TalkCondition();
            condition.Configure(
                TalkCondition.With("talk_kaal", "kaal"),
                TalkCondition.With("talk_aryaman", "aryaman"),
                TalkCondition.With("talk_madhavi", "madhavi"),
                TalkCondition.With("talk_rudrani", "rudrani"),
                TalkCondition.With("talk_gajendra", "gajendra"));
            return condition;
        }

        private ObjectiveTracker tracker;

        [SetUp]
        public void SetUp() => tracker = FiveStudents().CreateTracker();

        private static List<string> Listed(IEnumerable<string> items) => new(items);

        // Crediting

        [Test]
        public void AMatchingConversation_IsCredited()
        {
            Assert.IsTrue(tracker.TryCredit(GameplaySignal.Conversation("talk_kaal")));
            Assert.AreEqual(1, tracker.Current);
        }

        [Test]
        public void TheSameConversationTwice_CountsOnce()
        {
            tracker.TryCredit(GameplaySignal.Conversation("talk_kaal"));

            Assert.IsFalse(tracker.TryCredit(GameplaySignal.Conversation("talk_kaal")));
            Assert.AreEqual(1, tracker.Current);
        }

        [Test]
        public void AConversationItIsNotWaitingOn_IsIgnored()
        {
            Assert.IsFalse(tracker.TryCredit(GameplaySignal.Conversation("talk_guru")));
            Assert.AreEqual(0, tracker.Current);
        }

        [Test]
        public void AnInspectionOfTheSameName_IsIgnored()
        {
            Assert.IsFalse(tracker.TryCredit(GameplaySignal.Inspection("talk_kaal")));
        }

        [Test]
        public void ProgressNeverPassesTheTotal()
        {
            foreach (string id in tracker.Outstanding()) tracker.TryCredit(GameplaySignal.Conversation(id));

            Assert.AreEqual(5, tracker.Current);
            Assert.IsFalse(tracker.TryCredit(GameplaySignal.Conversation("talk_kaal")));
            Assert.AreEqual(5, tracker.Current);
        }

        [Test]
        public void ItIsSatisfiedOnlyAtTheTotal()
        {
            tracker.TryCredit(GameplaySignal.Conversation("talk_kaal"));
            Assert.IsFalse(tracker.IsSatisfied);

            foreach (string id in Listed(tracker.Outstanding()))
                tracker.TryCredit(GameplaySignal.Conversation(id));

            Assert.IsTrue(tracker.IsSatisfied);
        }

        // What is left

        [Test]
        public void OutstandingShrinksAsTargetsAreCredited()
        {
            Assert.AreEqual(5, Listed(tracker.Outstanding()).Count);

            tracker.TryCredit(GameplaySignal.Conversation("talk_madhavi"));

            List<string> left = Listed(tracker.Outstanding());
            Assert.AreEqual(4, left.Count);
            CollectionAssert.DoesNotContain(left, "talk_madhavi");
        }

        [Test]
        public void AMarkerPointsAtTheCharacterWhoseConversationItIs()
        {
            Assert.AreEqual("rudrani", tracker.MarkerFor("talk_rudrani"));
        }

        // Save and load

        [Test]
        public void RestorePutsTheExactSetBack()
        {
            tracker.Restore(new[] { "talk_kaal", "talk_gajendra" });

            Assert.AreEqual(2, tracker.Current);
            Assert.IsTrue(tracker.HasCredited("talk_kaal"));
            Assert.IsTrue(tracker.HasCredited("talk_gajendra"));
        }

        [Test]
        public void RestoreDropsAnIdThisConditionDoesNotKnow()
        {
            tracker.Restore(new[] { "talk_kaal", "talk_someone_who_left" });

            Assert.AreEqual(1, tracker.Current);
        }

        [Test]
        public void RestoreDropsADuplicate()
        {
            tracker.Restore(new[] { "talk_kaal", "talk_kaal" });

            Assert.AreEqual(1, tracker.Current);
        }
    }
}
