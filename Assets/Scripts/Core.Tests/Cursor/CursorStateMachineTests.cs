using NUnit.Framework;
using ProjectAstra.Core.Cursor;
using ProjectAstra.Core.Units;

namespace ProjectAstra.Core.Tests.Cursor
{
    [TestFixture]
    public class CursorStateMachineTests
    {
        private CursorStateMachine machine;

        [SetUp]
        public void SetUp()
        {
            machine = new CursorStateMachine();
        }

        // --- Transition tests ---

        [Test]
        public void InitialState_IsFree()
        {
            Assert.AreEqual(CursorState.Free, machine.CurrentState);
        }

        [TestCase(CursorState.Free, CursorState.Selected)]
        [TestCase(CursorState.Selected, CursorState.Moving)]
        [TestCase(CursorState.Selected, CursorState.Free)]
        [TestCase(CursorState.Moving, CursorState.ActionMenu)]
        [TestCase(CursorState.ActionMenu, CursorState.Free)]
        [TestCase(CursorState.ActionMenu, CursorState.Selected)]
        [TestCase(CursorState.ActionMenu, CursorState.Targeting)]
        [TestCase(CursorState.Targeting, CursorState.ActionMenu)]
        public void SpecContractTransitions_AreLegal(CursorState from, CursorState to)
        {
            Assert.IsTrue(CursorStateMachine.IsLegal(from, to), $"{from} -> {to} should be legal.");
        }

        [TestCase(CursorState.Free, CursorState.Moving)]
        [TestCase(CursorState.Free, CursorState.ActionMenu)]
        [TestCase(CursorState.Free, CursorState.Targeting)]
        [TestCase(CursorState.Selected, CursorState.ActionMenu)]
        [TestCase(CursorState.Selected, CursorState.Targeting)]
        public void SkippingAStep_IsIllegal(CursorState from, CursorState to)
        {
            Assert.IsFalse(CursorStateMachine.IsLegal(from, to), $"{from} -> {to} should be rejected.");
        }

        [Test]
        public void TryTransition_RejectsIllegalAndLeavesStateUnchanged()
        {
            UnityEngine.TestTools.LogAssert.Expect(UnityEngine.LogType.Warning,
                "[CursorStateMachine] Illegal transition: Free -> ActionMenu. Rejected.");

            Assert.IsFalse(machine.TryTransition(CursorState.ActionMenu));
            Assert.AreEqual(CursorState.Free, machine.CurrentState);
        }

        [Test]
        public void TryTransition_ToSameState_SucceedsWithoutEvent()
        {
            int fired = 0;
            machine.StateChanged += (_, _) => fired++;

            Assert.IsTrue(machine.TryTransition(CursorState.Free));
            Assert.AreEqual(0, fired);
        }

        [Test]
        public void StateChanged_CarriesPreviousAndNext()
        {
            CursorState from = CursorState.Suspended, to = CursorState.Suspended;
            machine.StateChanged += (a, b) => { from = a; to = b; };

            machine.TryTransition(CursorState.Selected);

            Assert.AreEqual(CursorState.Free, from);
            Assert.AreEqual(CursorState.Selected, to);
        }

        // --- Suspend / restore ---

        [Test]
        public void RestoreFromSuspend_ReturnsToTheStateBeforeSuspending()
        {
            machine.TryTransition(CursorState.Selected);
            machine.TryTransition(CursorState.Suspended);

            Assert.IsTrue(machine.RestoreFromSuspend());
            Assert.AreEqual(CursorState.Selected, machine.CurrentState);
        }

        [Test]
        public void RestoreFromSuspend_WhenNotSuspended_DoesNothing()
        {
            Assert.IsFalse(machine.RestoreFromSuspend());
            Assert.AreEqual(CursorState.Free, machine.CurrentState);
        }

        // --- Hover ---

        [Test]
        public void SetHover_RaisesOnlyOnChange()
        {
            int fired = 0;
            machine.HoverChanged += _ => fired++;

            machine.SetHover(CursorHover.ReadyAlly);
            machine.SetHover(CursorHover.ReadyAlly);

            Assert.AreEqual(1, fired);
        }

        [Test]
        public void ResetHoverIfNotFree_ClearsStaleHoverOnceSelected()
        {
            machine.SetHover(CursorHover.ReadyAlly);
            machine.TryTransition(CursorState.Selected);

            machine.ResetHoverIfNotFree();

            Assert.AreEqual(CursorHover.Empty, machine.CurrentHover);
        }

        [Test]
        public void ResetHoverIfNotFree_KeepsHoverWhileFree()
        {
            machine.SetHover(CursorHover.Enemy);

            machine.ResetHoverIfNotFree();

            Assert.AreEqual(CursorHover.Enemy, machine.CurrentHover);
        }

        // --- Hover classification ---

        [Test]
        public void Classify_EmptyTile_IsEmpty()
        {
            Assert.AreEqual(CursorHover.Empty, CursorHoverClassifier.Classify(null, true));
        }

        [TestCase(Faction.Player, true, CursorHover.ReadyAlly)]
        [TestCase(Faction.Player, false, CursorHover.ActedAlly)]
        [TestCase(Faction.Allied, true, CursorHover.ReadyAlly)]
        [TestCase(Faction.Allied, false, CursorHover.ActedAlly)]
        [TestCase(Faction.Enemy, true, CursorHover.Enemy)]
        [TestCase(Faction.Enemy, false, CursorHover.Enemy)]
        public void Classify_MapsFactionAndActedFlag(Faction faction, bool canAct, CursorHover expected)
        {
            Assert.AreEqual(expected, CursorHoverClassifier.Classify(faction, canAct));
        }

        [Test]
        public void IsSelectable_OnlyReadyPlayerUnits()
        {
            Assert.IsTrue(CursorHoverClassifier.IsSelectable(CursorHover.ReadyAlly, Faction.Player));
            Assert.IsFalse(CursorHoverClassifier.IsSelectable(CursorHover.ReadyAlly, Faction.Allied));
            Assert.IsFalse(CursorHoverClassifier.IsSelectable(CursorHover.ActedAlly, Faction.Player));
            Assert.IsFalse(CursorHoverClassifier.IsSelectable(CursorHover.Enemy, Faction.Enemy));
        }

        [TestCase(CursorHover.ActedAlly, CursorErrorKind.UnitAlreadyActed)]
        [TestCase(CursorHover.Enemy, CursorErrorKind.NotYourUnit)]
        [TestCase(CursorHover.Empty, CursorErrorKind.InvalidTile)]
        public void ErrorFor_DistinguishesRefusalReasons(CursorHover hover, CursorErrorKind expected)
        {
            Assert.AreEqual(expected, CursorHoverClassifier.ErrorFor(hover));
        }
    }
}
