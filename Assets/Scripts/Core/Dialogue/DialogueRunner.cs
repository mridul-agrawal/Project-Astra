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

        private readonly DialogueScript _script;
        private readonly DialogueSpeakerRegistry _registry;
        private readonly IDialogueView _view;
        private readonly DialogueContext _context;
        private readonly float _defaultCharsPerSecond;

        private int _index;
        private DialogueNode _node;
        private string _text = string.Empty;
        private float _charsPerSecond;
        private float _revealed;
        private float _autoAdvanceElapsed;
        private bool _crawlComplete;

        public event Action OnComplete;
        public bool IsRunning { get; private set; }

        public DialogueRunner(DialogueScript script, DialogueSpeakerRegistry registry,
            IDialogueView view, DialogueContext context, float defaultCharsPerSecond)
        {
            _script = script;
            _registry = registry;
            _view = view;
            _context = context;
            _defaultCharsPerSecond = Mathf.Max(MinCharsPerSecond, defaultCharsPerSecond);
        }

        public void Start()
        {
            IsRunning = true;
            _index = 0;
            _view.Show(_context);
            PresentCurrentNode();
        }

        // Called every frame by the service with Time.deltaTime.
        public void Tick(float deltaTime)
        {
            if (!IsRunning) return;
            if (!_crawlComplete) AdvanceCrawl(deltaTime);
            else if (_node.AutoAdvances) AdvanceAutoTimer(deltaTime);
        }

        // Confirm snaps a crawling line to full first, then advances on the next press.
        public void Confirm()
        {
            if (!IsRunning) return;
            if (!_crawlComplete) CompleteCrawl();
            else Advance();
        }

        // Skip abandons the rest of the script and ends immediately.
        public void Skip()
        {
            if (IsRunning) Finish();
        }

        private void PresentCurrentNode()
        {
            if (_index >= _script.Nodes.Count) { Finish(); return; }

            _node = _script.Nodes[_index];
            if (!TryBuildLine(_node, out var line)) { SkipMissingSpeaker(); return; }

            BeginCrawl(_node, line);
        }

        private void BeginCrawl(DialogueNode node, in DialogueLineView line)
        {
            _view.ShowLine(line);
            _text = node.Text ?? string.Empty;
            _charsPerSecond = node.HasTextSpeedOverride ? node.TextSpeedOverride : _defaultCharsPerSecond;
            _revealed = 0f;
            _autoAdvanceElapsed = 0f;
            _crawlComplete = false;
            _view.SetVisibleCharacters(0);
            _view.SetContinueHintVisible(false);

            if (_text.Length == 0) CompleteCrawl();
        }

        private void AdvanceCrawl(float deltaTime)
        {
            _revealed += _charsPerSecond * deltaTime;
            int shown = Mathf.Min((int)_revealed, _text.Length);
            _view.SetVisibleCharacters(shown);
            if (shown >= _text.Length) CompleteCrawl();
        }

        private void CompleteCrawl()
        {
            _crawlComplete = true;
            _view.SetVisibleCharacters(_text.Length);
            // A line that advances on its own shouldn't beg for a button press.
            _view.SetContinueHintVisible(!_node.AutoAdvances);
        }

        private void AdvanceAutoTimer(float deltaTime)
        {
            _autoAdvanceElapsed += deltaTime;
            if (_autoAdvanceElapsed >= _node.AutoAdvanceDelay) Advance();
        }

        private void Advance()
        {
            _index++;
            PresentCurrentNode();
        }

        // A node pointing at a unit/speaker that doesn't exist is skipped, not fatal —
        // authors guard existence with branch nodes once those ship.
        private void SkipMissingSpeaker()
        {
            Debug.LogWarning($"[DialogueRunner] Script '{_script.ScriptId}' node {_node.NodeId}: " +
                             $"speaker '{_node.SpeakerId}' not found. Skipping line.");
            Advance();
        }

        private bool TryBuildLine(DialogueNode node, out DialogueLineView line)
        {
            line = default;
            bool hidden = node.PortraitPosition == PortraitPosition.None;

            if (DialogueSpeakerRegistry.IsNarrator(node.SpeakerId))
            {
                line = new DialogueLineView(null, node.PortraitPosition, string.Empty, node.Text, node.FullScreenImage);
                return true;
            }

            if (_registry == null || !_registry.TryResolve(node.SpeakerId, out var speaker))
                return false;

            var portrait = hidden ? null : speaker.ResolvePortrait(node.Expression);
            line = new DialogueLineView(portrait, node.PortraitPosition, speaker.DisplayName, node.Text, node.FullScreenImage);
            return true;
        }

        private void Finish()
        {
            IsRunning = false;
            _view.Hide();
            OnComplete?.Invoke();
        }
    }
}
