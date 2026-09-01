using System.Collections.Generic;
using NUnit.Framework;
using ProjectAstra.Core.Dialogue;

namespace ProjectAstra.Core.Tests.Dialogue
{
    [TestFixture]
    public class DialogueRunnerTests
    {
        private const float Speed = 10f; // chars/second — 1 char per 0.1s

        private FakeView view;
        private DialogueSpeakerRegistry registry;
        private int completeCount;

        [SetUp]
        public void SetUp()
        {
            view = new FakeView();
            registry = DialogueSpeakerRegistry.CreateForTest(
                DialogueSpeaker.CreateForTest("A", "Arya"),
                DialogueSpeaker.CreateForTest("B", "Bhima"));
            completeCount = 0;
        }

        private DialogueRunner Build(DialogueScript script)
        {
            var runner = new DialogueRunner(script, registry, view, DialogueTriggeringContext.Cutscene, Speed);
            runner.OnComplete += () => completeCount++;
            return runner;
        }

        [Test]
        public void Start_ShowsViewAndFirstLine()
        {
            var script = DialogueScript.CreateForTest("S",
                DialogueNode.CreateForTest(0, "A", "Hello"));

            Build(script).Start();

            Assert.AreEqual(1, view.ShowCount);
            Assert.AreEqual(1, view.Lines.Count);
            Assert.AreEqual("Hello", view.Lines[0].Text);
            Assert.AreEqual("Arya", view.Lines[0].SpeakerName);
            Assert.AreEqual(0, view.LastVisible, "line starts hidden, crawl reveals it");
        }

        [Test]
        public void Tick_RevealsCharactersAtConfiguredSpeed()
        {
            var script = DialogueScript.CreateForTest("S",
                DialogueNode.CreateForTest(0, "A", "Hello")); // 5 chars
            var runner = Build(script);
            runner.Start();

            runner.Tick(0.25f); // 2.5 chars revealed
            Assert.AreEqual(2, view.LastVisible);

            runner.Tick(0.25f); // 5.0 chars revealed → complete
            Assert.AreEqual(5, view.LastVisible);
            Assert.IsTrue(view.HintVisible, "continue hint appears once the line finishes");
        }

        [Test]
        public void Confirm_DuringCrawl_SnapsLineToFull()
        {
            var script = DialogueScript.CreateForTest("S",
                DialogueNode.CreateForTest(0, "A", "Hello"));
            var runner = Build(script);
            runner.Start();

            runner.Confirm();

            Assert.AreEqual(5, view.LastVisible);
            Assert.IsTrue(view.HintVisible);
            Assert.AreEqual(1, view.Lines.Count, "still on the first line, not advanced");
        }

        [Test]
        public void Confirm_AfterComplete_AdvancesToNextLine()
        {
            var script = DialogueScript.CreateForTest("S",
                DialogueNode.CreateForTest(0, "A", "Hello"),
                DialogueNode.CreateForTest(1, "B", "Reply"));
            var runner = Build(script);
            runner.Start();

            runner.Confirm(); // complete line 1
            runner.Confirm(); // advance to line 2

            Assert.AreEqual(2, view.Lines.Count);
            Assert.AreEqual("Reply", view.Lines[1].Text);
            Assert.AreEqual("Bhima", view.Lines[1].SpeakerName);
        }

        [Test]
        public void AutoAdvance_AdvancesWithoutInput_AfterDelay()
        {
            var script = DialogueScript.CreateForTest("S",
                DialogueNode.CreateForTest(0, "A", "Hi", autoAdvanceDelay: 0.5f),
                DialogueNode.CreateForTest(1, "B", "Next"));
            var runner = Build(script);
            runner.Start();

            runner.Tick(0.3f); // crawl completes (2 chars at 10/s)
            Assert.IsFalse(view.HintVisible, "auto-advancing lines don't show the press-to-continue hint");
            Assert.AreEqual(1, view.Lines.Count);

            runner.Tick(0.5f); // auto-advance fires
            Assert.AreEqual(2, view.Lines.Count);
            Assert.AreEqual("Next", view.Lines[1].Text);
        }

        [Test]
        public void ReachingEnd_HidesViewAndRaisesOnComplete()
        {
            var script = DialogueScript.CreateForTest("S",
                DialogueNode.CreateForTest(0, "A", "Only line"));
            var runner = Build(script);
            runner.Start();

            runner.Confirm(); // complete
            runner.Confirm(); // advance past the end

            Assert.AreEqual(1, completeCount);
            Assert.AreEqual(1, view.HideCount);
            Assert.IsFalse(runner.IsRunning);
        }

        [Test]
        public void MissingSpeaker_SkipsLineAndContinues()
        {
            var script = DialogueScript.CreateForTest("S",
                DialogueNode.CreateForTest(0, "GHOST", "Unseen"),
                DialogueNode.CreateForTest(1, "A", "Seen"));
            var runner = Build(script);

            runner.Start();

            Assert.AreEqual(1, view.Lines.Count, "the missing-speaker line is skipped");
            Assert.AreEqual("Seen", view.Lines[0].Text);
        }

        [Test]
        public void AllNodesMissing_FinishesWithoutShowingAnything()
        {
            var script = DialogueScript.CreateForTest("S",
                DialogueNode.CreateForTest(0, "GHOST", "Unseen"));
            var runner = Build(script);

            runner.Start();

            Assert.AreEqual(0, view.Lines.Count);
            Assert.AreEqual(1, completeCount);
            Assert.IsFalse(runner.IsRunning);
        }

        [Test]
        public void Skip_EndsImmediately()
        {
            var script = DialogueScript.CreateForTest("S",
                DialogueNode.CreateForTest(0, "A", "One"),
                DialogueNode.CreateForTest(1, "B", "Two"),
                DialogueNode.CreateForTest(2, "A", "Three"));
            var runner = Build(script);
            runner.Start();

            runner.Skip();

            Assert.AreEqual(1, completeCount);
            Assert.AreEqual(1, view.HideCount);
            Assert.IsFalse(runner.IsRunning);
            Assert.AreEqual(1, view.Lines.Count, "skip stops after the first line was shown");
        }

        [Test]
        public void NarratorLine_ShowsTextWithNoSpeakerName()
        {
            var script = DialogueScript.CreateForTest("S",
                DialogueNode.CreateForTest(0, DialogueSpeakerRegistry.NarratorId, "The village burned.",
                    position: PortraitPosition.None));
            var runner = Build(script);

            runner.Start();

            Assert.AreEqual(1, view.Lines.Count);
            Assert.AreEqual("The village burned.", view.Lines[0].Text);
            Assert.AreEqual("", view.Lines[0].SpeakerName);
            Assert.IsNull(view.Lines[0].Portrait);
        }

        [Test]
        public void EmptyText_CompletesCrawlImmediately()
        {
            var script = DialogueScript.CreateForTest("S",
                DialogueNode.CreateForTest(0, "A", ""));
            var runner = Build(script);

            runner.Start();

            Assert.IsTrue(view.HintVisible, "an empty line is instantly 'complete' and waits for confirm");
        }

        [Test]
        public void PortraitFacing_ThreadsThroughToLine()
        {
            var script = DialogueScript.CreateForTest("S",
                DialogueNode.CreateForTest(0, "A", "Hi", facing: PortraitFacing.Right));

            Build(script).Start();

            Assert.AreEqual(PortraitFacing.Right, view.Lines[0].Facing);
        }

        [Test]
        public void Typewriter_OnlyWritesViewWhenVisibleCountChanges()
        {
            var script = DialogueScript.CreateForTest("S",
                DialogueNode.CreateForTest(0, "A", "Hi")); // 2 chars, Speed = 10/s
            var runner = Build(script);
            runner.Start();
            int afterStart = view.VisibleSetCount; // BeginCrawl wrote 0 once

            for (int i = 0; i < 20; i++) runner.Tick(0.001f); // 0.02s total -> 0.2 chars, still 0
            Assert.AreEqual(afterStart, view.VisibleSetCount, "no redundant view writes while the count is unchanged");

            runner.Tick(0.1f); // crosses to 1 char
            Assert.AreEqual(afterStart + 1, view.VisibleSetCount);
        }

        private sealed class FakeView : IDialogueView
        {
            public int ShowCount;
            public int HideCount;
            public int LastVisible;
            public bool HintVisible;
            public int VisibleSetCount;
            public readonly List<DialogueLineView> Lines = new();

            public void Show(DialogueTriggeringContext context) => ShowCount++;

            public void ShowLine(in DialogueLineView line)
            {
                Lines.Add(line);
                LastVisible = 0;
                HintVisible = false;
            }

            public void SetVisibleCharacters(int count) { LastVisible = count; VisibleSetCount++; }
            public void SetContinueHintVisible(bool visible) => HintVisible = visible;
            public void Hide() => HideCount++;

            public readonly List<string> ChoiceLabels = new();
            public readonly List<bool> ChoiceEnabled = new();
            public int Highlighted = -1;
            public bool ChoicesVisible;
            public int HideChoicesCount;

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

            public void HideChoices() { ChoicesVisible = false; HideChoicesCount++; }
        }
    }
}
