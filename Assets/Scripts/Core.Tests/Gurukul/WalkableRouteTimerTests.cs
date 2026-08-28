using NUnit.Framework;
using UnityEngine;
using ProjectAstra.Core.Gurukul;

namespace ProjectAstra.Core.Tests.Gurukul
{
    [TestFixture]
    public class WalkableRouteTimerTests
    {
        private static readonly Rect Footprint = new(-0.25f, 0f, 0.5f, 0.25f);

        private GurukulCollisionMap map;

        [SetUp]
        public void SetUp() => map = new GurukulCollisionMap(40, 40);   // 20x20 tiles, all clear

        private void BlockColumn(int cellX, int fromY, int toY)
        {
            for (int y = fromY; y <= toY; y++) map.SetGroundBlocked(cellX, y, true);
        }

        [Test]
        public void AStraightWalk_TakesDistanceOverSpeed()
        {
            Assert.IsTrue(WalkableRouteTimer.TryMeasureSeconds(map, Footprint,
                new Vector2(2f, 2f), new Vector2(9f, 2f), out float seconds));

            // Seven tiles at 3.5 tiles a second.
            Assert.AreEqual(2f, seconds, 0.2f);
        }

        [Test]
        public void StandingOnTheTarget_TakesNoTime()
        {
            Assert.IsTrue(WalkableRouteTimer.TryMeasureSeconds(map, Footprint,
                new Vector2(4f, 4f), new Vector2(4f, 4f), out float seconds));
            Assert.AreEqual(0f, seconds, 0.001f);
        }

        // Cardinal movement means a diagonal is walked as two legs, so the distance is the sum of
        // both rather than the straight line.
        [Test]
        public void ADiagonalTarget_IsMeasuredAsTwoLegs()
        {
            WalkableRouteTimer.TryMeasureCells(map, Footprint, new Vector2(2f, 2f), new Vector2(5f, 6f), out int cells);

            Assert.AreEqual(14, cells, 2, "Three tiles across and four up is fourteen half-tile steps.");
        }

        [Test]
        public void AWallInTheWay_MakesTheRouteLonger()
        {
            BlockColumn(12, 0, 30);   // a wall with a gap above cellY 30

            Assert.IsTrue(WalkableRouteTimer.TryMeasureCells(map, Footprint,
                new Vector2(2f, 2f), new Vector2(10f, 2f), out int detoured));
            Assert.Greater(detoured, 16, "Going round the wall must cost more than going straight through.");
        }

        [Test]
        public void SomewhereSealedOff_CannotBeReached()
        {
            BlockColumn(12, 0, 39);   // a wall all the way across

            Assert.IsFalse(WalkableRouteTimer.TryMeasureSeconds(map, Footprint,
                new Vector2(2f, 2f), new Vector2(10f, 2f), out _));
        }

        [Test]
        public void StartingInsideSomethingSolid_CannotBeRoutedFrom()
        {
            map.SetGroundBlocked(4, 4, true);

            Assert.IsFalse(WalkableRouteTimer.TryMeasureSeconds(map, Footprint,
                new Vector2(2.25f, 2.1f), new Vector2(9f, 9f), out _));
        }

        // A character's own body is solid, so a route to them has to count arriving beside them
        // rather than on top of them.
        [Test]
        public void ATargetStandingInSomethingSolid_IsStillReachableBeside()
        {
            map.Stamp(new Rect(8.75f, 8f, 0.5f, 0.5f));

            Assert.IsFalse(WalkableRouteTimer.TryMeasureSeconds(map, Footprint,
                new Vector2(2f, 2f), new Vector2(9f, 8.1f), out _));

            Assert.IsTrue(WalkableRouteTimer.CanReachNeighbour(map, Footprint,
                new Vector2(2f, 2f), new Vector2(9f, 8.1f), out float seconds));
            Assert.Greater(seconds, 0f);
        }

        [Test]
        public void SomewhereWalledOff_IsNotReachableBesideEither()
        {
            BlockColumn(12, 0, 39);

            Assert.IsFalse(WalkableRouteTimer.CanReachNeighbour(map, Footprint,
                new Vector2(2f, 2f), new Vector2(10f, 2f), out _));
        }
    }
}
