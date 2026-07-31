using NUnit.Framework;
using UnityEngine;
using ProjectAstra.Core.Animation;

namespace ProjectAstra.Core.Tests.Animation
{
    [TestFixture]
    public class SpriteSheetGridTests
    {
        [Test]
        public void SliceByGrid_ProducesRowMajorCount()
        {
            var slices = SpriteSheetGrid.SliceByGrid(64, 32, 4, 2, "run");
            Assert.AreEqual(8, slices.Length);
        }

        [Test]
        public void SliceByGrid_TopLeftIsFirst_BottomLeftOrigin()
        {
            var slices = SpriteSheetGrid.SliceByGrid(64, 32, 4, 2, "run");   // 16x16 cells
            // Frame 0 = top-left: x=0, y = texHeight - cellHeight = 16
            Assert.AreEqual(new Rect(0, 16, 16, 16), slices[0].Rect);
            Assert.AreEqual("run_0", slices[0].Name);
            // Frame 4 = start of the second (bottom) row: x=0, y=0
            Assert.AreEqual(new Rect(0, 0, 16, 16), slices[4].Rect);
            Assert.AreEqual("run_4", slices[4].Name);
        }

        [Test]
        public void SliceByCell_MatchesGrid()
        {
            var slices = SpriteSheetGrid.SliceByCell(64, 32, 16, 16, "x");
            Assert.AreEqual(8, slices.Length);
            Assert.AreEqual(new Rect(48, 16, 16, 16), slices[3].Rect);   // top row, last column
        }

        [Test]
        public void RowLabels_NameFramesByRow()
        {
            var slices = SpriteSheetGrid.SliceByGrid(48, 32, 3, 2, "c", new[] { "idle", "run_n" });
            Assert.AreEqual("idle_0", slices[0].Name);
            Assert.AreEqual("idle_2", slices[2].Name);
            Assert.AreEqual("run_n_0", slices[3].Name);
            Assert.AreEqual("run_n_2", slices[5].Name);
        }

        [Test]
        public void NonDivisibleWidth_FloorsCellSize_IgnoresRemainder()
        {
            var slices = SpriteSheetGrid.SliceByGrid(70, 32, 4, 2, "x");   // 70/4 = 17, 2px remainder
            Assert.AreEqual(8, slices.Length);
            Assert.AreEqual(17, slices[0].Rect.width);
        }

        [Test]
        public void SingleRowStrip_Works()
        {
            var slices = SpriteSheetGrid.SliceByGrid(64, 16, 4, 1, "walk");
            Assert.AreEqual(4, slices.Length);
            Assert.AreEqual(new Rect(0, 0, 16, 16), slices[0].Rect);
        }

        [Test]
        public void ZeroColumns_ReturnsEmpty()
        {
            Assert.IsEmpty(SpriteSheetGrid.SliceByGrid(64, 32, 0, 2, "x"));
            Assert.IsEmpty(SpriteSheetGrid.SliceByCell(64, 32, 0, 16, "x"));
        }
    }
}
