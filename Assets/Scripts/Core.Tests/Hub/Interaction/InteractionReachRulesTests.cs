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

        private GameObject wall;

        [TearDown]
        public void TearDown()
        {
            if (wall != null) Object.DestroyImmediate(wall);
            wall = null;
        }

        private void WallAt(Vector2 centre, Vector2 size)
        {
            wall = new GameObject("Wall") { layer = LayerMask.NameToLayer(PhysicsSolidSpace.SolidLayer) };
            wall.transform.position = centre;
            BoxCollider2D box = wall.AddComponent<BoxCollider2D>();
            box.size = size;
            Physics2D.SyncTransforms();
        }

        [Test]
        public void AcrossOpenGround_IsVisible()
        {
            Assert.IsTrue(InteractionReachRules.HasLineOfSight(new Vector2(1f, 1f), new Vector2(3f, 1f)));
        }

        [Test]
        public void ThroughAWall_IsNot()
        {
            WallAt(new Vector2(2f, 1f), new Vector2(0.4f, 0.4f));

            Assert.IsFalse(InteractionReachRules.HasLineOfSight(new Vector2(1f, 1f), new Vector2(3f, 1f)),
                "a wall between them breaks the interaction");
        }

        // A character's own body is solid, and would otherwise block the view of itself.
        [Test]
        public void ATargetStandingOnSolidGround_IsStillVisible()
        {
            WallAt(new Vector2(2f, 1f), new Vector2(0.4f, 0.4f));

            Assert.IsTrue(InteractionReachRules.HasLineOfSight(new Vector2(1.7f, 1f), new Vector2(2f, 1f)),
                "standing right in front of someone solid must still count as seeing them");
        }

        [Test]
        public void ATriggerIsNotAWall()
        {
            WallAt(new Vector2(2f, 1f), new Vector2(0.4f, 0.4f));
            wall.GetComponent<BoxCollider2D>().isTrigger = true;
            Physics2D.SyncTransforms();

            Assert.IsTrue(InteractionReachRules.HasLineOfSight(new Vector2(1f, 1f), new Vector2(3f, 1f)),
                "an interaction volume must never block the view of anything");
        }
    }
}
