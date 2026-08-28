using System;
using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using ProjectAstra.Core.Gurukul;

namespace ProjectAstra.Core.Tests.Gurukul
{
    [TestFixture]
    public class GurukulStateMachineTests
    {
        private GurukulStateMachine machine;

        [SetUp]
        public void SetUp() => machine = new GurukulStateMachine();

        [Test]
        public void StartsInFreeExploration()
        {
            Assert.AreEqual(GurukulSubState.FreeExploration, machine.CurrentState);
        }

        [Test]
        public void AVisitCanOpenStraightIntoItsEvent()
        {
            var opening = new GurukulStateMachine(GurukulSubState.ScriptedEvent);

            Assert.AreEqual(GurukulSubState.ScriptedEvent, opening.CurrentState);
        }

        [TestCase(GurukulSubState.FreeExploration, GurukulSubState.Conversation)]
        [TestCase(GurukulSubState.FreeExploration, GurukulSubState.ScriptedEvent)]
        [TestCase(GurukulSubState.FreeExploration, GurukulSubState.LocationTransition)]
        [TestCase(GurukulSubState.FreeExploration, GurukulSubState.Departure)]
        [TestCase(GurukulSubState.Conversation, GurukulSubState.ChoiceOrQuiz)]
        [TestCase(GurukulSubState.Conversation, GurukulSubState.FreeExploration)]
        [TestCase(GurukulSubState.ChoiceOrQuiz, GurukulSubState.Conversation)]
        [TestCase(GurukulSubState.ChoiceOrQuiz, GurukulSubState.Departure)]
        [TestCase(GurukulSubState.ScriptedEvent, GurukulSubState.Conversation)]
        [TestCase(GurukulSubState.ScriptedEvent, GurukulSubState.FreeExploration)]
        [TestCase(GurukulSubState.ScriptedEvent, GurukulSubState.Departure)]
        [TestCase(GurukulSubState.LocationTransition, GurukulSubState.FreeExploration)]
        [TestCase(GurukulSubState.LocationTransition, GurukulSubState.ScriptedEvent)]
        public void LegalTransition_IsAccepted(GurukulSubState from, GurukulSubState to)
        {
            Assert.IsTrue(GurukulStateMachine.IsLegal(from, to), $"Expected {from} -> {to} to be legal");
        }

        // Walking straight into a choice, or into a door from inside a menu, would mean a press
        // could land in two places at once.
        [TestCase(GurukulSubState.FreeExploration, GurukulSubState.ChoiceOrQuiz)]
        [TestCase(GurukulSubState.Conversation, GurukulSubState.LocationTransition)]
        [TestCase(GurukulSubState.ChoiceOrQuiz, GurukulSubState.LocationTransition)]
        [TestCase(GurukulSubState.LocationTransition, GurukulSubState.Conversation)]
        [TestCase(GurukulSubState.LocationTransition, GurukulSubState.Departure)]
        public void IllegalTransition_IsRejected(GurukulSubState from, GurukulSubState to)
        {
            Assert.IsFalse(GurukulStateMachine.IsLegal(from, to), $"Expected {from} -> {to} to be illegal");
        }

        // Once the battle is committed to, nothing may pull the player back into the hub.
        [Test]
        public void Departure_IsTerminal()
        {
            foreach (GurukulSubState state in Enum.GetValues(typeof(GurukulSubState)))
            {
                if (state == GurukulSubState.Departure) continue;
                Assert.IsFalse(GurukulStateMachine.IsLegal(GurukulSubState.Departure, state),
                    $"Departure -> {state} should be illegal");
            }
        }

        [Test]
        public void RejectedTransition_LeavesTheStateAloneAndWarns()
        {
            LogAssert.Expect(LogType.Warning, new System.Text.RegularExpressions.Regex("Illegal transition"));

            bool accepted = machine.TryTransition(GurukulSubState.ChoiceOrQuiz);

            Assert.IsFalse(accepted);
            Assert.AreEqual(GurukulSubState.FreeExploration, machine.CurrentState);
        }

        [Test]
        public void TransitioningToTheCurrentState_IsANoOp()
        {
            bool changed = false;
            machine.StateChanged += (_, _) => changed = true;

            Assert.IsTrue(machine.TryTransition(GurukulSubState.FreeExploration));
            Assert.IsFalse(changed, "Re-entering the same state should not announce a change.");
        }

        [Test]
        public void StateChanged_ReportsBothEndsOfTheEdge()
        {
            var edges = new List<(GurukulSubState, GurukulSubState)>();
            machine.StateChanged += (from, to) => edges.Add((from, to));

            machine.TryTransition(GurukulSubState.Conversation);
            machine.TryTransition(GurukulSubState.ChoiceOrQuiz);

            CollectionAssert.AreEqual(new[]
            {
                (GurukulSubState.FreeExploration, GurukulSubState.Conversation),
                (GurukulSubState.Conversation, GurukulSubState.ChoiceOrQuiz)
            }, edges);
        }

        [Test]
        public void OnlyFreeExploration_AcceptsMovementAndWorldInteraction()
        {
            Assert.IsTrue(machine.AcceptsMovement);
            Assert.IsTrue(machine.AcceptsWorldInteraction);

            machine.TryTransition(GurukulSubState.Conversation);

            Assert.IsFalse(machine.AcceptsMovement);
            Assert.IsFalse(machine.AcceptsWorldInteraction);
        }
    }
}
