using System;
using UnityEngine;

namespace ProjectAstra.Core.Dialogue
{
    // Walks one script's nodes in order and drives a view: reveals each line
    // letter by letter, waits for the player (or an auto-advance timer), and
    // raises OnComplete at the end. Pure C# and time is fed in through Tick, so
    // it unit-tests without a Canvas — same shape as DelayedAutoShift.
    internal class DialogueRunner
    {
        private const float MinCharsPerSecond = 1f;

        private readonly DialogueScript script;
        private readonly DialogueSpeakerRegistry registry;
        private readonly IDialogueView view;
        private readonly DialogueTriggeringContext context;
        private readonly float defaultCharsPerSecond;

        private int index;
        private DialogueNode node;
        private string text = string.Empty;
        private float charsPerSecond;
        private float revealed;
        private float autoAdvanceElapsed;
        private bool crawlComplete;
        private int lastShown;

        public event Action OnComplete;
        public bool IsRunning { get; private set; }

        public DialogueRunner(DialogueScript script, DialogueSpeakerRegistry registry,
            IDialogueView view, DialogueTriggeringContext context, float defaultCharsPerSecond)
        {
            this.script = script;
            this.registry = registry;
            this.view = view;
            this.context = context;
            this.defaultCharsPerSecond = Mathf.Max(MinCharsPerSecond, defaultCharsPerSecond);
        }

        public void Start()
        {
            IsRunning = true;
            index = 0;
            view.Show(context);
            PresentCurrentNode();
        }

        // Called every frame by the service with Time.deltaTime.
        public void Tick(float deltaTime)
        {
            if (!IsRunning) return;
            if (!crawlComplete) AdvanceCrawl(deltaTime);
            else if (node.AutoAdvances) AdvanceAutoTimer(deltaTime);
        }

        // Confirm snaps a crawling line to full first, then advances on the next press.
        public void Confirm()
        {
            if (!IsRunning) return;
            if (!crawlComplete) CompleteCrawl();
            else Advance();
        }

        // Skip abandons the rest of the script and ends immediately.
        public void Skip()
        {
            if (IsRunning) Finish();
        }

        private void PresentCurrentNode()
        {
            if (index >= script.Nodes.Count) { Finish(); return; }

            node = script.Nodes[index];
            if (!TryBuildLine(node, out var line)) { SkipMissingSpeaker(); return; }

            BeginCrawl(node, line);
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
            PresentCurrentNode();
        }

        // A node pointing at a unit/speaker that doesn't exist is skipped, not fatal —
        // authors guard existence with branch nodes once those ship.
        private void SkipMissingSpeaker()
        {
            Debug.LogWarning($"[DialogueRunner] Script '{script.ScriptId}' node {node.NodeId}: " +
                             $"speaker '{node.SpeakerId}' not found. Skipping line.");
            Advance();
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
            IsRunning = false;
            view.Hide();
            OnComplete?.Invoke();
        }
    }
}
