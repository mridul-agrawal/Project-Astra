using System;
using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;
using ProjectAstra.Core.Dialogue;
using ProjectAstra.Core.Gurukul;
using ProjectAstra.Core.Gurukul.Conversation;

namespace ProjectAstra.Core.Tests.Gurukul
{
    // Walks whole graphs against a fake presenter, so a quiz's wrong-answer loop and a topic menu's
    // greying are proven without a Canvas or a dialogue box.
    [TestFixture]
    public class GurukulConversationRunnerTests
    {
        // Answers every prompt the instant it arrives, recording what it was asked to do. Choices
        // are answered by a queue the test fills in advance.
        private sealed class FakePresenter : IConversationPresenter
        {
            public readonly List<string> PlayedScripts = new();
            public readonly List<List<ConversationChoice>> ChoicesShown = new();
            public readonly Queue<int> Answers = new();
            public bool CancelNext;
            public int BeginCount, EndCount;
            public bool LastAllowedCancel;

            public void BeginConversation() => BeginCount++;

            public void PlayScript(DialogueScript script, Action onFinished)
            {
                PlayedScripts.Add(script.ScriptId);
                onFinished();
            }

            public void ShowChoices(IReadOnlyList<ConversationChoice> choices, bool allowCancel,
                Action<int> onChosen, Action onCancelled)
            {
                ChoicesShown.Add(new List<ConversationChoice>(choices));
                LastAllowedCancel = allowCancel;

                if (CancelNext) { CancelNext = false; onCancelled(); return; }
                onChosen(Answers.Count > 0 ? Answers.Dequeue() : 0);
            }

            public void EndConversation() => EndCount++;
        }

        private readonly List<DialogueScript> scripts = new();
        private readonly List<ConversationGraphData> graphs = new();

        [TearDown]
        public void TearDown()
        {
            foreach (var script in scripts) UnityEngine.Object.DestroyImmediate(script);
            foreach (var graph in graphs) UnityEngine.Object.DestroyImmediate(graph);
            scripts.Clear();
            graphs.Clear();
        }

        private DialogueScript Script(string id)
        {
            var script = DialogueScript.CreateRuntime(id, "NARRATOR", new[] { "line" });
            scripts.Add(script);
            return script;
        }

        private ConversationNode ScriptNode(string id, string next) => new()
        {
            nodeId = id, kind = ConversationNodeKind.Script, script = Script(id), nextNodeId = next
        };

        private static ConversationNode ChoiceNode(string id, bool allowCancel, params ConversationOption[] options) => new()
        {
            nodeId = id, kind = ConversationNodeKind.Choice, options = options, allowCancel = allowCancel
        };

        private static ConversationNode TopicNode(string id, params ConversationOption[] options) => new()
        {
            nodeId = id, kind = ConversationNodeKind.TopicMenu, options = options, allowCancel = true
        };

        private static ConversationOption Option(string id, string label, string next,
            bool askOnce = false, string repeatNode = null) => new()
        {
            optionId = id, label = label, nextNodeId = next, askOnce = askOnce, repeatNodeId = repeatNode
        };

        private (GurukulConversationRunner runner, FakePresenter presenter, GurukulRuntimeState state) Build(
            string entry, params ConversationNode[] nodes)
        {
            var graph = ConversationGraphData.CreateForTest("talk_kaal", entry, nodes);
            graphs.Add(graph);

            var state = new GurukulRuntimeState("hub1");
            var presenter = new FakePresenter();
            return (new GurukulConversationRunner(graph, presenter, state), presenter, state);
        }

        // Straight-line walks

        [Test]
        public void AChainOfScripts_PlaysInOrderThenEnds()
        {
            var (runner, presenter, _) = Build("a", ScriptNode("a", "b"), ScriptNode("b", null));

            runner.Begin();

            CollectionAssert.AreEqual(new[] { "a", "b" }, presenter.PlayedScripts);
            Assert.AreEqual(1, presenter.EndCount);
            Assert.IsFalse(runner.IsRunning);
        }

        // The box is opened once around the whole run and closed once at the end — that bracketing
        // is what stops it blinking between scripts at every choice point.
        [Test]
        public void TheConversationIsBracketedOnce_NotOncePerScript()
        {
            var (runner, presenter, _) = Build("a", ScriptNode("a", "b"), ScriptNode("b", null));

            runner.Begin();

            Assert.AreEqual(1, presenter.BeginCount);
            Assert.AreEqual(1, presenter.EndCount);
        }

        [Test]
        public void FinishingAConversation_RecordsIt()
        {
            var (runner, _, state) = Build("a", ScriptNode("a", null));

            runner.Begin();

            Assert.IsTrue(state.HasCompletedConversation("talk_kaal"));
        }

        [Test]
        public void AMissingNextNode_EndsCleanly()
        {
            var (runner, presenter, _) = Build("a", ScriptNode("a", "nowhere"));

            runner.Begin();

            Assert.AreEqual(1, presenter.EndCount);
            Assert.IsFalse(runner.IsRunning);
        }

        // Choices

        [Test]
        public void AChoice_FollowsThePickedBranch()
        {
            var (runner, presenter, _) = Build("ask",
                ChoiceNode("ask", true, Option("yes", "Yes", "accepted"), Option("no", "No", "declined")),
                ScriptNode("accepted", null), ScriptNode("declined", null));
            presenter.Answers.Enqueue(1);

            runner.Begin();

            CollectionAssert.AreEqual(new[] { "declined" }, presenter.PlayedScripts);
        }

        [Test]
        public void Cancelling_FollowsTheCancelBranch()
        {
            var cancellable = ChoiceNode("ask", true, Option("yes", "Yes", "accepted"));
            cancellable.cancelNodeId = "backedOut";

            var (runner, presenter, _) = Build("ask", cancellable,
                ScriptNode("accepted", null), ScriptNode("backedOut", null));
            presenter.CancelNext = true;

            runner.Begin();

            CollectionAssert.AreEqual(new[] { "backedOut" }, presenter.PlayedScripts);
        }

        // A knowledge check can't be backed out of, so the presenter is told not to offer it.
        [Test]
        public void AChoiceMarkedUncancellable_SaysSoWhenItIsShown()
        {
            var (runner, presenter, _) = Build("q",
                ChoiceNode("q", false, Option("a", "A", null)));

            runner.Begin();

            Assert.IsFalse(presenter.LastAllowedCancel);
        }

        // Quizzes are a graph shape, not a system: a wrong answer points back at the question.
        [Test]
        public void AWrongAnswer_ComesBackToTheSameQuestion()
        {
            var (runner, presenter, _) = Build("question",
                ScriptNode("question", "answers"),
                ChoiceNode("answers", false,
                    Option("right", "Correct", "passed"),
                    Option("wrong", "Wrong", "correction")),
                ScriptNode("correction", "question"),
                ScriptNode("passed", null));

            presenter.Answers.Enqueue(1);   // wrong first
            presenter.Answers.Enqueue(0);   // then right

            runner.Begin();

            CollectionAssert.AreEqual(new[] { "question", "correction", "question", "passed" },
                presenter.PlayedScripts);
            Assert.AreEqual(2, presenter.ChoicesShown.Count, "The question should be asked again after a miss.");
        }

        // Topic menus

        [Test]
        public void ATopicMenu_ReopensAfterEachTopic()
        {
            var (runner, presenter, _) = Build("menu",
                TopicNode("menu",
                    Option("asur", "Ask about Asur", "asurAnswer"),
                    Option("theory", "Ask about Weapon Theory", "theoryAnswer")),
                ScriptNode("asurAnswer", "menu"),
                ScriptNode("theoryAnswer", null));

            presenter.Answers.Enqueue(0);   // Asur, returns to the menu
            presenter.Answers.Enqueue(1);   // then Weapon Theory, which ends it

            runner.Begin();

            CollectionAssert.AreEqual(new[] { "asurAnswer", "theoryAnswer" }, presenter.PlayedScripts);
            Assert.AreEqual(2, presenter.ChoicesShown.Count);
        }

        [Test]
        public void AnAskedTopic_IsGreyedTheNextTimeTheMenuOpens()
        {
            var (runner, presenter, _) = Build("menu",
                TopicNode("menu",
                    Option("asur", "Ask about Asur", "asurAnswer", askOnce: true),
                    Option("theory", "Ask about Weapon Theory", "theoryAnswer")),
                ScriptNode("asurAnswer", "menu"),
                ScriptNode("theoryAnswer", null));

            presenter.Answers.Enqueue(0);
            presenter.Answers.Enqueue(1);

            runner.Begin();

            Assert.IsTrue(presenter.ChoicesShown[0][0].Enabled, "First time round it should be pickable.");
            Assert.IsFalse(presenter.ChoicesShown[1][0].Enabled, "Once asked, it should show as used.");
        }

        [Test]
        public void AskingATopicAgain_PlaysItsShorterAnswer()
        {
            var (runner, presenter, _) = Build("menu",
                TopicNode("menu",
                    Option("asur", "Ask about Asur", "asurAnswer", repeatNode: "asurAgain"),
                    Option("done", "Nothing", null)),
                ScriptNode("asurAnswer", "menu"),
                ScriptNode("asurAgain", "menu"),
                ScriptNode("done", null));

            presenter.Answers.Enqueue(0);   // first ask
            presenter.Answers.Enqueue(0);   // ask again
            presenter.Answers.Enqueue(1);   // leave

            runner.Begin();

            CollectionAssert.AreEqual(new[] { "asurAnswer", "asurAgain" }, presenter.PlayedScripts);
        }

        [Test]
        public void TopicsAreRememberedPerConversation_NotGlobally()
        {
            var (runner, presenter, state) = Build("menu",
                TopicNode("menu", Option("asur", "Ask about Asur", null, askOnce: true)));

            runner.Begin();

            Assert.IsTrue(state.HasAskedTopic("talk_kaal", "asur"));
            Assert.IsFalse(state.HasAskedTopic("talk_madhavi", "asur"),
                "Another character's topic of the same name is a different question.");
        }

        // Flags and repeat entries

        [Test]
        public void ASetFlagNode_RaisesItsFlagAndCarriesOn()
        {
            var raised = new List<string>();
            var flagNode = new ConversationNode
            {
                nodeId = "flag", kind = ConversationNodeKind.SetFlag, flagId = "quiz_passed", nextNodeId = "after"
            };

            var (runner, presenter, _) = Build("flag", flagNode, ScriptNode("after", null));
            runner.FlagRaised += raised.Add;

            runner.Begin();

            CollectionAssert.AreEqual(new[] { "quiz_passed" }, raised);
            CollectionAssert.AreEqual(new[] { "after" }, presenter.PlayedScripts);
        }

        [Test]
        public void ASecondVisit_StartsFromTheRepeatEntryWhenThereIsOne()
        {
            var graph = ConversationGraphData.CreateForTest("talk_kaal", "first",
                new[] { ScriptNode("first", null), ScriptNode("again", null) }, repeatEntryNodeId: "again");
            graphs.Add(graph);

            var state = new GurukulRuntimeState("hub1");
            var presenter = new FakePresenter();

            new GurukulConversationRunner(graph, presenter, state).Begin();
            new GurukulConversationRunner(graph, presenter, state).Begin();

            CollectionAssert.AreEqual(new[] { "first", "again" }, presenter.PlayedScripts);
        }

        [Test]
        public void WithNoRepeatEntry_ASecondVisitReplaysTheFirstTimeContent()
        {
            var graph = ConversationGraphData.CreateForTest("talk_kaal", "first",
                new[] { ScriptNode("first", null) });
            graphs.Add(graph);

            var state = new GurukulRuntimeState("hub1");
            var presenter = new FakePresenter();

            new GurukulConversationRunner(graph, presenter, state).Begin();
            new GurukulConversationRunner(graph, presenter, state).Begin();

            CollectionAssert.AreEqual(new[] { "first", "first" }, presenter.PlayedScripts);
        }
    }
}
