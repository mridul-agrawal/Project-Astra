using NUnit.Framework;
using UnityEngine;
using ProjectAstra.Core.Camera;

namespace ProjectAstra.Core.Tests.Camera
{
    [TestFixture]
    public class CameraContainmentTests
    {
        // A room wider than the view: the camera follows, but stops short of the edges.

        [Test]
        public void WellInsideTheRoom_TheCameraSitsWhereItWants()
        {
            float result = CameraContainment.ContainAxis(desired: 10f, min: 0f, max: 30f, viewSize: 8f);

            Assert.AreEqual(10f, result, 0.0001f);
        }

        [Test]
        public void NearTheLowEdge_TheCameraStopsHalfAViewIn()
        {
            float result = CameraContainment.ContainAxis(desired: 1f, min: 0f, max: 30f, viewSize: 8f);

            Assert.AreEqual(4f, result, 0.0001f, "half of an 8-wide view past the low edge");
        }

        [Test]
        public void NearTheHighEdge_TheCameraStopsHalfAViewShort()
        {
            float result = CameraContainment.ContainAxis(desired: 29f, min: 0f, max: 30f, viewSize: 8f);

            Assert.AreEqual(26f, result, 0.0001f);
        }


        // The awkward case: a room no bigger than one screenful can't be clamped, because the
        // lowest allowed centre would sit above the highest.

        [Test]
        public void ARoomNarrowerThanTheView_IsCentredInstead()
        {
            float result = CameraContainment.ContainAxis(desired: 1f, min: 0f, max: 5f, viewSize: 8f);

            Assert.AreEqual(2.5f, result, 0.0001f, "the room's own centre, not a clamp");
        }

        [Test]
        public void ARoomNarrowerThanTheView_IgnoresWhereTheTargetIs()
        {
            float atLeft = CameraContainment.ContainAxis(0f, 0f, 5f, 8f);
            float atRight = CameraContainment.ContainAxis(5f, 0f, 5f, 8f);

            Assert.AreEqual(atLeft, atRight, 0.0001f, "the view can't move — it already shows the whole room");
        }

        [Test]
        public void ARoomExactlyTheViewSize_IsCentred()
        {
            float result = CameraContainment.ContainAxis(desired: 0f, min: 0f, max: 8f, viewSize: 8f);

            Assert.AreEqual(4f, result, 0.0001f);
        }


        // Both axes at once, and each judged on its own — a room can be wide but short.

        [Test]
        public void EachAxisIsContainedIndependently()
        {
            var room = new Rect(0f, 0f, 30f, 5f);   // wide enough to scroll, too short to
            Vector2 result = CameraContainment.Contain(new Vector2(29f, 0f), room, new Vector2(8f, 8f));

            Assert.AreEqual(26f, result.x, 0.0001f, "x clamps");
            Assert.AreEqual(2.5f, result.y, 0.0001f, "y centres");
        }


        // Pixel rounding.

        [Test]
        public void APositionOffThePixelGrid_IsRoundedOntoIt()
        {
            Vector2 result = CameraContainment.RoundToPixel(new Vector2(1.019f, 2.994f), pixelsPerUnit: 32f);

            Assert.AreEqual(1.03125f, result.x, 0.0001f, "33/32");
            Assert.AreEqual(3f, result.y, 0.0001f, "96/32");
        }

        [Test]
        public void AlreadyOnTheGrid_NothingMoves()
        {
            Vector2 exact = new(2f, 3.5f);

            Vector2 result = CameraContainment.RoundToPixel(exact, 32f);

            Assert.AreEqual(exact.x, result.x, 0.0001f);
            Assert.AreEqual(exact.y, result.y, 0.0001f);
        }

        // A camera with no pixels-per-unit yet — read before MapCamera configured it — must not
        // divide by zero and fling the camera to the origin.
        [Test]
        public void WithNoPixelsPerUnit_ThePositionIsLeftAlone()
        {
            Vector2 result = CameraContainment.RoundToPixel(new Vector2(1.019f, 2.994f), 0f);

            Assert.AreEqual(1.019f, result.x, 0.0001f);
            Assert.AreEqual(2.994f, result.y, 0.0001f);
        }
    }
}
