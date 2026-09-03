using NUnit.Framework;
using UnityEngine;
using ProjectAstra.Core.Hub;

namespace ProjectAstra.Core.Tests.Hub
{
    [TestFixture]
    public class WalkableRouteTimerTests
    {
        private static readonly Rect Footprint = new(-0.25f, 0f, 0.5f, 0.25f);

        private FakeSolidSpace space;

        [SetUp]
        public void SetUp() => space = new FakeSolidSpace(20f, 20f);

        private void BlockColumn(int cellX, int fromY, int toY)
        {
            space.Block(cellX * HubMover.Resolution, fromY * HubMover.Resolution,
                HubMover.Resolution, (toY - fromY + 1) * HubMover.Resolution);
        }

        [Test]
        public void AStraightWalk_TakesDistanceOverSpeed()
        {
            Assert.IsTrue(WalkableRouteTimer.TryMeasureSeconds(space, Footprint,
                new Vector2(2f, 2f), new Vector2(9f, 2f), out float seconds));

            // Seven tiles at 3.5 tiles a second.
            Assert.AreEqual(2f, seconds, 0.2f);
        }

        [Test]
        public void StandingOnTheTarget_TakesNoTime()
        {
            Assert.IsTrue(WalkableRouteTimer.TryMeasureSeconds(space, Footprint,
                new Vector2(4f, 4f), new Vector2(4f, 4f), out float seconds));
            Assert.AreEqual(0f, seconds, 0.001f);
        }

        // Cardinal movement means a diagonal is walked as two legs, so the distance is the sum of
        // both rather than the straight line.
        [Test]
        public void ADiagonalTarget_IsMeasuredAsTwoLegs()
        {
            WalkableRouteTimer.TryMeasureCells(space, Footprint, new Vector2(2f, 2f), new Vector2(5f, 6f), out int cells);

            Assert.AreEqual(14, cells, 2, "Three tiles across and four up is fourteen half-tile steps.");
        }

        [Test]
        public void AWallInTheWay_MakesTheRouteLonger()
        {
            BlockColumn(12, 0, 30);   // a wall with a gap above cellY 30

            Assert.IsTrue(WalkableRouteTimer.TryMeasureCells(space, Footprint,
                new Vector2(2f, 2f), new Vector2(10f, 2f), out int detoured));
            Assert.Greater(detoured, 16, "Going round the wall must cost more than going straight through.");
        }

        [Test]
        public void SomewhereSealedOff_CannotBeReached()
        {
            BlockColumn(12, 0, 39);   // a wall all the way across

            Assert.IsFalse(WalkableRouteTimer.TryMeasureSeconds(space, Footprint,
                new Vector2(2f, 2f), new Vector2(10f, 2f), out _));
        }

        [Test]
        public void StartingInsideSomethingSolid_CannotBeRoutedFrom()
        {
            space.Block(4 * HubMover.Resolution, 4 * HubMover.Resolution, HubMover.Resolution, HubMover.Resolution);

            Assert.IsFalse(WalkableRouteTimer.TryMeasureSeconds(space, Footprint,
                new Vector2(2.25f, 2.1f), new Vector2(9f, 9f), out _));
        }

        // A character's own body is solid, so a route to them has to count arriving beside them
        // rather than on top of them.
        [Test]
        public void ATargetStandingInSomethingSolid_IsStillReachableBeside()
        {
            space.Block(new Rect(8.75f, 8f, 0.5f, 0.5f));

            Assert.IsFalse(WalkableRouteTimer.TryMeasureSeconds(space, Footprint,
                new Vector2(2f, 2f), new Vector2(9f, 8.1f), out _));

            Assert.IsTrue(WalkableRouteTimer.CanReachNeighbour(space, Footprint,
                new Vector2(2f, 2f), new Vector2(9f, 8.1f), out float seconds));
            Assert.Greater(seconds, 0f);
        }

        [Test]
        public void SomewhereWalledOff_IsNotReachableBesideEither()
        {
            BlockColumn(12, 0, 39);

            Assert.IsFalse(WalkableRouteTimer.CanReachNeighbour(space, Footprint,
                new Vector2(2f, 2f), new Vector2(10f, 2f), out _));
        }
    }
}
