using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;
using ProjectAstra.Core.Animation;
using ProjectAstra.Core.Hub;

namespace ProjectAstra.Core.Tests.Hub
{
    // Pins the rules the GDD is strictest about: credit lands once per unique target, only for the
    // objective that is active, and an objective's effects apply exactly once when it completes.
    [TestFixture]
    public class ObjectiveSequenceRunnerTests
    {
        private readonly List<HubObjectiveData> created = new();

        [TearDown]
        public void TearDown()
        {
            foreach (var objective in created)
                Object.DestroyImmediate(objective);
            created.Clear();
        }

        private HubObjectiveData Objective(string id, HubConditionKind kind, string[] targets,
            bool showCounter = false, HubEffect[] effects = null, string[] markers = null)
        {
            var condition = new HubCondition { kind = kind, targetIds = targets, showCounter = showCounter };
            var objective = HubObjectiveData.CreateForTest(id, condition, effects, markers);
            created.Add(objective);
            return objective;
        }

        private static HubObjectiveData[] Sequence(params HubObjectiveData[] objectives) => objectives;

        private static (ObjectiveSequenceRunner runner, HubRuntimeState state) Start(params HubObjectiveData[] objectives)
        {
            var state = new HubRuntimeState("hub1");
            var runner = new ObjectiveSequenceRunner(Sequence(objectives), state);
            runner.Begin();
            return (runner, state);
        }

        private static void Talk(ObjectiveSequenceRunner runner, string conversationId) =>
            runner.Report(HubConditionKind.ConversationCompleted, conversationId);

        // Counters

        [Test]
        public void Counter_CreditsOncePerUniqueTarget()
        {
            var (runner, _) = Start(Objective("talk", HubConditionKind.ConversationCompleted,
                new[] { "a", "b", "c" }, showCounter: true));

            Talk(runner, "a");
            Talk(runner, "b");

            Assert.AreEqual(2, runner.CurrentProgress);
            Assert.AreEqual(3, runner.RequiredProgress);
            Assert.IsTrue(runner.ShowsCounter);
        }

        [Test]
        public void Counter_IgnoresARepeatedTarget()
        {
            var (runner, _) = Start(Objective("talk", HubConditionKind.ConversationCompleted,
                new[] { "a", "b", "c" }));

            Talk(runner, "a");
            bool secondReport = runner.Report(HubConditionKind.ConversationCompleted, "a");

            Assert.IsFalse(secondReport, "A target already cleared must not be credited again.");
            Assert.AreEqual(1, runner.CurrentProgress);
        }

        [Test]
        public void Counter_IgnoresATargetThatIsNotInTheSet()
        {
            var (runner, _) = Start(Objective("talk", HubConditionKind.ConversationCompleted, new[] { "a" }));

            bool credited = runner.Report(HubConditionKind.ConversationCompleted, "stranger");

            Assert.IsFalse(credited);
            Assert.AreEqual(0, runner.CurrentProgress);
        }

        [Test]
        public void Counter_IgnoresTheRightIdReportedAsTheWrongKind()
        {
            var (runner, _) = Start(Objective("talk", HubConditionKind.ConversationCompleted, new[] { "a" }));

            bool credited = runner.Report(HubConditionKind.ObjectInspected, "a");

            Assert.IsFalse(credited);
            Assert.AreEqual(0, runner.CurrentProgress);
        }

        [Test]
        public void CompletedTargetSet_IsPreservedNotJustTheCount()
        {
            var (runner, state) = Start(Objective("talk", HubConditionKind.ConversationCompleted,
                new[] { "a", "b", "c" }));

            Talk(runner, "c");
            Talk(runner, "a");

            CollectionAssert.AreEquivalent(new[] { "c", "a" }, state.SatisfiedTargets);
        }

        // Sequencing

        [Test]
        public void ClearingEveryTarget_ActivatesTheNextObjective()
        {
            var first = Objective("talk", HubConditionKind.ConversationCompleted, new[] { "a", "b" });
            var second = Objective("collect", HubConditionKind.ObjectInspected, new[] { "card" });
            var (runner, _) = Start(first, second);

            Talk(runner, "a");
            Talk(runner, "b");

            Assert.AreSame(second, runner.ActiveObjective);
        }

        [Test]
        public void AdvancingToTheNextObjective_ResetsProgress()
        {
            var first = Objective("talk", HubConditionKind.ConversationCompleted, new[] { "a" });
            var second = Objective("collect", HubConditionKind.ObjectInspected, new[] { "card", "letter" });
            var (runner, _) = Start(first, second);

            Talk(runner, "a");

            Assert.AreEqual(0, runner.CurrentProgress);
            Assert.AreEqual(2, runner.RequiredProgress);
        }

        [Test]
        public void ALaterObjectivesTarget_EarnsNoCreditWhileAnEarlierOneIsActive()
        {
            var first = Objective("talk", HubConditionKind.ConversationCompleted, new[] { "a" });
            var second = Objective("collect", HubConditionKind.ObjectInspected, new[] { "card" });
            var (runner, _) = Start(first, second);

            bool credited = runner.Report(HubConditionKind.ObjectInspected, "card");

            Assert.IsFalse(credited, "Acting on a later stage's target must not bank early credit.");
            Assert.AreSame(first, runner.ActiveObjective);
        }

        [Test]
        public void ReportingAfterTheLastObjective_DoesNothing()
        {
            var only = Objective("talk", HubConditionKind.ConversationCompleted, new[] { "a" });
            var (runner, _) = Start(only);

            Talk(runner, "a");
            bool credited = runner.Report(HubConditionKind.ConversationCompleted, "a");

            Assert.IsFalse(credited);
            Assert.IsTrue(runner.IsVisitComplete);
            Assert.IsNull(runner.ActiveObjective);
        }

        [Test]
        public void ObjectiveChanged_AnnouncesTheOpeningObjectiveThenEachNextOne()
        {
            var first = Objective("talk", HubConditionKind.ConversationCompleted, new[] { "a" });
            var second = Objective("collect", HubConditionKind.ObjectInspected, new[] { "card" });

            var state = new HubRuntimeState("hub1");
            var runner = new ObjectiveSequenceRunner(Sequence(first, second), state);
            var announced = new List<HubObjectiveData>();
            runner.ObjectiveChanged += announced.Add;

            runner.Begin();
            Talk(runner, "a");

            CollectionAssert.AreEqual(new[] { first, second }, announced);
        }

        [Test]
        public void AnImmediateObjective_ClearsItselfBeforeThePlayerGetsControl()
        {
            var opener = Objective("opening", HubConditionKind.Immediate, new string[0],
                effects: new[] { new HubEffect { kind = HubEffectKind.SetGate, targetId = "guru_door", open = true } });
            var next = Objective("talk", HubConditionKind.ConversationCompleted, new[] { "a" });
            var (runner, state) = Start(opener, next);

            Assert.AreSame(next, runner.ActiveObjective);
            Assert.IsTrue(state.IsGateOpen("guru_door"));
        }

        // Effects

        [Test]
        public void CompletingAnObjective_AppliesItsEffectsToTheRuntimeState()
        {
            var effects = new[]
            {
                new HubEffect { kind = HubEffectKind.SetGate, targetId = "guru_door", open = true },
                new HubEffect
                {
                    kind = HubEffectKind.RelocateCharacter, targetId = "kaal",
                    locationId = "training_ground", position = new Vector2(4f, 6f), facing = Facing.West
                },
                new HubEffect
                {
                    kind = HubEffectKind.SetInteractableState, targetId = "blackboard",
                    state = HubInteractableState.Available
                }
            };
            var (runner, state) = Start(Objective("talk", HubConditionKind.ConversationCompleted,
                new[] { "a" }, effects: effects));

            Talk(runner, "a");

            Assert.IsTrue(state.IsGateOpen("guru_door"));
            Assert.IsTrue(state.TryGetRelocation("kaal", out var moved));
            Assert.AreEqual("training_ground", moved.locationId);
            Assert.AreEqual(new Vector2(4f, 6f), moved.position);
            Assert.AreEqual(Facing.West, moved.facing);
            Assert.AreEqual(HubInteractableState.Available,
                state.GetInteractableState("blackboard", HubInteractableState.Inactive));
        }

        [Test]
        public void EffectsApplyExactlyOnce_EvenWhenTheLastTargetIsReportedAgain()
        {
            int fired = 0;
            var effects = new[] { new HubEffect { kind = HubEffectKind.FireEvent, valueId = "training_scene" } };
            var only = Objective("talk", HubConditionKind.ConversationCompleted, new[] { "a" }, effects: effects);

            var state = new HubRuntimeState("hub1");
            var runner = new ObjectiveSequenceRunner(Sequence(only), state);
            runner.EventRequested += _ => fired++;
            runner.Begin();

            Talk(runner, "a");
            Talk(runner, "a");

            Assert.AreEqual(1, fired);
        }

        // Markers

        [Test]
        public void AMarkerStaysOnlyWhileItsTargetIsOutstanding()
        {
            var (runner, _) = Start(Objective("talk", HubConditionKind.ConversationCompleted,
                new[] { "a", "b" }, markers: new[] { "a", "b" }));

            Talk(runner, "a");

            Assert.IsFalse(runner.IsMarkerTargetOutstanding("a"), "A cleared target must drop its marker.");
            Assert.IsTrue(runner.IsMarkerTargetOutstanding("b"));
            Assert.IsFalse(runner.IsMarkerTargetOutstanding("unrelated"));
        }

        // Content validation

        [Test]
        public void AConditionWithNoTargets_IsReportedAsAContentError()
        {
            var broken = Objective("talk", HubConditionKind.ConversationCompleted, new string[0]);

            Assert.IsFalse(ObjectiveSequenceRunner.IsAuthoredCorrectly(broken, out string problem));
            StringAssert.Contains("never complete", problem);
        }

        [Test]
        public void AWellFormedObjective_PassesValidation()
        {
            var good = Objective("talk", HubConditionKind.ConversationCompleted, new[] { "a" });

            Assert.IsTrue(ObjectiveSequenceRunner.IsAuthoredCorrectly(good, out string problem), problem);
        }

        [Test]
        public void AnImmediateObjectiveNeedsNoTargets_AndStillValidates()
        {
            var opener = Objective("opening", HubConditionKind.Immediate, new string[0]);

            Assert.IsTrue(ObjectiveSequenceRunner.IsAuthoredCorrectly(opener, out string problem), problem);
        }
    }
}
