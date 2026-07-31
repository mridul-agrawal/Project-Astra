using NUnit.Framework;
using ProjectAstra.Core.Animation;

namespace ProjectAstra.Core.Tests.Animation
{
    [TestFixture]
    public class LocomotionStateTests
    {
        [Test]
        public void Moving_SuppressesSelected_AndSetsDirection()
        {
            var p = LocomotionState.Select(moving: true, Facing.East, selected: true);
            Assert.IsTrue(p.Moving);
            Assert.IsFalse(p.Selected, "Moving wins over Selected");
            Assert.AreEqual(1f, p.MoveX);
            Assert.AreEqual(0f, p.MoveY);
        }

        [Test]
        public void Still_AndSelected_ShowsSelected()
        {
            var p = LocomotionState.Select(moving: false, Facing.South, selected: true);
            Assert.IsFalse(p.Moving);
            Assert.IsTrue(p.Selected);
            Assert.AreEqual(0f, p.MoveX);
            Assert.AreEqual(0f, p.MoveY);
        }

        [Test]
        public void Still_AndNotSelected_IsIdle()
        {
            var p = LocomotionState.Select(moving: false, Facing.North, selected: false);
            Assert.IsFalse(p.Moving);
            Assert.IsFalse(p.Selected);
        }

        [TestCase(Facing.North, 0f, 1f)]
        [TestCase(Facing.South, 0f, -1f)]
        [TestCase(Facing.East, 1f, 0f)]
        [TestCase(Facing.West, -1f, 0f)]
        public void Direction_MapsToUnitVector(Facing facing, float expectedX, float expectedY)
        {
            var p = LocomotionState.Select(moving: true, facing, selected: false);
            Assert.AreEqual(expectedX, p.MoveX);
            Assert.AreEqual(expectedY, p.MoveY);
        }
    }
}
