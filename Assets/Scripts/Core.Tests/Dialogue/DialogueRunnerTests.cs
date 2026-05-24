using System.Collections.Generic;
using NUnit.Framework;
using ProjectAstra.Core.Dialogue;

namespace ProjectAstra.Core.Tests.Dialogue
{
    [TestFixture]
    public class DialogueRunnerTests
    {
        private const float Speed = 10f; // chars/second — 1 char per 0.1s

        private FakeView _view;
        private DialogueSpeakerRegistry _registry;
        private int _completeCount;

        [SetUp]
        public void SetUp()
        {
            _view = new FakeView();
            _registry = DialogueSpeakerRegistry.CreateForTest(
                DialogueSpeaker.CreateForTest("A", "Arya"),
                DialogueSpeaker.CreateForTest("B", "Bhima"));
            _completeCount = 0;
        }

        private DialogueRunner Build(DialogueScript script)
        {
            var runner = new DialogueRunner(script, _registry, _view, DialogueContext.Cutscene, Speed);
            runner.OnComplete += () => _completeCount++;
            return runner;
        }

        [Test]
        public void Start_ShowsViewAndFirstLine()
        {
            var script = DialogueScript.CreateForTest("S",
                DialogueNode.CreateForTest(0, "A", "Hello"));

            Build(script).Start();

            Assert.AreEqual(1, _view.ShowCount);
            Assert.AreEqual(1, _view.Lines.Count);
            Assert.AreEqual("Hello", _view.Lines[0].Text);
            Assert.AreEqual("Arya", _view.Lines[0].SpeakerName);
            Assert.AreEqual(0, _view.LastVisible, "line starts hidden, crawl reveals it");
        }

        [Test]
        public void Tick_RevealsCharactersAtConfiguredSpeed()
        {
            var script = DialogueScript.CreateForTest("S",
                DialogueNode.CreateForTest(0, "A", "Hello")); // 5 chars
            var runner = Build(script);
            runner.Start();

            runner.Tick(0.25f); // 2.5 chars revealed
            Assert.AreEqual(2, _view.LastVisible);

            runner.Tick(0.25f); // 5.0 chars revealed → complete
            Assert.AreEqual(5, _view.LastVisible);
            Assert.IsTrue(_view.HintVisible, "continue hint appears once the line finishes");
        }

        [Test]
        public void Confirm_DuringCrawl_SnapsLineToFull()
        {
            var script = DialogueScript.CreateForTest("S",
                DialogueNode.CreateForTest(0, "A", "Hello"));
            var runner = Build(script);
            runner.Start();

            runner.Confirm();

            Assert.AreEqual(5, _view.LastVisible);
            Assert.IsTrue(_view.HintVisible);
            Assert.AreEqual(1, _view.Lines.Count, "still on the first line, not advanced");
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

            Assert.AreEqual(2, _view.Lines.Count);
            Assert.AreEqual("Reply", _view.Lines[1].Text);
            Assert.AreEqual("Bhima", _view.Lines[1].SpeakerName);
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
            Assert.IsFalse(_view.HintVisible, "auto-advancing lines don't show the press-to-continue hint");
            Assert.AreEqual(1, _view.Lines.Count);

            runner.Tick(0.5f); // auto-advance fires
            Assert.AreEqual(2, _view.Lines.Count);
            Assert.AreEqual("Next", _view.Lines[1].Text);
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

            Assert.AreEqual(1, _completeCount);
            Assert.AreEqual(1, _view.HideCount);
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

            Assert.AreEqual(1, _view.Lines.Count, "the missing-speaker line is skipped");
            Assert.AreEqual("Seen", _view.Lines[0].Text);
        }

        [Test]
        public void AllNodesMissing_FinishesWithoutShowingAnything()
        {
            var script = DialogueScript.CreateForTest("S",
                DialogueNode.CreateForTest(0, "GHOST", "Unseen"));
            var runner = Build(script);

            runner.Start();

            Assert.AreEqual(0, _view.Lines.Count);
            Assert.AreEqual(1, _completeCount);
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

            Assert.AreEqual(1, _completeCount);
            Assert.AreEqual(1, _view.HideCount);
            Assert.IsFalse(runner.IsRunning);
            Assert.AreEqual(1, _view.Lines.Count, "skip stops after the first line was shown");
        }

        [Test]
        public void NarratorLine_ShowsTextWithNoSpeakerName()
        {
            var script = DialogueScript.CreateForTest("S",
                DialogueNode.CreateForTest(0, DialogueSpeakerRegistry.NarratorId, "The village burned.",
                    position: PortraitPosition.None));
            var runner = Build(script);

            runner.Start();

            Assert.AreEqual(1, _view.Lines.Count);
            Assert.AreEqual("The village burned.", _view.Lines[0].Text);
            Assert.AreEqual("", _view.Lines[0].SpeakerName);
            Assert.IsNull(_view.Lines[0].Portrait);
        }

        [Test]
        public void EmptyText_CompletesCrawlImmediately()
        {
            var script = DialogueScript.CreateForTest("S",
                DialogueNode.CreateForTest(0, "A", ""));
            var runner = Build(script);

            runner.Start();

            Assert.IsTrue(_view.HintVisible, "an empty line is instantly 'complete' and waits for confirm");
        }

        private sealed class FakeView : IDialogueView
        {
            public int ShowCount;
            public int HideCount;
            public int LastVisible;
            public bool HintVisible;
            public readonly List<DialogueLineView> Lines = new();

            public void Show(DialogueContext context) => ShowCount++;

            public void ShowLine(in DialogueLineView line)
            {
                Lines.Add(line);
                LastVisible = 0;
                HintVisible = false;
            }

            public void SetVisibleCharacters(int count) => LastVisible = count;
            public void SetContinueHintVisible(bool visible) => HintVisible = visible;
            public void Hide() => HideCount++;
        }
    }
}
