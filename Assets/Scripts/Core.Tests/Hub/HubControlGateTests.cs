using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using ProjectAstra.Core.Hub;
using ProjectAstra.Core.State;

namespace ProjectAstra.Core.Tests.Hub
{
    [TestFixture]
    public class HubControlGateTests
    {
        private HubControlGate machine;

        [SetUp]
        public void SetUp()
        {
            // No manager means "someone pressed Play on the hub scene", which the gate treats as
            // the hub being in control. Cleared explicitly so a manager left behind by another
            // fixture can't decide these tests.
            ClearGameStateManagerInstance();
            machine = new HubControlGate();
        }

        [TearDown]
        public void TearDown() => ClearGameStateManagerInstance();

        private static void ClearGameStateManagerInstance() =>
            typeof(GameStateManager)
                .GetProperty("Instance", BindingFlags.Public | BindingFlags.Static)
                .SetValue(null, null);

        [Test]
        public void WithNothingInFlight_TheHubAcceptsInput()
        {
            Assert.IsTrue(machine.AcceptsMovement);
            Assert.IsTrue(machine.AcceptsWorldInteraction);
            Assert.IsFalse(machine.IsHandoverInFlight);
        }

        [Test]
        public void AHandoverInFlight_StopsTheHubAcceptingAnything()
        {
            Assert.IsTrue(machine.TryBeginHandover());

            Assert.IsTrue(machine.IsHandoverInFlight);
            Assert.IsFalse(machine.AcceptsMovement);
            Assert.IsFalse(machine.AcceptsWorldInteraction);
        }

        // Two doorways at once, or a door during a departure, would each land her somewhere the
        // other didn't expect.
        [Test]
        public void ASecondHandover_IsRefusedWhileOneIsInFlight()
        {
            machine.TryBeginHandover();

            Assert.IsFalse(machine.TryBeginHandover());
        }

        [Test]
        public void EndingTheHandover_GivesControlBackAndAnnouncesIt()
        {
            int ended = 0;
            machine.HandoverEnded += () => ended++;
            machine.TryBeginHandover();

            machine.EndHandover();

            Assert.AreEqual(1, ended, "The router needs this to re-arm a still-held button");
            Assert.IsTrue(machine.AcceptsMovement);
        }

        // This is the departure soft-lock: a refused departure used to leave the hub in a terminal
        // state with movement and interaction both dead.
        [Test]
        public void ARefusedHandover_CanHandControlBack()
        {
            machine.TryBeginHandover();
            machine.EndHandover();

            Assert.IsTrue(machine.TryBeginHandover(), "The hub must be able to try leaving again");
            machine.EndHandover();
            Assert.IsTrue(machine.AcceptsMovement);
        }

        [Test]
        public void EndingAHandoverThatNeverStarted_WarnsInsteadOfGoingNegative()
        {
            LogAssert.Expect(LogType.Warning, new System.Text.RegularExpressions.Regex("nothing in flight"));

            machine.EndHandover();

            Assert.IsFalse(machine.IsHandoverInFlight);
            Assert.IsTrue(machine.AcceptsMovement);
        }

        // A conversation is a high-level GameState now, so the hub must go quiet during one even
        // though no handover is in flight.
        [Test]
        public void DialogueElsewhere_StopsTheHubAcceptingAnything()
        {
            var go = new GameObject("TestGameStateManager");
            var table = ScriptableObject.CreateInstance<GameStateTransitionTable>();
            try
            {
                var manager = go.AddComponent<GameStateManager>();
                typeof(GameStateTransitionTable)
                    .GetField("validTransitions", BindingFlags.NonPublic | BindingFlags.Instance)
                    .SetValue(table, GameStateTransitionTable.CreateDefaultTransitions());
                manager.Initialize(table, GameState.HubExploration);

                Assert.IsTrue(machine.AcceptsMovement, "The hub is in control while HubExploration is up");

                Assert.IsTrue(manager.RequestTransition(GameState.Dialogue, "test"));

                Assert.IsFalse(machine.AcceptsMovement);
                Assert.IsFalse(machine.AcceptsWorldInteraction);
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(table);
                UnityEngine.Object.DestroyImmediate(go);
                ClearGameStateManagerInstance();
            }
        }
    }
}
