using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;
using ProjectAstra.Core.Quests;

namespace ProjectAstra.Core.Tests.Quests
{
    [TestFixture]
    public class QuestRunnerTests
    {
        private QuestProgress progress;
        private FakeQuestWorld world;
        private QuestRunner runner;
        private List<string> log;
        private readonly List<Object> built = new();

        [SetUp]
        public void SetUp()
        {
            progress = new QuestProgress();
            world = new FakeQuestWorld();
            runner = new QuestRunner(progress, world);
            log = world.Log;

            runner.ObjectiveActivated += s => log.Add($"activated:{s.Objective.ObjectiveId}");
            runner.ObjectiveProgressed += s => log.Add($"progressed:{s.Objective.ObjectiveId}");
            runner.ObjectiveCompleted += o => log.Add($"completed:{o.ObjectiveId}");
            runner.QuestCompleted += q => log.Add($"quest-done:{q.QuestId}");
        }

        [TearDown]
        public void TearDown()
        {
            foreach (Object asset in built) Object.DestroyImmediate(asset);
            built.Clear();
        }

        // --- fixtures ---

        private TalkCondition Talk(params string[] conversationIds)
        {
            var condition = new TalkCondition();
            var targets = new TalkCondition.Target[conversationIds.Length];
            for (int i = 0; i < conversationIds.Length; i++)
                targets[i] = TalkCondition.With(conversationIds[i], conversationIds[i] + "_who");
            condition.Configure(targets);
            return condition;
        }

        private QuestObjective Objective(string id, ObjectiveCondition completion,
            QuestEvent[] onStart = null, QuestEvent[] onComplete = null, bool counter = false)
        {
            QuestObjective objective = QuestObjective.Create(id, completion, id, counter, onStart, onComplete);
            built.Add(objective);
            return objective;
        }

        private QuestData Quest(params QuestObjective[] objectives)
        {
            QuestData quest = QuestData.Create("q", objectives);
            built.Add(quest);
            return quest;
        }

        private static SetFlagEvent Flag(string id)
        {
            var authored = new SetFlagEvent();
            authored.Configure(id);
            return authored;
        }

        // --- starting ---

        [Test]
        public void BeginActivatesTheFirstObjective()
        {
            runner.Begin(Quest(Objective("one", Talk("a")), Objective("two", Talk("b"))));

            Assert.AreEqual("one", runner.ActiveObjective.ObjectiveId);
            CollectionAssert.Contains(log, "activated:one");
        }

        [Test]
        public void BeginRunsTheQuestsOwnOpeningEvents()
        {
            QuestData quest = QuestData.Create("q", new[] { Objective("one", Talk("a")) },
                onStart: new QuestEvent[] { Flag("visit_opened") });
            built.Add(quest);

            runner.Begin(quest);

            Assert.Less(log.IndexOf("flag:visit_opened=True"), log.IndexOf("activated:one"));
        }

        [Test]
        public void ANullQuestDoesNothing()
        {
            runner.Begin(null);

            Assert.IsNull(runner.ActiveObjective);
            Assert.IsFalse(runner.Report(GameplaySignal.Conversation("a")));
        }

        // --- crediting ---

        [Test]
        public void OnlyTheActiveObjectiveIsCredited()
        {
            runner.Begin(Quest(Objective("one", Talk("a")), Objective("two", Talk("b"))));

            Assert.IsFalse(runner.Report(GameplaySignal.Conversation("b")));
            Assert.AreEqual("one", runner.ActiveObjective.ObjectiveId);
        }

        [Test]
        public void ALaterStagesTargetGetsNoEarlyCredit()
        {
            runner.Begin(Quest(Objective("one", Talk("a")), Objective("two", Talk("b"))));
            runner.Report(GameplaySignal.Conversation("b"));
            runner.Report(GameplaySignal.Conversation("a"));

            Assert.AreEqual("two", runner.ActiveObjective.ObjectiveId);
            Assert.AreEqual(0, runner.CurrentProgress);
        }

        [Test]
        public void TheCounterReadsCurrentOverRequired()
        {
            runner.Begin(Quest(Objective("students", Talk("a", "b", "c", "d", "e"), counter: true)));
            runner.Report(GameplaySignal.Conversation("c"));
            runner.Report(GameplaySignal.Conversation("a"));

            Assert.AreEqual(2, runner.CurrentProgress);
            Assert.AreEqual(5, runner.RequiredProgress);
            Assert.IsTrue(runner.ShowsCounter);
        }

        [Test]
        public void ARepeatedTargetDoesNotProgressTheCounter()
        {
            runner.Begin(Quest(Objective("students", Talk("a", "b"))));
            runner.Report(GameplaySignal.Conversation("a"));
            runner.Report(GameplaySignal.Conversation("a"));

            Assert.AreEqual(1, runner.CurrentProgress);
        }

        // --- completing ---

        [Test]
        public void ClearingEveryTargetCompletesTheObjective()
        {
            runner.Begin(Quest(Objective("one", Talk("a", "b")), Objective("two", Talk("c"))));
            runner.Report(GameplaySignal.Conversation("a"));
            runner.Report(GameplaySignal.Conversation("b"));

            Assert.AreEqual("two", runner.ActiveObjective.ObjectiveId);
            CollectionAssert.Contains(log, "completed:one");
        }

        [Test]
        public void EffectsLandBeforeTheNextObjectiveIsAnnounced()
        {
            runner.Begin(Quest(
                Objective("one", Talk("a"), onComplete: new QuestEvent[] { Flag("gate_open") }),
                Objective("two", Talk("b"))));
            runner.Report(GameplaySignal.Conversation("a"));

            Assert.Less(log.IndexOf("flag:gate_open=True"), log.IndexOf("completed:one"));
            Assert.Less(log.IndexOf("completed:one"), log.IndexOf("activated:two"));
        }

        [Test]
        public void CompletionIsAppliedOnceEvenWhenTheSignalRepeats()
        {
            runner.Begin(Quest(Objective("one", Talk("a")), Objective("two", Talk("b"))));
            runner.Report(GameplaySignal.Conversation("a"));
            runner.Report(GameplaySignal.Conversation("a"));

            Assert.AreEqual(1, log.FindAll(entry => entry == "completed:one").Count);
        }

        [Test]
        public void TwoSignalsInOneBreathDoNotSkipAStage()
        {
            runner.Begin(Quest(Objective("one", Talk("a")), Objective("two", Talk("b")),
                Objective("three", Talk("c"))));
            runner.Report(GameplaySignal.Conversation("a"));
            runner.Report(GameplaySignal.Conversation("b"));

            Assert.AreEqual("three", runner.ActiveObjective.ObjectiveId);
            CollectionAssert.Contains(log, "completed:one");
            CollectionAssert.Contains(log, "completed:two");
        }

        [Test]
        public void SheCannotGoBackToACompletedStage()
        {
            runner.Begin(Quest(Objective("one", Talk("a")), Objective("two", Talk("b"))));
            runner.Report(GameplaySignal.Conversation("a"));
            runner.Report(GameplaySignal.Conversation("a"));

            Assert.AreEqual("two", runner.ActiveObjective.ObjectiveId);
            Assert.IsTrue(progress.IsObjectiveCompleted("one"));
        }

        // --- finishing ---

        [Test]
        public void TheLastStageFinishesTheQuest()
        {
            runner.Begin(Quest(Objective("one", Talk("a"))));
            runner.Report(GameplaySignal.Conversation("a"));

            Assert.IsTrue(runner.IsQuestComplete);
            Assert.IsNull(runner.ActiveObjective);
            CollectionAssert.Contains(log, "quest-done:q");
        }

        [Test]
        public void AFinishedQuestTakesNoMoreReports()
        {
            runner.Begin(Quest(Objective("one", Talk("a"))));
            runner.Report(GameplaySignal.Conversation("a"));

            Assert.IsFalse(runner.Report(GameplaySignal.Conversation("a")));
        }

        // --- stages that finish the instant they start ---

        [Test]
        public void AnImmediateStageSettlesOnActivation()
        {
            runner.Begin(Quest(
                Objective("opening", new ImmediateCondition(),
                    onComplete: new QuestEvent[] { Flag("opened") }),
                Objective("one", Talk("a"))));

            Assert.AreEqual("one", runner.ActiveObjective.ObjectiveId);
            CollectionAssert.Contains(log, "flag:opened=True");
        }

        [Test]
        public void ARunOfImmediateStagesSettlesTogether()
        {
            runner.Begin(Quest(
                Objective("first", new ImmediateCondition()),
                Objective("second", new ImmediateCondition()),
                Objective("real", Talk("a"))));

            Assert.AreEqual("real", runner.ActiveObjective.ObjectiveId);
            Assert.IsTrue(progress.IsObjectiveCompleted("first"));
            Assert.IsTrue(progress.IsObjectiveCompleted("second"));
        }

        [Test]
        public void AQuestOfOnlyImmediateStagesFinishes()
        {
            runner.Begin(Quest(Objective("only", new ImmediateCondition())));

            Assert.IsTrue(runner.IsQuestComplete);
            CollectionAssert.Contains(log, "quest-done:q");
        }

        // --- markers ---

        [Test]
        public void OutstandingTargetsAreTheOnesStillToDo()
        {
            runner.Begin(Quest(Objective("students", Talk("a", "b", "c"))));
            runner.Report(GameplaySignal.Conversation("b"));

            var left = new List<string>(runner.OutstandingTargets());
            CollectionAssert.AreEquivalent(new[] { "a", "c" }, left);
        }

        [Test]
        public void AMarkerResolvesToTheCharacter()
        {
            runner.Begin(Quest(Objective("students", Talk("a"))));

            Assert.AreEqual("a_who", runner.MarkerFor("a"));
        }

        // --- persistence ---

        [Test]
        public void ResumePutsBackTheStageAndItsExactCompletedSet()
        {
            runner.Begin(Quest(Objective("students", Talk("a", "b", "c")), Objective("two", Talk("d"))));
            runner.Report(GameplaySignal.Conversation("a"));
            runner.Report(GameplaySignal.Conversation("c"));
            QuestProgressDto saved = progress.Serialize();

            QuestRunner reloaded = Reloaded(saved,
                Quest(Objective("students", Talk("a", "b", "c")), Objective("two", Talk("d"))));

            Assert.AreEqual("students", reloaded.ActiveObjective.ObjectiveId);
            Assert.AreEqual(2, reloaded.CurrentProgress);
            CollectionAssert.AreEquivalent(new[] { "b" }, new List<string>(reloaded.OutstandingTargets()));
        }

        [Test]
        public void ResumeComesBackOnTheStageSheHadReached()
        {
            runner.Begin(Quest(Objective("one", Talk("a")), Objective("two", Talk("b"))));
            runner.Report(GameplaySignal.Conversation("a"));
            QuestProgressDto saved = progress.Serialize();

            QuestRunner reloaded = Reloaded(saved,
                Quest(Objective("one", Talk("a")), Objective("two", Talk("b"))));

            Assert.AreEqual("two", reloaded.ActiveObjective.ObjectiveId);
            Assert.AreEqual(0, reloaded.CurrentProgress);
        }

        [Test]
        public void ResumeDoesNotReplayTheStagesOpeningEvents()
        {
            QuestObjective opener = Objective("one", Talk("a"), onStart: new QuestEvent[] { Flag("cue") });
            runner.Begin(Quest(opener));
            log.Clear();

            Reloaded(progress.Serialize(), Quest(Objective("one", Talk("a"),
                onStart: new QuestEvent[] { Flag("cue") })));

            CollectionAssert.DoesNotContain(log, "flag:cue=True");
        }

        private QuestRunner Reloaded(QuestProgressDto saved, QuestData quest)
        {
            var reloadedProgress = new QuestProgress();
            var reloadedRunner = new QuestRunner(reloadedProgress, world);
            reloadedRunner.Resume(quest, saved);
            return reloadedRunner;
        }
    }
}
