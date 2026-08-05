using NUnit.Framework;
using UnityEngine;
using ProjectAstra.Core.Cursor;

namespace ProjectAstra.Core.Tests.Cursor
{
    [TestFixture]
    public class CursorVisualsTests
    {
        private static CursorStateVisual Visual(float inset = 0.4f) => new()
        {
            tint = Color.white,
            inset = inset,
            pieceScale = 1f,
            breathAmplitude = 0f,
            breathPeriod = 1f,
            arrowsPointInward = false,
        };

        // --- Visual state mapping ---

        [TestCase(CursorHover.Empty, CursorVisualState.Idle)]
        [TestCase(CursorHover.ReadyAlly, CursorVisualState.Selectable)]
        [TestCase(CursorHover.ActedAlly, CursorVisualState.Acted)]
        [TestCase(CursorHover.Enemy, CursorVisualState.Enemy)]
        public void VisualState_WhileFree_FollowsHover(CursorHover hover, CursorVisualState expected)
        {
            Assert.AreEqual(expected, CursorVisualStateMap.From(CursorState.Free, hover));
        }

        [TestCase(CursorState.Selected)]
        [TestCase(CursorState.Moving)]
        [TestCase(CursorState.ActionMenu)]
        [TestCase(CursorState.Targeting)]
        public void VisualState_OncePickedUp_StaysSelected(CursorState state)
        {
            Assert.AreEqual(CursorVisualState.Selected, CursorVisualStateMap.From(state, CursorHover.Enemy));
        }

        // --- Slot geometry ---

        [Test]
        public void CornersAndEdges_SitTheSameDistanceFromCentre()
        {
            float cornerDistance = CursorSlotGeometry.DirectionOf((int)CursorSlot.CornerNE).magnitude;
            float edgeDistance = CursorSlotGeometry.DirectionOf((int)CursorSlot.EdgeN).magnitude;

            Assert.AreEqual(edgeDistance, cornerDistance, 0.0001f,
                "A corner piece must not fly out further than an edge piece for the same inset.");
        }

        [Test]
        public void EveryCornerMorphsToADistinctEdge()
        {
            var seen = new System.Collections.Generic.HashSet<int>();
            for (int corner = 0; corner < CursorSlotGeometry.CornerCount; corner++)
            {
                int edge = CursorSlotGeometry.EdgeSlotForCorner(corner);
                Assert.IsFalse(CursorSlotGeometry.IsCorner(edge));
                Assert.IsTrue(seen.Add(edge), "Two corners morphed into the same edge.");
            }
        }

        // --- Directional hints ---

        [Test]
        public void DirectionalHints_HideStepsOutsideTheReachableSet()
        {
            var reachable = new System.Collections.Generic.HashSet<Vector2Int> { new(3, 4), new(2, 3) };
            var into = new bool[DirectionalHintModule.DirectionCount];

            DirectionalHintModule.Compute(into, new Vector2Int(3, 3), reachable.Contains);

            Assert.IsTrue(into[0], "North (3,4) is reachable.");
            Assert.IsTrue(into[1], "West (2,3) is reachable.");
            Assert.IsFalse(into[2], "South (3,2) is not reachable.");
            Assert.IsFalse(into[3], "East (4,3) is not reachable.");
        }

        [Test]
        public void DirectionalHints_WithNoTest_ShowEverything()
        {
            var into = new bool[DirectionalHintModule.DirectionCount];
            DirectionalHintModule.Compute(into, Vector2Int.zero, null);

            foreach (bool valid in into) Assert.IsTrue(valid);
        }

        // --- Morph targets ---

        [Test]
        public void Morph_OverAUnit_MovesCornerPiecesOntoEdges()
        {
            var targets = new CursorPose[CursorSlotGeometry.SlotCount];

            MorphDriver.WriteTargets(targets, Visual(), morphToEdges: true, validDirections: null);

            // An edge pose lies on an axis; a corner pose does not.
            for (int corner = 0; corner < CursorSlotGeometry.CornerCount; corner++)
            {
                Vector2 offset = targets[corner].offset;
                bool onAxis = Mathf.Approximately(offset.x, 0f) || Mathf.Approximately(offset.y, 0f);
                Assert.IsTrue(onAxis, $"Corner {corner} should have swept onto an edge.");
            }
        }

        [Test]
        public void Morph_NeverUsesTheDedicatedEdgeSlots()
        {
            var targets = new CursorPose[CursorSlotGeometry.SlotCount];

            MorphDriver.WriteTargets(targets, Visual(), morphToEdges: true, validDirections: null);

            for (int slot = CursorSlotGeometry.CornerCount; slot < CursorSlotGeometry.SlotCount; slot++)
                Assert.IsFalse(targets[slot].visible, "A morph variant only ever uses four pieces.");
        }

        // --- Pose interpolation ---

        [Test]
        public void PolarLerp_SweepsTheRequestedWayRound()
        {
            var from = new CursorPose { offset = new Vector2(0.4f, 0f), scale = 1f, visible = true };
            var to = new CursorPose { offset = new Vector2(-0.4f, 0f), scale = 1f, visible = true };

            var counterClockwise = CursorPose.PolarLerp(from, to, 0.5f, 1);
            var clockwise = CursorPose.PolarLerp(from, to, 0.5f, -1);

            Assert.Greater(counterClockwise.offset.y, 0f, "Counter-clockwise should pass above the centre.");
            Assert.Less(clockwise.offset.y, 0f, "Clockwise should pass below the centre.");
        }

        [Test]
        public void PolarLerp_KeepsPiecesOffTheCentre()
        {
            var from = new CursorPose { offset = new Vector2(0.4f, 0f), scale = 1f, visible = true };
            var to = new CursorPose { offset = new Vector2(-0.4f, 0f), scale = 1f, visible = true };

            // A straight blend would collapse through zero halfway; a sweep must not.
            var swept = CursorPose.PolarLerp(from, to, 0.5f, 1);

            Assert.AreEqual(0.4f, swept.offset.magnitude, 0.0001f);
        }

        [Test]
        public void EdgeArrows_PointingInward_FlipAndTuckIn()
        {
            var outward = Visual();
            var inward = Visual();
            inward.arrowsPointInward = true;

            var outPose = EdgeArrowModule.PoseFor((int)CursorSlot.EdgeN, outward);
            var inPose = EdgeArrowModule.PoseFor((int)CursorSlot.EdgeN, inward);

            Assert.AreEqual(180f, Mathf.DeltaAngle(outPose.rotation, inPose.rotation), 0.01f);
            Assert.Less(inPose.offset.magnitude, outPose.offset.magnitude);
        }
    }
}
