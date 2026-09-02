using NUnit.Framework;
using UnityEngine;
using ProjectAstra.Core.Animation;
using ProjectAstra.Core.Hub;
using ProjectAstra.Core.Hub.Interaction;

namespace ProjectAstra.Core.Tests.Hub.Interaction
{
    [TestFixture]
    public class InteractionReachRulesTests
    {
        private static InteractorPose Facing(Facing facing, float x = 5f, float y = 5f) =>
            new(new Vector2(x, y), facing);


        // Facing — "visibly adjacent and facing", with no tile-centre alignment.

        [Test]
        public void SomethingDeadAhead_IsFaced()
        {
            Assert.IsTrue(InteractionReachRules.IsFacing(Facing(ProjectAstra.Core.Animation.Facing.North),
                new Vector2(5f, 6f)));
        }

        [Test]
        public void SomethingBehindHer_IsNot()
        {
            Assert.IsFalse(InteractionReachRules.IsFacing(Facing(ProjectAstra.Core.Animation.Facing.North),
                new Vector2(5f, 4f)));
        }

        [Test]
        public void SomethingBesideHer_IsNot()
        {
            Assert.IsFalse(InteractionReachRules.IsFacing(Facing(ProjectAstra.Core.Animation.Facing.North),
                new Vector2(6f, 5f)));
        }

        // The rule that makes "no pixel-perfect positioning" true.
        [Test]
        public void SlightlyOffToOneSide_StillCounts()
        {
            Assert.IsTrue(InteractionReachRules.IsFacing(Facing(ProjectAstra.Core.Animation.Facing.North),
                new Vector2(5.4f, 6f)), "half a tile of drift must not break an interaction");
        }

        [Test]
        public void FarOffToOneSide_DoesNot()
        {
            Assert.IsFalse(InteractionReachRules.IsFacing(Facing(ProjectAstra.Core.Animation.Facing.North),
                new Vector2(6.2f, 6f)));
        }

        [TestCase(ProjectAstra.Core.Animation.Facing.North, 5f, 6f)]
        [TestCase(ProjectAstra.Core.Animation.Facing.South, 5f, 4f)]
        [TestCase(ProjectAstra.Core.Animation.Facing.East, 6f, 5f)]
        [TestCase(ProjectAstra.Core.Animation.Facing.West, 4f, 5f)]
        public void EveryCardinalSideWorks(Facing facing, float targetX, float targetY)
        {
            Assert.IsTrue(InteractionReachRules.IsFacing(Facing(facing), new Vector2(targetX, targetY)));
        }

        [Test]
        public void AWiderToleranceAcceptsMoreDrift()
        {
            var pose = Facing(ProjectAstra.Core.Animation.Facing.North);
            var target = new Vector2(6.2f, 6f);

            Assert.IsFalse(InteractionReachRules.IsFacing(pose, target));
            Assert.IsTrue(InteractionReachRules.IsFacing(pose, target, lateralTolerance: 2f),
                "a wide counter can ask for a wider approach");
        }


        // Lateral offset — how the controller breaks a tie between two things in range.

        [Test]
        public void DeadAhead_HasNoLateralOffset()
        {
            Assert.AreEqual(0f,
                InteractionReachRules.LateralOffset(Facing(ProjectAstra.Core.Animation.Facing.North),
                    new Vector2(5f, 7f)), 0.0001f);
        }

        [Test]
        public void OffToTheSide_MeasuresHowFar()
        {
            Assert.AreEqual(1.5f,
                InteractionReachRules.LateralOffset(Facing(ProjectAstra.Core.Animation.Facing.North),
                    new Vector2(6.5f, 7f)), 0.0001f);
        }

        [Test]
        public void WhichSideDoesNotMatter()
        {
            var pose = Facing(ProjectAstra.Core.Animation.Facing.North);

            Assert.AreEqual(
                InteractionReachRules.LateralOffset(pose, new Vector2(6.5f, 7f)),
                InteractionReachRules.LateralOffset(pose, new Vector2(3.5f, 7f)), 0.0001f);
        }


        // Line of sight — the rule that stops her talking through a wall.

        [Test]
        public void WithNoCollisionMap_EverythingIsVisible()
        {
            Assert.IsTrue(InteractionReachRules.HasLineOfSight(
                new Vector2(0f, 0f), new Vector2(5f, 0f), null));
        }

        [Test]
        public void AcrossOpenGround_IsVisible()
        {
            var map = new HubCollisionMap(20, 20);

            Assert.IsTrue(InteractionReachRules.HasLineOfSight(
                new Vector2(1f, 1f), new Vector2(3f, 1f), map));
        }

        [Test]
        public void ThroughABlockedCell_IsNot()
        {
            var map = new HubCollisionMap(20, 20);
            map.Stamp(new Rect(1.9f, 0.9f, 0.4f, 0.4f));

            Assert.IsFalse(InteractionReachRules.HasLineOfSight(
                new Vector2(1f, 1f), new Vector2(3f, 1f), map),
                "a wall between them breaks the interaction");
        }

        // A character's own body is solid, and would otherwise block the view of itself.
        [Test]
        public void ATargetStandingOnBlockedGround_IsStillVisible()
        {
            var map = new HubCollisionMap(20, 20);
            map.Stamp(new Rect(1.9f, 0.9f, 0.4f, 0.4f));

            Assert.IsTrue(InteractionReachRules.HasLineOfSight(
                new Vector2(1.7f, 1f), new Vector2(2f, 1f), map),
                "standing right in front of someone solid must still count as seeing them");
        }
    }
}
