using NUnit.Framework;
using UnityEngine;
using ProjectAstra.Core.Hub;

namespace ProjectAstra.Core.Tests.Hub
{
    // The viewport is 15 x 8.4375 tiles, which is 480x270 at 32px per tile.
    [TestFixture]
    public class EdgeIndicatorSolverTests
    {
        private static readonly Vector2 View = new(15f, 8.4375f);
        private static readonly Vector2 Camera = new(20f, 20f);
        private const float Inset = 40f;

        private static bool Solve(Vector2 target, out Vector2 canvas, out Vector2 direction) =>
            EdgeIndicatorSolver.TrySolve(target, Camera, View, Inset, out canvas, out direction);

        [Test]
        public void SomethingOnScreen_NeedsNoIndicator()
        {
            Assert.IsFalse(Solve(Camera + new Vector2(3f, 2f), out _, out _));
        }

        [Test]
        public void SomethingJustPastTheEdge_NeedsOne()
        {
            Assert.IsTrue(Solve(Camera + new Vector2(9f, 0f), out _, out _));
        }

        [Test]
        public void TheIndicatorPointsAtTheTarget()
        {
            Solve(Camera + new Vector2(0f, 12f), out _, out Vector2 direction);

            Assert.AreEqual(0f, direction.x, 0.001f);
            Assert.AreEqual(1f, direction.y, 0.001f);
        }

        // Pulled in by the inset so the marker sits inside the frame rather than half off it.
        [Test]
        public void SomethingDueEast_SitsOnTheRightEdge()
        {
            Solve(Camera + new Vector2(20f, 0f), out Vector2 canvas, out _);

            Assert.AreEqual(HubScreenSpace.CanvasWidth - Inset, canvas.x, 0.5f);
            Assert.AreEqual(HubScreenSpace.CanvasHeight * 0.5f, canvas.y, 0.5f);
        }

        [Test]
        public void SomethingDueNorth_SitsOnTheTopEdge()
        {
            Solve(Camera + new Vector2(0f, 20f), out Vector2 canvas, out _);

            Assert.AreEqual(HubScreenSpace.CanvasWidth * 0.5f, canvas.x, 0.5f);
            Assert.AreEqual(HubScreenSpace.CanvasHeight - Inset, canvas.y, 0.5f);
        }

        [Test]
        public void TheIndicatorAlwaysStaysInsideTheCanvas()
        {
            var directions = new[]
            {
                new Vector2(30f, 3f), new Vector2(-30f, 3f),
                new Vector2(3f, 30f), new Vector2(3f, -30f),
                new Vector2(25f, 25f), new Vector2(-25f, -25f)
            };

            foreach (Vector2 offset in directions)
            {
                Assert.IsTrue(Solve(Camera + offset, out Vector2 canvas, out _));
                Assert.GreaterOrEqual(canvas.x, Inset - 0.5f);
                Assert.LessOrEqual(canvas.x, HubScreenSpace.CanvasWidth - Inset + 0.5f);
                Assert.GreaterOrEqual(canvas.y, Inset - 0.5f);
                Assert.LessOrEqual(canvas.y, HubScreenSpace.CanvasHeight - Inset + 0.5f);
            }
        }

        [Test]
        public void SomethingExactlyOnTheCamera_NeedsNoIndicator()
        {
            Assert.IsFalse(Solve(Camera, out _, out _));
        }

        [Test]
        public void TheCanvasScale_IsTheProjectsFourTimesMapping()
        {
            Assert.AreEqual(4f, HubScreenSpace.CanvasScale, 0.0001f);
            Assert.AreEqual(240f, HubScreenSpace.ToCanvas(60f), 0.0001f);
        }

        [Test]
        public void WorldToCanvas_PutsTheCameraCentreInTheMiddle()
        {
            Vector2 canvas = HubScreenSpace.WorldToCanvas(Camera, Camera, View);

            Assert.AreEqual(HubScreenSpace.CanvasWidth * 0.5f, canvas.x, 0.001f);
            Assert.AreEqual(HubScreenSpace.CanvasHeight * 0.5f, canvas.y, 0.001f);
        }
    }
}
