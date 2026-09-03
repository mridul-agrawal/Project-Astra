using NUnit.Framework;
using UnityEngine;
using ProjectAstra.Core.Animation;
using ProjectAstra.Core.Hub;

namespace ProjectAstra.Core.Tests.Hub
{
    [TestFixture]
    public class HubMoverTests
    {
        // A half-tile-wide box sitting at the feet, which is what a 32px character blocks on.
        private static readonly Rect Footprint = new(-0.25f, 0f, 0.5f, 0.25f);

        private FakeSolidSpace space;

        [SetUp]
        public void SetUp() => space = new FakeSolidSpace(8f, 8f);

        private Vector2 Move(Vector2 from, Facing? direction, float deltaTime, out bool moved) =>
            HubMover.Move(space, from, Footprint, direction, deltaTime, out moved);

        // A wall running the whole height of the room, half a tile thick.
        private void BlockColumn(int cellX)
        {
            space.Block(cellX * HubMover.Resolution, 0f, HubMover.Resolution, 8f);
        }

        [Test]
        public void OpenGround_MovesAtTheAuthoredSpeed()
        {
            Vector2 result = Move(new Vector2(2f, 2f), Facing.East, 1f, out bool moved);

            Assert.IsTrue(moved);
            Assert.AreEqual(2f + HubMover.DefaultSpeedTilesPerSecond, result.x, 0.001f);
            Assert.AreEqual(2f, result.y, 0.001f);
        }

        [Test]
        public void NoDirection_StandsStill()
        {
            Vector2 result = Move(new Vector2(2f, 2f), null, 1f, out bool moved);

            Assert.IsFalse(moved);
            Assert.AreEqual(new Vector2(2f, 2f), result);
        }

        [Test]
        public void ZeroDeltaTime_StandsStill()
        {
            Move(new Vector2(2f, 2f), Facing.East, 0f, out bool moved);

            Assert.IsFalse(moved);
        }

        [TestCase(Facing.North, 0f, 1f)]
        [TestCase(Facing.South, 0f, -1f)]
        [TestCase(Facing.East, 1f, 0f)]
        [TestCase(Facing.West, -1f, 0f)]
        public void EachDirection_MovesAlongItsOwnAxisOnly(Facing facing, float signX, float signY)
        {
            const float dt = 0.1f;
            float distance = HubMover.DefaultSpeedTilesPerSecond * dt;
            var start = new Vector2(4f, 4f);

            Vector2 result = Move(start, facing, dt, out _);

            Assert.AreEqual(start.x + signX * distance, result.x, 0.001f);
            Assert.AreEqual(start.y + signY * distance, result.y, 0.001f);
        }

        // Cell 8 covers world x 4.0 to 4.5, so a footprint whose right edge is at 4.0 is flush
        // against it. Stopping short of that would show a visible gap at this zoom.
        [Test]
        public void WalkingIntoAWall_StopsFlushAgainstIt()
        {
            BlockColumn(8);

            Vector2 result = Move(new Vector2(2f, 2f), Facing.East, 1f, out bool moved);

            Assert.IsTrue(moved);
            Assert.AreEqual(4f, result.x + Footprint.xMax, 0.01f);
        }

        [Test]
        public void AlreadyAgainstAWall_ReportsNoMovement()
        {
            BlockColumn(8);
            Vector2 against = Move(new Vector2(2f, 2f), Facing.East, 1f, out _);

            Move(against, Facing.East, 0.1f, out bool movedAgain);

            Assert.IsFalse(movedAgain, "Pressing into a wall must not creep, jitter or bank movement.");
        }

        [Test]
        public void PushingIntoAWall_LeavesTheOtherAxisFree()
        {
            BlockColumn(8);
            Vector2 against = Move(new Vector2(2f, 2f), Facing.East, 1f, out _);

            Vector2 result = Move(against, Facing.North, 0.1f, out bool moved);

            Assert.IsTrue(moved);
            Assert.Greater(result.y, against.y);
        }

        // A frame long enough to cover several tiles must still be stopped by a single half-tile
        // wall, which is what the sub-stepping is for.
        [Test]
        public void AVeryLongFrame_DoesNotTunnelThroughAThinWall()
        {
            BlockColumn(8);

            Vector2 result = Move(new Vector2(2f, 2f), Facing.East, 10f, out _);

            Assert.LessOrEqual(result.x + Footprint.xMax, 4.01f,
                "A hitching frame must not carry the player past a wall.");
        }

        [Test]
        public void TheMapEdge_StopsMovementWithoutAnyPaintedWall()
        {
            Vector2 result = Move(new Vector2(7f, 4f), Facing.East, 10f, out _);

            Assert.LessOrEqual(result.x + Footprint.xMax, 8.01f);
        }

        [Test]
        public void FootprintAt_OffsetsTheBoxToThePosition()
        {
            Rect box = HubMover.FootprintAt(new Vector2(3f, 5f), Footprint);

            Assert.AreEqual(2.75f, box.xMin, 0.001f);
            Assert.AreEqual(3.25f, box.xMax, 0.001f);
            Assert.AreEqual(5f, box.yMin, 0.001f);
        }
    }
}
