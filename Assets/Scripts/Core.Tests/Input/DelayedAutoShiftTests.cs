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

        private DelayedAutoShift das;
        private List<Vector2Int> moves;

        [SetUp]
        public void SetUp()
        {
            das = new DelayedAutoShift(InitialDelay, RepeatRate, FastRepeatRate);
            moves = new List<Vector2Int>();
            das.CursorMoveTriggered += dir => moves.Add(dir);
        }

        [Test]
        public void Press_FiresOneMoveImmediately()
        {
            das.Press(CursorDirection.Up);
            Assert.AreEqual(new[] { Vector2Int.up }, moves.ToArray());
        }

        [Test]
        public void EachDirection_MapsToCorrectVector()
        {
            das.Press(CursorDirection.Up);
            das.Press(CursorDirection.Down);
            das.Press(CursorDirection.Left);
            das.Press(CursorDirection.Right);

            Assert.AreEqual(
                new[] { Vector2Int.up, Vector2Int.down, Vector2Int.left, Vector2Int.right },
                moves.ToArray());
        }

        [Test]
        public void HeldDirection_DoesNotRepeatBeforeInitialDelay()
        {
            das.Press(CursorDirection.Up);
            das.Tick(InitialDelay - 0.01f, false);
            Assert.AreEqual(1, moves.Count);
        }

        [Test]
        public void HeldDirection_RepeatsAfterInitialDelayThenAtRepeatRate()
        {
            das.Press(CursorDirection.Up);

            das.Tick(InitialDelay, false);
            Assert.AreEqual(2, moves.Count, "should fire once when initial delay elapses");

            das.Tick(RepeatRate, false);
            Assert.AreEqual(3, moves.Count, "should fire again one repeat-rate later");
        }

        [Test]
        public void FastCursorHeld_UsesFastRepeatRate()
        {
            das.Press(CursorDirection.Up);
            das.Tick(InitialDelay, true);

            das.Tick(FastRepeatRate, true);
            Assert.AreEqual(3, moves.Count);
        }

        [Test]
        public void Release_StopsRepeats()
        {
            das.Press(CursorDirection.Up);
            das.Tick(InitialDelay, false);
            das.Release(CursorDirection.Up);

            das.Tick(10f, false);
            Assert.AreEqual(2, moves.Count, "no further moves after release");
        }

        [Test]
        public void Reset_ClearsHeldState()
        {
            das.Press(CursorDirection.Up);
            das.Reset();

            das.Tick(10f, false);
            Assert.AreEqual(1, moves.Count, "only the initial press move survives a reset");
        }

        [Test]
        public void RePress_RestartsInitialDelay()
        {
            das.Press(CursorDirection.Up);
            das.Tick(InitialDelay, false);
            das.Press(CursorDirection.Up);

            das.Tick(RepeatRate, false);
            Assert.AreEqual(3, moves.Count,
                "re-press fires immediately then waits the full initial delay again");
        }

        [Test]
        public void Advance_CarriesOvershootIntoNextTick()
        {
            das.Press(CursorDirection.Up);
            das.Tick(InitialDelay, false);

            das.Tick(RepeatRate + RepeatRate * 0.5f, false);
            Assert.AreEqual(3, moves.Count, "one repeat fires; remainder is carried");

            das.Tick(RepeatRate * 0.5f, false);
            Assert.AreEqual(4, moves.Count, "carried remainder completes the next repeat");
        }
    }
}
