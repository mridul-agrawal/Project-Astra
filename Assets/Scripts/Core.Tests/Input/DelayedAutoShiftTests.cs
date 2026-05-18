using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;
using ProjectAstra.Core.Input;

namespace ProjectAstra.Core.Tests.Input
{
    [TestFixture]
    public class DelayedAutoShiftTests
    {
        private const float InitialDelay = 0.4f;
        private const float RepeatRate = 0.1f;
        private const float FastRepeatRate = 0.05f;

        private DelayedAutoShift _das;
        private List<Vector2Int> _moves;

        [SetUp]
        public void SetUp()
        {
            _das = new DelayedAutoShift(InitialDelay, RepeatRate, FastRepeatRate);
            _moves = new List<Vector2Int>();
            _das.CursorMoveTriggered += dir => _moves.Add(dir);
        }

        [Test]
        public void Press_FiresOneMoveImmediately()
        {
            _das.Press(CursorDirection.Up);
            Assert.AreEqual(new[] { Vector2Int.up }, _moves.ToArray());
        }

        [Test]
        public void EachDirection_MapsToCorrectVector()
        {
            _das.Press(CursorDirection.Up);
            _das.Press(CursorDirection.Down);
            _das.Press(CursorDirection.Left);
            _das.Press(CursorDirection.Right);

            Assert.AreEqual(
                new[] { Vector2Int.up, Vector2Int.down, Vector2Int.left, Vector2Int.right },
                _moves.ToArray());
        }

        [Test]
        public void HeldDirection_DoesNotRepeatBeforeInitialDelay()
        {
            _das.Press(CursorDirection.Up);
            _das.Tick(InitialDelay - 0.01f, false);
            Assert.AreEqual(1, _moves.Count);
        }

        [Test]
        public void HeldDirection_RepeatsAfterInitialDelayThenAtRepeatRate()
        {
            _das.Press(CursorDirection.Up);

            _das.Tick(InitialDelay, false);
            Assert.AreEqual(2, _moves.Count, "should fire once when initial delay elapses");

            _das.Tick(RepeatRate, false);
            Assert.AreEqual(3, _moves.Count, "should fire again one repeat-rate later");
        }

        [Test]
        public void FastCursorHeld_UsesFastRepeatRate()
        {
            _das.Press(CursorDirection.Up);
            _das.Tick(InitialDelay, true);

            _das.Tick(FastRepeatRate, true);
            Assert.AreEqual(3, _moves.Count);
        }

        [Test]
        public void Release_StopsRepeats()
        {
            _das.Press(CursorDirection.Up);
            _das.Tick(InitialDelay, false);
            _das.Release(CursorDirection.Up);

            _das.Tick(10f, false);
            Assert.AreEqual(2, _moves.Count, "no further moves after release");
        }

        [Test]
        public void Reset_ClearsHeldState()
        {
            _das.Press(CursorDirection.Up);
            _das.Reset();

            _das.Tick(10f, false);
            Assert.AreEqual(1, _moves.Count, "only the initial press move survives a reset");
        }

        [Test]
        public void RePress_RestartsInitialDelay()
        {
            _das.Press(CursorDirection.Up);
            _das.Tick(InitialDelay, false);
            _das.Press(CursorDirection.Up);

            _das.Tick(RepeatRate, false);
            Assert.AreEqual(3, _moves.Count,
                "re-press fires immediately then waits the full initial delay again");
        }

        [Test]
        public void Advance_CarriesOvershootIntoNextTick()
        {
            _das.Press(CursorDirection.Up);
            _das.Tick(InitialDelay, false);

            _das.Tick(RepeatRate + RepeatRate * 0.5f, false);
            Assert.AreEqual(3, _moves.Count, "one repeat fires; remainder is carried");

            _das.Tick(RepeatRate * 0.5f, false);
            Assert.AreEqual(4, _moves.Count, "carried remainder completes the next repeat");
        }
    }
}
