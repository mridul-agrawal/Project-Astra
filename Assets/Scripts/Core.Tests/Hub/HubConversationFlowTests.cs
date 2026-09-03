using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using ProjectAstra.Core.Dialogue;
using ProjectAstra.Core.Editor;

namespace ProjectAstra.Core.Tests.Hub
{
    // The flow down the middle of the conversation editor is what a writer reads instead of the
    // script. If it disagrees with what the runner would do, it is worse than nothing.
    [TestFixture]
    public class HubConversationFlowTests
    {
        private readonly List<Object> made = new();

        [TearDown]
        public void TearDown()
        {
            foreach (Object one in made) Object.DestroyImmediate(one);
            made.Clear();
        }

        [Test]
        public void AScriptWithNoLabelsIsOneRun()
        {
            IReadOnlyList<HubConversationFlow.Block> flow =
                HubConversationFlow.Read(Script(Line(), Line(), Line()));

            Assert.AreEqual(1, flow.Count);
            Assert.AreEqual(3, flow[0].Count);
            Assert.AreEqual("the opening", flow[0].Name);
        }

        [Test]
        public void ARunWithNothingAfterItEndsThere()
        {
            HubConversationFlow.Block only = HubConversationFlow.Read(Script(Line()))[0];

            Assert.AreEqual(1, only.Exits.Count);
            Assert.IsTrue(only.Exits[0].Ends);
        }

        [Test]
        public void ALabelStartsANewRun()
        {
            IReadOnlyList<HubConversationFlow.Block> flow =
                HubConversationFlow.Read(Script(Line(), Line(label: "after"), Line()));

            Assert.AreEqual(2, flow.Count);
            Assert.AreEqual("after", flow[1].Label);
            Assert.AreEqual(2, flow[1].Count);
        }

        [Test]
        public void ARunWithoutABranchRunsOnIntoTheNext()
        {
            IReadOnlyList<HubConversationFlow.Block> flow =
                HubConversationFlow.Read(Script(Line(), Line(label: "after")));

            Assert.AreEqual("after", flow[0].Exits[0].TargetLabel);
            Assert.IsFalse(flow[0].Exits[0].Dangling);
        }

        [Test]
        public void AJumpGoesWhereItSays()
        {
            IReadOnlyList<HubConversationFlow.Block> flow =
                HubConversationFlow.Read(Script(Jump("elsewhere"), Line(label: "elsewhere")));

            Assert.AreEqual("elsewhere", flow[0].Exits[0].TargetLabel);
            Assert.IsFalse(flow[0].Exits[0].Dangling);
        }

        // A branch to a label nothing carries silently ends the conversation at runtime, which is
        // exactly the mistake a writer cannot see in a list of lines.
        [Test]
        public void AJumpToNowhereIsMarked()
        {
            IReadOnlyList<HubConversationFlow.Block> flow =
                HubConversationFlow.Read(Script(Jump("nowhere")));

            Assert.IsTrue(flow[0].Exits[0].Dangling);
        }

        [Test]
        public void AChoiceHasOneWayOutPerAnswer()
        {
            DialogueScript script = Script(
                Choice(("yes", "agreed"), ("no", "refused")),
                Line(label: "agreed"), Line(label: "refused"));

            Assert.AreEqual(2, HubConversationFlow.Read(script)[0].Exits.Count);
        }

        [Test]
        public void AChoiceThatCanBeBackedOutOfHasAWayOutForThatToo()
        {
            DialogueScript script = Script(
                Choice(allowCancel: true, cancelTo: "dropped", answers: ("yes", "agreed")),
                Line(label: "agreed"), Line(label: "dropped"));

            Assert.AreEqual(2, HubConversationFlow.Read(script)[0].Exits.Count);
            Assert.IsTrue(HubConversationFlow.Read(script)[0].Exits.Any(e => e.Reads == "backs out"));
        }

        // Nothing continues past a jump, so lines written after one in the same run are dead.
        [Test]
        public void LinesAfterABranchAreCountedAsUnreachable()
        {
            HubConversationFlow.Block only =
                HubConversationFlow.Read(Script(Jump("nowhere"), Line(), Line()))[0];

            Assert.AreEqual(2, only.Unreachable);
        }

        [Test]
        public void ACleanScriptHasNothingWrongWithIt()
        {
            Assert.IsEmpty(HubConversationFlow.Problems(
                Script(Line(), Jump("end"), Line(label: "end"))));
        }

        [Test]
        public void ABranchToNowhereIsReported()
        {
            Assert.IsTrue(HubConversationFlow.Problems(Script(Jump("nowhere")))
                .Any(problem => problem.Contains("nowhere")));
        }

        [Test]
        public void DeadLinesAreReported()
        {
            Assert.IsTrue(HubConversationFlow.Problems(Script(Jump("end"), Line(), Line(label: "end")))
                .Any(problem => problem.Contains("nothing reaches them")));
        }

        [Test]
        public void TwoEntriesWithTheSameLabelAreReported()
        {
            Assert.IsTrue(HubConversationFlow.Problems(
                    Script(Line(), Line(label: "twice"), Line(label: "twice")))
                .Any(problem => problem.Contains("both labelled")));
        }

        [Test]
        public void NothingIsReadFromNothing()
        {
            Assert.IsEmpty(HubConversationFlow.Read(null));
        }

        // --- Building a script by hand ---

        private static (string text, string label, DialogueNodeKind kind, string target,
            (string, string)[] answers, bool cancel, string cancelTo) Line(string label = null) =>
            ("a line", label, DialogueNodeKind.Line, null, null, false, null);

        private static (string, string, DialogueNodeKind, string, (string, string)[], bool, string)
            Jump(string target, string label = null) =>
            (null, label, DialogueNodeKind.Jump, target, null, false, null);

        private static (string, string, DialogueNodeKind, string, (string, string)[], bool, string)
            Choice(params (string, string)[] answers) =>
            (null, null, DialogueNodeKind.Choice, null, answers, false, null);

        private static (string, string, DialogueNodeKind, string, (string, string)[], bool, string)
            Choice(bool allowCancel, string cancelTo, params (string, string)[] answers) =>
            (null, null, DialogueNodeKind.Choice, null, answers, allowCancel, cancelTo);

        private DialogueScript Script(
            params (string text, string label, DialogueNodeKind kind, string target,
                (string, string)[] answers, bool cancel, string cancelTo)[] entries)
        {
            var script = ScriptableObject.CreateInstance<DialogueScript>();
            made.Add(script);

            var editable = new SerializedObject(script);
            editable.FindProperty("scriptId").stringValue = "under_test";

            SerializedProperty segments = editable.FindProperty("segments");
            segments.arraySize = 1;
            SerializedProperty lines = segments.GetArrayElementAtIndex(0).FindPropertyRelative("lines");
            lines.arraySize = entries.Length;

            for (int i = 0; i < entries.Length; i++) Write(lines.GetArrayElementAtIndex(i), entries[i]);
            editable.ApplyModifiedPropertiesWithoutUndo();

            return script;
        }

        private static void Write(SerializedProperty entry,
            (string text, string label, DialogueNodeKind kind, string target,
                (string, string)[] answers, bool cancel, string cancelTo) from)
        {
            entry.FindPropertyRelative("text").stringValue = from.text;
            entry.FindPropertyRelative("label").stringValue = from.label;
            entry.FindPropertyRelative("kind").enumValueIndex = (int)from.kind;
            entry.FindPropertyRelative("targetLabel").stringValue = from.target;
            entry.FindPropertyRelative("allowCancel").boolValue = from.cancel;
            entry.FindPropertyRelative("cancelTargetLabel").stringValue = from.cancelTo;

            SerializedProperty options = entry.FindPropertyRelative("options");
            options.arraySize = from.answers?.Length ?? 0;

            for (int i = 0; i < options.arraySize; i++)
            {
                SerializedProperty option = options.GetArrayElementAtIndex(i);
                option.FindPropertyRelative("label").stringValue = from.answers[i].Item1;
                option.FindPropertyRelative("targetLabel").stringValue = from.answers[i].Item2;
            }
        }
    }
}
