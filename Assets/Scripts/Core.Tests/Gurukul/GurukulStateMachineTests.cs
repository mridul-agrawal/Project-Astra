using System;
using System.Collections.Generic;
using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using ProjectAstra.Core.Gurukul;
using ProjectAstra.Core.State;

namespace ProjectAstra.Core.Tests.Gurukul
{
    [TestFixture]
    public class GurukulStateMachineTests
    {
        private GurukulStateMachine machine;

        [SetUp]
        public void SetUp()
        {
            // No manager means "someone pressed Play on the hub scene", which the machine treats as
            // the hub being in control. Cleared explicitly so a manager left behind by another
            // fixture can't decide these tests.
            ClearGameStateManagerInstance();
            machine = new GurukulStateMachine();
        }

        [TearDown]
        public void TearDown() => ClearGameStateManagerInstance();

        private static void ClearGameStateManagerInstance() =>
            typeof(GameStateManager)
                .GetProperty("Instance", BindingFlags.Public | BindingFlags.Static)
                .SetValue(null, null);

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

        [TestCase(GurukulSubState.FreeExploration, GurukulSubState.ScriptedEvent)]
        [TestCase(GurukulSubState.FreeExploration, GurukulSubState.LocationTransition)]
        [TestCase(GurukulSubState.FreeExploration, GurukulSubState.Departure)]
        [TestCase(GurukulSubState.ScriptedEvent, GurukulSubState.LocationTransition)]
        [TestCase(GurukulSubState.ScriptedEvent, GurukulSubState.FreeExploration)]
        [TestCase(GurukulSubState.ScriptedEvent, GurukulSubState.Departure)]
        [TestCase(GurukulSubState.LocationTransition, GurukulSubState.FreeExploration)]
        [TestCase(GurukulSubState.LocationTransition, GurukulSubState.ScriptedEvent)]
        public void LegalTransition_IsAccepted(GurukulSubState from, GurukulSubState to)
        {
            Assert.IsTrue(GurukulStateMachine.IsLegal(from, to), $"Expected {from} -> {to} to be legal");
        }

        // Walking straight out of a doorway into the battle would skip everything the hub still
        // has to settle first.
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

            bool accepted = machine.TryTransition(GurukulSubState.Departure);
            machine.TryTransition(GurukulSubState.FreeExploration);

            Assert.IsTrue(accepted);
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

            machine.TryTransition(GurukulSubState.ScriptedEvent);
            machine.TryTransition(GurukulSubState.LocationTransition);

            CollectionAssert.AreEqual(new[]
            {
                (GurukulSubState.FreeExploration, GurukulSubState.ScriptedEvent),
                (GurukulSubState.ScriptedEvent, GurukulSubState.LocationTransition)
            }, edges);
        }

        [Test]
        public void OnlyFreeExploration_AcceptsMovementAndWorldInteraction()
        {
            Assert.IsTrue(machine.AcceptsMovement);
            Assert.IsTrue(machine.AcceptsWorldInteraction);

            machine.TryTransition(GurukulSubState.ScriptedEvent);

            Assert.IsFalse(machine.AcceptsMovement);
            Assert.IsFalse(machine.AcceptsWorldInteraction);
        }
    }
}
