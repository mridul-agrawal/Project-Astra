using NUnit.Framework;
using ProjectAstra.Core.Animation;
using ProjectAstra.Core.Hub;

namespace ProjectAstra.Core.Tests.Hub
{
    // One test per rule in the movement spec's direction-resolution section.
    [TestFixture]
    public class CardinalInputResolverTests
    {
        private CardinalInputResolver resolver;

        [SetUp]
        public void SetUp() => resolver = new CardinalInputResolver();

        private Facing? Frame(bool north = false, bool south = false, bool east = false, bool west = false) =>
            resolver.Resolve(north, south, east, west);

        [Test]
        public void NothingHeld_IsNoDirection()
        {
            Assert.IsNull(Frame());
        }

        [TestCase(true, false, false, false, Facing.North)]
        [TestCase(false, true, false, false, Facing.South)]
        [TestCase(false, false, true, false, Facing.East)]
        [TestCase(false, false, false, true, Facing.West)]
        public void OneDirectionHeld_WalksThatWay(bool north, bool south, bool east, bool west, Facing expected)
        {
            Assert.AreEqual(expected, Frame(north, south, east, west));
        }

        [Test]
        public void TwoPerpendicularDirections_TheMoreRecentPressWins()
        {
            Frame(north: true);
            Assert.AreEqual(Facing.East, Frame(north: true, east: true));
        }

        [Test]
        public void TwoPerpendicularDirectionsPressedOnTheSameUpdate_PrefersHorizontal()
        {
            Assert.AreEqual(Facing.East, Frame(north: true, east: true));
        }

        [Test]
        public void ReleasingTheActiveDirection_ResumesTheHeldOneWithoutANewPress()
        {
            Frame(north: true);
            Frame(north: true, east: true);

            Assert.AreEqual(Facing.North, Frame(north: true),
                "Letting go of the newer direction should fall straight back to the one still held.");
        }

        [Test]
        public void BothDirectionsOnOneAxis_LeaveThatAxisNeutral()
        {
            Frame(east: true);
            Assert.IsNull(Frame(east: true, west: true));
        }

        [Test]
        public void ANeutralAxis_DoesNotBlockTheOtherOne()
        {
            Frame(east: true, west: true);
            Assert.AreEqual(Facing.North, Frame(east: true, west: true, north: true));
        }

        [Test]
        public void ReleasingOneSideOfANeutralAxis_ResumesTheSideStillHeld()
        {
            Frame(east: true);
            Frame(east: true, west: true);

            Assert.AreEqual(Facing.East, Frame(east: true));
        }

        [Test]
        public void HoldingEverything_StillNeverProducesADiagonal()
        {
            Facing? resolved = Frame(north: true, south: true, east: true, west: true);

            Assert.IsNull(resolved, "Both axes neutral means standing still, not a diagonal.");
        }

        [Test]
        public void Clear_StopsMovementUntilInputIsSeenAgain()
        {
            Frame(east: true);
            resolver.Clear();

            // The button is still down, but the release/press history is gone — so this frame reads
            // as a fresh press and walking resumes, which is what "until valid input resumes" means.
            Assert.AreEqual(Facing.East, Frame(east: true));
        }

        [Test]
        public void ADirectionHeldThroughSeveralFrames_KeepsWalking()
        {
            Frame(west: true);
            Frame(west: true);

            Assert.AreEqual(Facing.West, Frame(west: true));
        }
    }
}
