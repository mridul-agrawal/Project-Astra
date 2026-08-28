using NUnit.Framework;
using UnityEngine;
using ProjectAstra.Core.Animation;
using ProjectAstra.Core.Gurukul.Events;

namespace ProjectAstra.Core.Tests.Gurukul
{
    [TestFixture]
    public class CardinalRouteFollowerTests
    {
        [Test]
        public void AlreadyThere_IsArrival()
        {
            Assert.IsTrue(CardinalRouteFollower.HasArrived(new Vector2(4f, 4f), new Vector2(4f, 4f)));
            Assert.IsNull(CardinalRouteFollower.NextStep(new Vector2(4f, 4f), new Vector2(4f, 4f)));
        }

        [TestCase(6f, 4f, Facing.East)]
        [TestCase(2f, 4f, Facing.West)]
        [TestCase(4f, 6f, Facing.North)]
        [TestCase(4f, 2f, Facing.South)]
        public void AStraightLeg_WalksThatWay(float toX, float toY, Facing expected)
        {
            Assert.AreEqual(expected, CardinalRouteFollower.NextStep(new Vector2(4f, 4f), new Vector2(toX, toY)));
        }

        // A corner is walked as two legs, never as a diagonal.
        [Test]
        public void ACornerIsWalkedHorizontallyFirst()
        {
            Assert.AreEqual(Facing.East, CardinalRouteFollower.NextStep(new Vector2(4f, 4f), new Vector2(8f, 9f)));
        }

        [Test]
        public void OnceAlignedHorizontally_ItTurnsVertical()
        {
            Assert.AreEqual(Facing.North, CardinalRouteFollower.NextStep(new Vector2(8f, 4f), new Vector2(8f, 9f)));
        }

        [Test]
        public void ArrivalHasATolerance_SoItSettlesInsteadOfShuffling()
        {
            var almost = new Vector2(8f - CardinalRouteFollower.ArrivalTolerance * 0.5f, 9f);

            Assert.IsTrue(CardinalRouteFollower.HasArrived(almost, new Vector2(8f, 9f)));
            Assert.IsNull(CardinalRouteFollower.NextStep(almost, new Vector2(8f, 9f)));
        }

        // A slow frame would otherwise carry the character past the corner and straight into
        // walking back to it.
        [Test]
        public void AStepThatWouldOvershoot_IsCutAtTheCorner()
        {
            var from = new Vector2(4f, 4f);
            var stepped = new Vector2(9f, 4f);
            var corner = new Vector2(8f, 4f);

            Vector2 clamped = CardinalRouteFollower.ClampToCorner(from, stepped, corner, Facing.East);

            Assert.AreEqual(8f, clamped.x, 0.0001f);
            Assert.AreEqual(4f, clamped.y, 0.0001f);
        }

        [Test]
        public void AStepThatFallsShort_IsLeftAlone()
        {
            var from = new Vector2(4f, 4f);
            var stepped = new Vector2(5f, 4f);

            Vector2 clamped = CardinalRouteFollower.ClampToCorner(from, stepped, new Vector2(8f, 4f), Facing.East);

            Assert.AreEqual(5f, clamped.x, 0.0001f);
        }

        [Test]
        public void OvershootIsCutOnTheVerticalLegToo()
        {
            var from = new Vector2(4f, 4f);
            var stepped = new Vector2(4f, 1f);

            Vector2 clamped = CardinalRouteFollower.ClampToCorner(from, stepped, new Vector2(4f, 2f), Facing.South);

            Assert.AreEqual(2f, clamped.y, 0.0001f);
            Assert.AreEqual(4f, clamped.x, 0.0001f);
        }
    }
}
