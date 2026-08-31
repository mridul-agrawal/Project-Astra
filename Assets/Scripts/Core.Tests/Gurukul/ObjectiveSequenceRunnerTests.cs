using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;
using ProjectAstra.Core.Animation;
using ProjectAstra.Core.Gurukul;

namespace ProjectAstra.Core.Tests.Gurukul
{
    // Pins the rules the GDD is strictest about: credit lands once per unique target, only for the
    // objective that is active, and an objective's effects apply exactly once when it completes.
    [TestFixture]
    public class ObjectiveSequenceRunnerTests
    {
        private readonly List<GurukulObjectiveData> created = new();

        [TearDown]
        public void TearDown()
        {
            foreach (var objective in created)
                Object.DestroyImmediate(objective);
            created.Clear();
        }

        private GurukulObjectiveData Objective(string id, GurukulConditionKind kind, string[] targets,
            bool showCounter = false, GurukulEffect[] effects = null, string[] markers = null)
        {
            var condition = new GurukulCondition { kind = kind, targetIds = targets, showCounter = showCounter };
            var objective = GurukulObjectiveData.CreateForTest(id, condition, effects, markers);
            created.Add(objective);
            return objective;
        }

        private static GurukulObjectiveData[] Sequence(params GurukulObjectiveData[] objectives) => objectives;

        private static (ObjectiveSequenceRunner runner, GurukulRuntimeState state) Start(params GurukulObjectiveData[] objectives)
        {
            var state = new GurukulRuntimeState("hub1");
            var runner = new ObjectiveSequenceRunner(Sequence(objectives), state);
            runner.Begin();
            return (runner, state);
        }

        private static void Talk(ObjectiveSequenceRunner runner, string conversationId) =>
            runner.Report(GurukulConditionKind.ConversationCompleted, conversationId);

        // Counters

        [Test]
        public void Counter_CreditsOncePerUniqueTarget()
        {
            var (runner, _) = Start(Objective("talk", GurukulConditionKind.ConversationCompleted,
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
            var (runner, _) = Start(Objective("talk", GurukulConditionKind.ConversationCompleted,
                new[] { "a", "b", "c" }));

            Talk(runner, "a");
            bool secondReport = runner.Report(GurukulConditionKind.ConversationCompleted, "a");

            Assert.IsFalse(secondReport, "A target already cleared must not be credited again.");
            Assert.AreEqual(1, runner.CurrentProgress);
        }

        [Test]
        public void Counter_IgnoresATargetThatIsNotInTheSet()
        {
            var (runner, _) = Start(Objective("talk", GurukulConditionKind.ConversationCompleted, new[] { "a" }));

            bool credited = runner.Report(GurukulConditionKind.ConversationCompleted, "stranger");

            Assert.IsFalse(credited);
            Assert.AreEqual(0, runner.CurrentProgress);
        }

        [Test]
        public void Counter_IgnoresTheRightIdReportedAsTheWrongKind()
        {
            var (runner, _) = Start(Objective("talk", GurukulConditionKind.ConversationCompleted, new[] { "a" }));

            bool credited = runner.Report(GurukulConditionKind.ObjectInspected, "a");

            Assert.IsFalse(credited);
            Assert.AreEqual(0, runner.CurrentProgress);
        }

        [Test]
        public void CompletedTargetSet_IsPreservedNotJustTheCount()
        {
            var (runner, state) = Start(Objective("talk", GurukulConditionKind.ConversationCompleted,
                new[] { "a", "b", "c" }));

            Talk(runner, "c");
            Talk(runner, "a");

            CollectionAssert.AreEquivalent(new[] { "c", "a" }, state.SatisfiedTargets);
        }

        // Sequencing

        [Test]
        public void ClearingEveryTarget_ActivatesTheNextObjective()
        {
            var first = Objective("talk", GurukulConditionKind.ConversationCompleted, new[] { "a", "b" });
            var second = Objective("collect", GurukulConditionKind.ObjectInspected, new[] { "card" });
            var (runner, _) = Start(first, second);

            Talk(runner, "a");
            Talk(runner, "b");

            Assert.AreSame(second, runner.ActiveObjective);
        }

        [Test]
        public void AdvancingToTheNextObjective_ResetsProgress()
        {
            var first = Objective("talk", GurukulConditionKind.ConversationCompleted, new[] { "a" });
            var second = Objective("collect", GurukulConditionKind.ObjectInspected, new[] { "card", "letter" });
            var (runner, _) = Start(first, second);

            Talk(runner, "a");

            Assert.AreEqual(0, runner.CurrentProgress);
            Assert.AreEqual(2, runner.RequiredProgress);
        }

        [Test]
        public void ALaterObjectivesTarget_EarnsNoCreditWhileAnEarlierOneIsActive()
        {
            var first = Objective("talk", GurukulConditionKind.ConversationCompleted, new[] { "a" });
            var second = Objective("collect", GurukulConditionKind.ObjectInspected, new[] { "card" });
            var (runner, _) = Start(first, second);

            bool credited = runner.Report(GurukulConditionKind.ObjectInspected, "card");

            Assert.IsFalse(credited, "Acting on a later stage's target must not bank early credit.");
            Assert.AreSame(first, runner.ActiveObjective);
        }

        [Test]
        public void ReportingAfterTheLastObjective_DoesNothing()
        {
            var only = Objective("talk", GurukulConditionKind.ConversationCompleted, new[] { "a" });
            var (runner, _) = Start(only);

            Talk(runner, "a");
            bool credited = runner.Report(GurukulConditionKind.ConversationCompleted, "a");

            Assert.IsFalse(credited);
            Assert.IsTrue(runner.IsVisitComplete);
            Assert.IsNull(runner.ActiveObjective);
        }

        [Test]
        public void ObjectiveChanged_AnnouncesTheOpeningObjectiveThenEachNextOne()
        {
            var first = Objective("talk", GurukulConditionKind.ConversationCompleted, new[] { "a" });
            var second = Objective("collect", GurukulConditionKind.ObjectInspected, new[] { "card" });

            var state = new GurukulRuntimeState("hub1");
            var runner = new ObjectiveSequenceRunner(Sequence(first, second), state);
            var announced = new List<GurukulObjectiveData>();
            runner.ObjectiveChanged += announced.Add;

            runner.Begin();
            Talk(runner, "a");

            CollectionAssert.AreEqual(new[] { first, second }, announced);
        }

        [Test]
        public void AnImmediateObjective_ClearsItselfBeforeThePlayerGetsControl()
        {
            var opener = Objective("opening", GurukulConditionKind.Immediate, new string[0],
                effects: new[] { new GurukulEffect { kind = GurukulEffectKind.SetGate, targetId = "guru_door", open = true } });
            var next = Objective("talk", GurukulConditionKind.ConversationCompleted, new[] { "a" });
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
                new GurukulEffect { kind = GurukulEffectKind.SetGate, targetId = "guru_door", open = true },
                new GurukulEffect
                {
                    kind = GurukulEffectKind.RelocateCharacter, targetId = "kaal",
                    locationId = "training_ground", position = new Vector2(4f, 6f), facing = Facing.West
                },
                new GurukulEffect
                {
                    kind = GurukulEffectKind.SetInteractableState, targetId = "blackboard",
                    state = GurukulInteractableState.Available
                }
            };
            var (runner, state) = Start(Objective("talk", GurukulConditionKind.ConversationCompleted,
                new[] { "a" }, effects: effects));

            Talk(runner, "a");

            Assert.IsTrue(state.IsGateOpen("guru_door"));
            Assert.IsTrue(state.TryGetRelocation("kaal", out var moved));
            Assert.AreEqual("training_ground", moved.locationId);
            Assert.AreEqual(new Vector2(4f, 6f), moved.position);
            Assert.AreEqual(Facing.West, moved.facing);
            Assert.AreEqual(GurukulInteractableState.Available,
                state.GetInteractableState("blackboard", GurukulInteractableState.Inactive));
        }

        [Test]
        public void EffectsApplyExactlyOnce_EvenWhenTheLastTargetIsReportedAgain()
        {
            int fired = 0;
            var effects = new[] { new GurukulEffect { kind = GurukulEffectKind.FireEvent, valueId = "training_scene" } };
            var only = Objective("talk", GurukulConditionKind.ConversationCompleted, new[] { "a" }, effects: effects);

            var state = new GurukulRuntimeState("hub1");
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
            var (runner, _) = Start(Objective("talk", GurukulConditionKind.ConversationCompleted,
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
            var broken = Objective("talk", GurukulConditionKind.ConversationCompleted, new string[0]);

            Assert.IsFalse(ObjectiveSequenceRunner.IsAuthoredCorrectly(broken, out string problem));
            StringAssert.Contains("never complete", problem);
        }

        [Test]
        public void AWellFormedObjective_PassesValidation()
        {
            var good = Objective("talk", GurukulConditionKind.ConversationCompleted, new[] { "a" });

            Assert.IsTrue(ObjectiveSequenceRunner.IsAuthoredCorrectly(good, out string problem), problem);
        }

        [Test]
        public void AnImmediateObjectiveNeedsNoTargets_AndStillValidates()
        {
            var opener = Objective("opening", GurukulConditionKind.Immediate, new string[0]);

            Assert.IsTrue(ObjectiveSequenceRunner.IsAuthoredCorrectly(opener, out string problem), problem);
        }
    }
}
