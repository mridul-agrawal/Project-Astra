using NUnit.Framework;
using UnityEngine;
using ProjectAstra.Core.Hub;

namespace ProjectAstra.Core.Tests.Hub
{
    // Cells are half a tile, so a 4x4-tile room is an 8x8 map and cell (4,0) covers world x 2.0-2.5.
    [TestFixture]
    public class HubCollisionMapTests
    {
        private HubCollisionMap map;

        [SetUp]
        public void SetUp() => map = new HubCollisionMap(8, 8);

        [Test]
        public void AnEmptyMap_IsWalkableInside()
        {
            Assert.IsFalse(map.IsCellBlocked(0, 0));
            Assert.IsFalse(map.IsCellBlocked(7, 7));
        }

        // The room's edge is the wall — nothing has to be painted around the border.
        [TestCase(-1, 0)]
        [TestCase(0, -1)]
        [TestCase(8, 0)]
        [TestCase(0, 8)]
        public void OutsideTheMap_IsBlocked(int cellX, int cellY)
        {
            Assert.IsTrue(map.IsCellBlocked(cellX, cellY));
        }

        [Test]
        public void PaintedGround_ReadsAsBlocked()
        {
            map.SetGroundBlocked(3, 3, true);

            Assert.IsTrue(map.IsCellBlocked(3, 3));
            Assert.IsFalse(map.IsCellBlocked(3, 4));
        }

        [Test]
        public void StampingAProp_BlocksTheCellsItCovers()
        {
            map.Stamp(new Rect(1f, 1f, 0.5f, 0.5f));

            Assert.IsTrue(map.IsCellBlocked(2, 2));
            Assert.IsFalse(map.IsCellBlocked(3, 2));
        }

        [Test]
        public void UnstampingAProp_OpensItBackUp()
        {
            var footprint = new Rect(1f, 1f, 0.5f, 0.5f);
            map.Stamp(footprint);
            map.Unstamp(footprint);

            Assert.IsFalse(map.IsCellBlocked(2, 2));
        }

        // Two props sharing a cell each hold it blocked. Removing one must not open a hole under
        // the other — the reason props are counted rather than baked flat.
        [Test]
        public void OverlappingProps_StayBlockedUntilBothAreRemoved()
        {
            var first = new Rect(1f, 1f, 1f, 1f);
            var second = new Rect(1.5f, 1.5f, 1f, 1f);
            map.Stamp(first);
            map.Stamp(second);

            map.Unstamp(first);

            Assert.IsTrue(map.IsCellBlocked(3, 3), "The second prop still covers this cell.");
            map.Unstamp(second);
            Assert.IsFalse(map.IsCellBlocked(3, 3));
        }

        [Test]
        public void UnstampingSomethingNeverStamped_DoesNotGoNegative()
        {
            var footprint = new Rect(1f, 1f, 0.5f, 0.5f);
            map.Unstamp(footprint);
            map.Stamp(footprint);

            Assert.IsTrue(map.IsCellBlocked(2, 2), "A stray unstamp must not leave a cell that can never block.");
        }

        // A footprint that stops exactly on a cell boundary is beside that cell, not inside it.
        // Without this, standing flush against a wall would read as being in it.
        [Test]
        public void ARectEndingExactlyOnABlockedCellsEdge_IsNotBlocked()
        {
            map.SetGroundBlocked(4, 0, true);   // covers world x 2.0 to 2.5

            Assert.IsFalse(map.IsRectBlocked(new Rect(1.5f, 0f, 0.5f, 0.25f)),
                "A rect ending at x=2.0 only touches the blocked cell's edge.");
            Assert.IsTrue(map.IsRectBlocked(new Rect(1.6f, 0f, 0.5f, 0.25f)),
                "Overlapping the blocked cell by any amount blocks.");
        }

        [Test]
        public void ARectStraddlingSeveralCells_BlocksIfAnyOfThemDoes()
        {
            map.SetGroundBlocked(5, 2, true);

            Assert.IsTrue(map.IsRectBlocked(new Rect(2.0f, 1.0f, 1.0f, 0.5f)));
        }

        [Test]
        public void ARectHangingOffTheEdge_IsBlocked()
        {
            Assert.IsTrue(map.IsRectBlocked(new Rect(-0.25f, 1f, 0.5f, 0.25f)));
        }

        [Test]
        public void DimensionsAreReportedInTiles()
        {
            Assert.AreEqual(4f, map.WidthInTiles);
            Assert.AreEqual(4f, map.HeightInTiles);
        }
    }
}
