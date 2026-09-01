using System;
using System.Collections.Generic;
using UnityEngine;

namespace ProjectAstra.Core.Dialogue
{
    // Walks one script's nodes and drives a view: reveals each line letter by letter, waits
    // for the player, puts a choice on screen when it reaches one, follows the branch, and
    // raises OnComplete at the end. Pure C# and time is fed in through Tick, so it unit-tests
    // without a Canvas — same shape as DelayedAutoShift.
    //
    // It owns which option is highlighted for the same reason it owns the crawl: the view
    // renders what it is told and decides nothing.
    internal class DialogueRunner
    {
        private const float MinCharsPerSecond = 1f;

        // Jumps and signals cost no frames, so a graph that loops would spin forever. The
        // budget gives up loudly instead.
        private const int MaxFlowStepsPerAdvance = 256;

        private readonly DialogueScript script;
        private readonly DialogueSpeakerRegistry registry;
        private readonly IDialogueView view;
        private readonly DialogueTriggeringContext context;
        private readonly IDialogueMemory memory;
        private readonly float defaultCharsPerSecond;

        private readonly List<DialogueChoiceView> choices = new();

        private int index;
        private DialogueNode node;
        private string text = string.Empty;
        private float charsPerSecond;
        private float revealed;
        private float autoAdvanceElapsed;
        private bool crawlComplete;
        private int lastShown;

        private bool awaitingChoice;
        private int highlighted = -1;

        public event Action OnComplete;

        // Raised for each Signal node. Whoever asked for the script decides what the id means.
        public event Action<string> SignalRaised;

        public bool IsRunning { get; private set; }
        public bool AwaitingChoice => awaitingChoice;
        public int HighlightedOption => highlighted;

        public DialogueRunner(DialogueScript script, DialogueSpeakerRegistry registry,
            IDialogueView view, DialogueTriggeringContext context, float defaultCharsPerSecond,
            IDialogueMemory memory = null)
        {
            this.script = script;
            this.registry = registry;
            this.view = view;
            this.context = context;
            this.memory = memory;
            this.defaultCharsPerSecond = Mathf.Max(MinCharsPerSecond, defaultCharsPerSecond);
        }

        public void Start()
        {
            IsRunning = true;
            index = ResolveEntryIndex();
            view.Show(context);
            RunFlow(skipLines: false);
        }

        // A script that has been played before starts at its repeat label, which is how one
        // asset holds both the first-time exchange and the short line afterwards.
        private int ResolveEntryIndex()
        {
            if (memory == null || !memory.HasPlayed(script.ScriptId)) return 0;

            int repeat = script.IndexOfLabel(script.RepeatEntryLabel);
            return repeat >= 0 ? repeat : 0;
        }

        // Called every frame by the service with Time.deltaTime.
        public void Tick(float deltaTime)
        {
            if (!IsRunning || awaitingChoice) return;
            if (!crawlComplete) AdvanceCrawl(deltaTime);
            else if (node.AutoAdvances) AdvanceAutoTimer(deltaTime);
        }

        // Confirm picks the highlighted option, or snaps a crawling line to full first and
        // advances on the next press.
        public void Confirm()
        {
            if (!IsRunning) return;

            if (awaitingChoice) ConfirmChoice();
            else if (!crawlComplete) CompleteCrawl();
            else Advance();
        }

        // Steps the highlight, wrapping and stepping over anything already taken so the cursor
        // never rests on a topic she has already asked.
        public void MoveSelection(int step)
        {
            if (!IsRunning || !awaitingChoice || step == 0 || choices.Count == 0) return;

            int candidate = highlighted;
            for (int i = 0; i < choices.Count; i++)
            {
                candidate = (candidate + step + choices.Count) % choices.Count;
                if (!choices[candidate].Enabled) continue;

                highlighted = candidate;
                view.ShowChoices(choices, highlighted);
                return;
            }
        }

        // A knowledge check can't be backed out of. When cancelling isn't allowed the menu
        // simply stays put rather than closing and leaving the script nowhere to go.
        public void Cancel()
        {
            if (!IsRunning || !awaitingChoice || !node.AllowCancel) return;

            string target = node.CancelTargetLabel;
            EndChoice();

            if (string.IsNullOrEmpty(target)) { Finish(); return; }
            if (GoTo(target)) RunFlow(skipLines: false);
        }

        // Abandons the remaining lines, but still raises the signals on the way so a skipped
        // effect lands exactly once, and stops at a choice because that still has to be answered.
        public void Skip()
        {
            if (!IsRunning || awaitingChoice) return;

            index++;
            RunFlow(skipLines: true);
        }


        // Flow:
        // One loop rather than recursion, because jumps and signals resolve without a frame
        // and a deep chain would otherwise grow the stack.
        private void RunFlow(bool skipLines)
        {
            for (int steps = 0; steps < MaxFlowStepsPerAdvance; steps++)
            {
                if (index < 0 || index >= script.Nodes.Count) { Finish(); return; }
                node = script.Nodes[index];

                switch (node.Kind)
                {
                    case DialogueNodeKind.Line:
                        if (skipLines) { index++; continue; }
                        if (TryBeginLine()) return;
                        index++;
                        continue;

                    case DialogueNodeKind.Choice:
                        if (BeginChoice()) return;
                        index++;
                        continue;

                    case DialogueNodeKind.Jump:
                        // A jump with nowhere to go is how a script ends early, which is what a
                        // branch that finishes mid-list needs.
                        if (string.IsNullOrEmpty(node.TargetLabel)) { Finish(); return; }
                        if (!GoTo(node.TargetLabel)) return;
                        continue;

                    case DialogueNodeKind.Signal:
                        SignalRaised?.Invoke(node.SignalId);
                        index++;
                        continue;

                    default:
                        index++;
                        continue;
                }
            }

            Debug.LogError($"[DialogueRunner] Script '{script.ScriptId}' never reached a line after " +
                           $"{MaxFlowStepsPerAdvance} steps — its jumps loop. Ending it.");
            Finish();
        }

        // Returns false (and ends the script) when the label nothing carries, rather than
        // silently running on into whatever happens to be next.
        private bool GoTo(string label)
        {
            int target = script.IndexOfLabel(label);
            if (target >= 0) { index = target; return true; }

            Debug.LogError($"[DialogueRunner] Script '{script.ScriptId}' jumps to '{label}', " +
                           "which no node is labelled. Ending it.");
            Finish();
            return false;
        }


        // Lines:
        private bool TryBeginLine()
        {
            if (TryBuildLine(node, out DialogueLineView line))
            {
                BeginCrawl(node, line);
                return true;
            }

            // A node pointing at a speaker that doesn't exist is skipped, not fatal.
            Debug.LogWarning($"[DialogueRunner] Script '{script.ScriptId}' node {node.NodeId}: " +
                             $"speaker '{node.SpeakerId}' not found. Skipping line.");
            return false;
        }

        private void BeginCrawl(DialogueNode node, in DialogueLineView line)
        {
            view.ShowLine(line);
            text = node.Text ?? string.Empty;
            charsPerSecond = node.HasTextSpeedOverride ? node.TextSpeedOverride : defaultCharsPerSecond;
            revealed = 0f;
            lastShown = 0;
            autoAdvanceElapsed = 0f;
            crawlComplete = false;
            view.SetVisibleCharacters(0);
            view.SetContinueHintVisible(false);

            if (text.Length == 0) CompleteCrawl();
        }

        private void AdvanceCrawl(float deltaTime)
        {
            revealed += charsPerSecond * deltaTime;
            int target = Mathf.Min((int)revealed, text.Length);

            // Reveal one letter at a time so the cadence is even and each letter blips.
            while (lastShown < target)
            {
                lastShown++;
                view.SetVisibleCharacters(lastShown);
            }

            if (lastShown >= text.Length) CompleteCrawl();
        }

        private void CompleteCrawl()
        {
            crawlComplete = true;
            lastShown = text.Length;
            view.SetVisibleCharacters(text.Length);
            // A line that advances on its own shouldn't beg for a button press.
            view.SetContinueHintVisible(!node.AutoAdvances);
        }

        private void AdvanceAutoTimer(float deltaTime)
        {
            autoAdvanceElapsed += deltaTime;
            if (autoAdvanceElapsed >= node.AutoAdvanceDelay) Advance();
        }

        private void Advance()
        {
            index++;
            RunFlow(skipLines: false);
        }


        // Choices:
        // Returns false when nothing on the menu can be picked, which falls through to the
        // next node rather than trapping the player in a dead menu.
        private bool BeginChoice()
        {
            BuildChoices();
            highlighted = FirstEnabled();

            if (highlighted < 0)
            {
                Debug.LogWarning($"[DialogueRunner] Script '{script.ScriptId}' node {node.NodeId}: " +
                                 "no option can be picked. Skipping the choice.");
                return false;
            }

            awaitingChoice = true;
            crawlComplete = true;
            view.SetContinueHintVisible(false);
            view.ShowChoices(choices, highlighted);
            return true;
        }

        // An option already taken is greyed rather than removed, so the menu keeps its shape
        // between visits.
        private void BuildChoices()
        {
            choices.Clear();
            IReadOnlyList<DialogueOption> options = node.Options;
            if (options == null) return;

            foreach (DialogueOption option in options)
            {
                if (option == null) continue;
                choices.Add(new DialogueChoiceView(option.Label, !IsUsedUp(option)));
            }
        }

        private bool IsUsedUp(DialogueOption option) =>
            option.AskOnce && HasBeenChosen(option);

        private bool HasBeenChosen(DialogueOption option) =>
            memory != null && memory.HasChosen(script.ScriptId, option.OptionId);

        private int FirstEnabled()
        {
            for (int i = 0; i < choices.Count; i++)
                if (choices[i].Enabled) return i;
            return -1;
        }

        private void ConfirmChoice()
        {
            if (highlighted < 0 || highlighted >= choices.Count) return;
            if (!choices[highlighted].Enabled) return;

            DialogueOption picked = node.Options[highlighted];

            // Read before marking, or the very first pick would already count as a repeat.
            bool askedBefore = HasBeenChosen(picked);
            memory?.MarkChosen(script.ScriptId, picked.OptionId);

            string target = askedBefore && !string.IsNullOrEmpty(picked.RepeatTargetLabel)
                ? picked.RepeatTargetLabel
                : picked.TargetLabel;

            EndChoice();

            if (string.IsNullOrEmpty(target)) { Finish(); return; }
            if (GoTo(target)) RunFlow(skipLines: false);
        }

        private void EndChoice()
        {
            awaitingChoice = false;
            highlighted = -1;
            choices.Clear();
            view.HideChoices();
        }


        private bool TryBuildLine(DialogueNode node, out DialogueLineView line)
        {
            line = default;
            bool hidden = node.PortraitPosition == PortraitPosition.None;

            if (DialogueSpeakerRegistry.IsNarrator(node.SpeakerId))
            {
                line = new DialogueLineView(null, node.PortraitPosition, node.PortraitFacing, string.Empty, node.Text, node.FullScreenImage);
                return true;
            }

            if (registry == null || !registry.TryResolve(node.SpeakerId, out var speaker))
                return false;

            var portrait = hidden ? null : speaker.ResolvePortrait(node.Expression);
            line = new DialogueLineView(portrait, node.PortraitPosition, node.PortraitFacing, speaker.DisplayName, node.Text, node.FullScreenImage);
            return true;
        }

        private void Finish()
        {
            if (!IsRunning) return;

            IsRunning = false;
            awaitingChoice = false;
            highlighted = -1;
            choices.Clear();
            memory?.MarkPlayed(script.ScriptId);
            view.HideChoices();
            view.Hide();
            OnComplete?.Invoke();
        }
    }
}
