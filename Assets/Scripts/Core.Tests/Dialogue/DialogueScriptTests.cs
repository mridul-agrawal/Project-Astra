using NUnit.Framework;
using ProjectAstra.Core.Dialogue;

namespace ProjectAstra.Core.Tests.Dialogue
{
    [TestFixture]
    public class DialogueScriptTests
    {
        [Test]
        public void Nodes_FlattenSegments_InheritBackgroundSpeedAutoAdvance()
        {
            var script = DialogueScript.CreateForTestWithSegments("S",
                DialogueSegment.CreateForTest(null, 25f, 1.5f,
                    DialogueLine.CreateForTest("A", "one", position: PortraitPosition.Left),
                    DialogueLine.CreateForTest("B", "two", position: PortraitPosition.Right)));

            var nodes = script.Nodes;

            Assert.AreEqual(2, nodes.Count);
            Assert.AreEqual("A", nodes[0].SpeakerId);
            Assert.AreEqual("two", nodes[1].Text);
            Assert.AreEqual(PortraitPosition.Right, nodes[1].PortraitPosition);

            // segment-level values inherited onto every flattened node
            Assert.IsTrue(nodes[0].HasTextSpeedOverride);
            Assert.AreEqual(25f, nodes[0].TextSpeedOverride);
            Assert.IsTrue(nodes[0].AutoAdvances);
            Assert.AreEqual(1.5f, nodes[0].AutoAdvanceDelay);
        }

        [Test]
        public void Nodes_NumberFlattenedLinesSequentiallyAcrossSegments()
        {
            var script = DialogueScript.CreateForTestWithSegments("S",
                DialogueSegment.CreateForTest(null, -1f, 0f, DialogueLine.CreateForTest("A", "1")),
                DialogueSegment.CreateForTest(null, -1f, 0f,
                    DialogueLine.CreateForTest("B", "2"), DialogueLine.CreateForTest("A", "3")));

            var nodes = script.Nodes;

            Assert.AreEqual(3, nodes.Count);
            Assert.AreEqual(0, nodes[0].NodeId);
            Assert.AreEqual(1, nodes[1].NodeId);
            Assert.AreEqual(2, nodes[2].NodeId);
        }

        [Test]
        public void Nodes_FallBackToLegacyNodes_WhenNoSegments()
        {
            var script = DialogueScript.CreateForTest("S",
                DialogueNode.CreateForTest(0, "A", "legacy line"));

            var nodes = script.Nodes;

            Assert.AreEqual(1, nodes.Count);
            Assert.AreEqual("legacy line", nodes[0].Text);
        }
    }
}
