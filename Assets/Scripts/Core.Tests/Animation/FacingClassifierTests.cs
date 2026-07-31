using NUnit.Framework;
using UnityEngine;
using ProjectAstra.Core.Animation;

namespace ProjectAstra.Core.Tests.Animation
{
    [TestFixture]
    public class FacingClassifierTests
    {
        private const float Frame = 1f / 60f;
        private static Vector2 EastStep => new(0.13f, 0f);   // ~8 tiles/sec at 60fps

        [Test]
        public void StartsIdleFacingSouth()
        {
            var classifier = new FacingClassifier();
            Assert.IsFalse(classifier.Moving);
            Assert.AreEqual(Facing.South, classifier.Facing);
        }

        [Test]
        public void MovingEast_ReportsMovingAndFacingEast()
        {
            var classifier = new FacingClassifier();
            classifier.Tick(EastStep, Frame);
            Assert.IsTrue(classifier.Moving);
            Assert.AreEqual(Facing.East, classifier.Facing);
        }

        [Test]
        public void DominantAxisWins_WhenPathIsSlightlyOffAxis()
        {
            var classifier = new FacingClassifier();
            classifier.Tick(new Vector2(0.05f, 0.13f), Frame);   // y is larger
            Assert.AreEqual(Facing.North, classifier.Facing);
        }

        [Test]
        public void TinyDrift_BelowThreshold_ReadsIdle()
        {
            var classifier = new FacingClassifier();
            classifier.Tick(new Vector2(0.001f, 0f), 1f);   // 0.001 tiles/sec
            Assert.IsFalse(classifier.Moving);
        }

        [Test]
        public void KeepsRunning_ThroughShortGap_ThenIdles()
        {
            var classifier = new FacingClassifier(idleHoldSeconds: 0.05f);
            classifier.Tick(EastStep, Frame);

            classifier.Tick(Vector2.zero, 0.02f);   // shorter than the hold
            Assert.IsTrue(classifier.Moving, "should still be running through a brief gap");
            Assert.AreEqual(Facing.East, classifier.Facing);

            classifier.Tick(Vector2.zero, 0.05f);   // now past the hold
            Assert.IsFalse(classifier.Moving);
            Assert.AreEqual(Facing.East, classifier.Facing, "facing is remembered while idle");
        }

        [Test]
        public void Teleport_IsNotReadAsRunning()
        {
            var classifier = new FacingClassifier();
            classifier.Tick(EastStep, Frame);              // running
            classifier.Tick(new Vector2(5f, 0f), Frame);   // undo-snap across the map
            Assert.IsFalse(classifier.Moving);
        }

        [Test]
        public void ZeroDeltaTime_IsIgnored()
        {
            var classifier = new FacingClassifier();
            Assert.DoesNotThrow(() => classifier.Tick(EastStep, 0f));
            Assert.IsFalse(classifier.Moving);
        }
    }
}
