using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using ProjectAstra.Core.Dialogue;

namespace ProjectAstra.Core.Tests.Dialogue
{
    // The rules that used to live in ConversationRunner and ChoiceMenuController, now pinned
    // against the one runner that does both.
    [TestFixture]
    public class DialogueBranchingTests
    {
        private const float Speed = 1000f; // effectively instant crawl

        private FakeView view;
        private DialogueSpeakerRegistry registry;
        private FakeMemory memory;

        [SetUp]
        public void SetUp()
        {
            view = new FakeView();
            memory = new FakeMemory();
            registry = DialogueSpeakerRegistry.CreateForTest(DialogueSpeaker.CreateForTest("A", "Arya"));
        }

        private DialogueRunner Run(DialogueScript script, IDialogueMemory mem = null)
        {
            var runner = new DialogueRunner(script, registry, view, DialogueTriggeringContext.Cutscene, Speed, mem);
            runner.Start();
            return runner;
        }

        private static DialogueNode Line(int id, string text, string label = null) =>
            label == null
                ? DialogueNode.CreateForTest(id, "A", text)
                : DialogueNode.CreateLabelledLineForTest(id, "A", text, label);


        // Choice presentation:

        [Test]
        public void AChoice_PutsItsOptionsOnScreenWithTheFirstHighlighted()
        {
            var script = DialogueScript.CreateForTest("S",
                DialogueNode.CreateChoiceForTest(0, "ask", false, null,
                    DialogueOption.Create("a", "Ask about Asur", "asur"),
                    DialogueOption.Create("b", "Ask about theory", "theory")),
                Line(1, "Asur.", "asur"),
                Line(2, "Theory.", "theory"));

            var runner = Run(script);

            Assert.IsTrue(runner.AwaitingChoice);
            CollectionAssert.AreEqual(new[] { "Ask about Asur", "Ask about theory" }, view.ChoiceLabels);
            Assert.AreEqual(0, view.Highlighted);
        }

        [Test]
        public void ConfirmingAnOption_FollowsItsTarget()
        {
            var script = DialogueScript.CreateForTest("S",
                DialogueNode.CreateChoiceForTest(0, "ask", false, null,
                    DialogueOption.Create("a", "First", "one"),
                    DialogueOption.Create("b", "Second", "two")),
                Line(1, "ONE", "one"),
                Line(2, "TWO", "two"));

            var runner = Run(script);
            runner.MoveSelection(1);
            runner.Confirm();

            Assert.IsFalse(runner.AwaitingChoice);
            Assert.AreEqual("TWO", view.Lines[^1].Text);
        }

        [Test]
        public void MovingTheSelection_WrapsAround()
        {
            var runner = Run(TwoOptionScript());

            runner.MoveSelection(-1);

            Assert.AreEqual(1, view.Highlighted);
        }

        [Test]
        public void ATextAdvance_CannotSkipPastAChoice()
        {
            var runner = Run(TwoOptionScript());

            runner.Skip();

            Assert.IsTrue(runner.AwaitingChoice, "a required answer must survive a skip");
            Assert.IsTrue(runner.IsRunning);
        }


        // Memory — greying and first-time/repeat:

        [Test]
        public void AnAskOnceOptionAlreadyTaken_IsShownGreyedNotRemoved()
        {
            memory.Chosen.Add(("S", "a"));

            Run(AskOnceScript(), memory);

            CollectionAssert.AreEqual(new[] { "Asur", "Theory" }, view.ChoiceLabels,
                "the menu must not reshuffle between visits");
            CollectionAssert.AreEqual(new[] { false, true }, view.ChoiceEnabled);
            Assert.AreEqual(1, view.Highlighted, "the cursor never rests on a used topic");
        }

        [Test]
        public void AUsedTopic_CannotBeConfirmed()
        {
            memory.Chosen.Add(("S", "a"));
            var runner = Run(AskOnceScript(), memory);

            runner.MoveSelection(-1);   // would land on the used one if greying were ignored
            runner.Confirm();

            Assert.AreEqual(1, view.Highlighted);
        }

        [Test]
        public void PickingATopic_RecordsItSoTheNextVisitGreysIt()
        {
            var runner = Run(AskOnceScript(), memory);

            runner.Confirm();

            Assert.IsTrue(memory.HasChosen("S", "a"));
        }

        [Test]
        public void AScriptPlayedBefore_StartsAtItsRepeatLabel()
        {
            memory.Played.Add("S");
            var script = DialogueScript.CreateForTest("S", "again",
                Line(0, "First time"),
                Line(1, "Short bark.", "again"));

            Run(script, memory);

            Assert.AreEqual("Short bark.", view.Lines[0].Text);
        }

        [Test]
        public void AScriptNotPlayedBefore_StartsAtTheTop()
        {
            var script = DialogueScript.CreateForTest("S", "again",
                Line(0, "First time"),
                Line(1, "Short bark.", "again"));

            Run(script, memory);

            Assert.AreEqual("First time", view.Lines[0].Text);
        }

        [Test]
        public void FinishingAScript_MarksItPlayed()
        {
            var runner = Run(DialogueScript.CreateForTest("S", Line(0, "Only line")), memory);

            runner.Confirm();   // snap to full
            runner.Confirm();   // advance past the last node

            Assert.IsFalse(runner.IsRunning);
            Assert.IsTrue(memory.HasPlayed("S"));
        }


        // Jump and Signal:

        [Test]
        public void AJump_ContinuesAtTheLabelledNode()
        {
            var script = DialogueScript.CreateForTest("S",
                DialogueNode.CreateJumpForTest(0, null, "end"),
                Line(1, "Skipped"),
                Line(2, "Landed", "end"));

            Run(script);

            Assert.AreEqual(1, view.Lines.Count);
            Assert.AreEqual("Landed", view.Lines[0].Text);
        }

        // A quiz is a choice whose wrong answer points back at the question.
        [Test]
        public void AWrongAnswer_CanLoopBackToTheQuestion()
        {
            var script = DialogueScript.CreateForTest("S",
                Line(0, "Which is it?", "question"),
                DialogueNode.CreateChoiceForTest(1, "answers", false, null,
                    DialogueOption.Create("wrong", "Wrong", "retry"),
                    DialogueOption.Create("right", "Right", "correct")),
                Line(2, "No — try again.", "retry"),
                DialogueNode.CreateJumpForTest(3, null, "question"),
                Line(4, "Correct.", "correct"));

            var runner = Run(script);
            runner.Confirm();            // snap the question to full
            runner.Confirm();            // advance into the choice
            runner.Confirm();            // pick "Wrong"
            runner.Confirm();            // snap the correction to full
            runner.Confirm();            // advance, hit the jump, land back on the question

            Assert.AreEqual("Which is it?", view.Lines[^1].Text, "a wrong answer returns to the question");
        }

        [Test]
        public void ASignal_IsAnnouncedAndPlayContinues()
        {
            var raised = new List<string>();
            var script = DialogueScript.CreateForTest("S",
                DialogueNode.CreateSignalForTest(0, null, "learned_theory"),
                Line(1, "After."));

            var runner = new DialogueRunner(script, registry, view, DialogueTriggeringContext.Cutscene, Speed);
            runner.SignalRaised += raised.Add;
            runner.Start();

            CollectionAssert.AreEqual(new[] { "learned_theory" }, raised);
            Assert.AreEqual("After.", view.Lines[0].Text);
        }

        [Test]
        public void ASkippedSignal_StillLands()
        {
            var raised = new List<string>();
            var script = DialogueScript.CreateForTest("S",
                Line(0, "Talking."),
                DialogueNode.CreateSignalForTest(1, null, "objective_done"),
                Line(2, "More."));

            var runner = new DialogueRunner(script, registry, view, DialogueTriggeringContext.Cutscene, Speed);
            runner.SignalRaised += raised.Add;
            runner.Start();
            runner.Skip();

            CollectionAssert.AreEqual(new[] { "objective_done" }, raised,
                "a visually skipped effect must still occur");
        }


        // Cancel:

        [Test]
        public void CancelIsRefusedWhenTheAnswerIsRequired()
        {
            var runner = Run(TwoOptionScript());

            runner.Cancel();

            Assert.IsTrue(runner.AwaitingChoice, "a knowledge check can't be backed out of");
        }

        [Test]
        public void CancelFollowsItsTargetWhenAllowed()
        {
            var script = DialogueScript.CreateForTest("S",
                DialogueNode.CreateChoiceForTest(0, "ask", true, "out",
                    DialogueOption.Create("a", "Stay", "stay")),
                Line(1, "Stayed", "stay"),
                Line(2, "Backed out", "out"));

            var runner = Run(script);
            runner.Cancel();

            Assert.IsFalse(runner.AwaitingChoice);
            Assert.AreEqual("Backed out", view.Lines[^1].Text);
        }


        // Authoring mistakes are reported, not silently played through:

        [Test]
        public void AJumpWithNoTarget_EndsTheScript()
        {
            var script = DialogueScript.CreateForTest("S",
                Line(0, "Said"),
                DialogueNode.CreateJumpForTest(1, null, null),
                Line(2, "Never said"));

            var runner = Run(script);
            runner.Confirm();
            runner.Confirm();

            Assert.IsFalse(runner.IsRunning);
            Assert.AreEqual(1, view.Lines.Count);
        }

        [Test]
        public void AJumpToAMissingLabel_EndsTheScriptLoudly()
        {
            LogAssert.Expect(LogType.Error, new System.Text.RegularExpressions.Regex("jumps to 'nowhere'"));
            var script = DialogueScript.CreateForTest("S",
                DialogueNode.CreateJumpForTest(0, null, "nowhere"),
                Line(1, "Unreachable"));

            var runner = Run(script);

            Assert.IsFalse(runner.IsRunning);
        }

        [Test]
        public void JumpsThatLoopForever_GiveUpInsteadOfHanging()
        {
            LogAssert.Expect(LogType.Error, new System.Text.RegularExpressions.Regex("jumps loop"));
            var script = DialogueScript.CreateForTest("S",
                DialogueNode.CreateJumpForTest(0, "top", "bottom"),
                DialogueNode.CreateJumpForTest(1, "bottom", "top"));

            var runner = Run(script);

            Assert.IsFalse(runner.IsRunning);
        }


        private static DialogueScript TwoOptionScript() =>
            DialogueScript.CreateForTest("S",
                DialogueNode.CreateChoiceForTest(0, "ask", false, null,
                    DialogueOption.Create("a", "First", "one"),
                    DialogueOption.Create("b", "Second", "two")),
                Line(1, "ONE", "one"),
                Line(2, "TWO", "two"));

        private static DialogueScript AskOnceScript() =>
            DialogueScript.CreateForTest("S",
                DialogueNode.CreateChoiceForTest(0, "ask", false, null,
                    DialogueOption.Create("a", "Asur", "asur", askOnce: true),
                    DialogueOption.Create("b", "Theory", "theory", askOnce: true)),
                Line(1, "About Asur.", "asur"),
                Line(2, "About theory.", "theory"));

        private sealed class FakeMemory : IDialogueMemory
        {
            public readonly HashSet<string> Played = new();
            public readonly HashSet<(string, string)> Chosen = new();

            public bool HasPlayed(string scriptId) => Played.Contains(scriptId);
            public void MarkPlayed(string scriptId) => Played.Add(scriptId);
            public bool HasChosen(string scriptId, string optionId) => Chosen.Contains((scriptId, optionId));
            public void MarkChosen(string scriptId, string optionId) => Chosen.Add((scriptId, optionId));
        }

        private sealed class FakeView : IDialogueView
        {
            public readonly List<DialogueLineView> Lines = new();
            public readonly List<string> ChoiceLabels = new();
            public readonly List<bool> ChoiceEnabled = new();
            public int Highlighted = -1;
            public bool ChoicesVisible;

            public void Show(DialogueTriggeringContext context) { }
            public void ShowLine(in DialogueLineView line) => Lines.Add(line);
            public void SetVisibleCharacters(int count) { }
            public void SetContinueHintVisible(bool visible) { }
            public void Hide() { }

            public void ShowChoices(IReadOnlyList<DialogueChoiceView> options, int highlighted)
            {
                ChoiceLabels.Clear();
                ChoiceEnabled.Clear();
                foreach (DialogueChoiceView option in options)
                {
                    ChoiceLabels.Add(option.Label);
                    ChoiceEnabled.Add(option.Enabled);
                }
                Highlighted = highlighted;
                ChoicesVisible = true;
            }

            public void HideChoices() => ChoicesVisible = false;
        }
    }
}
