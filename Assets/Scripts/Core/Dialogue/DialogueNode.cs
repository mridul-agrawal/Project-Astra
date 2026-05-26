using System;
using UnityEngine;

namespace ProjectAstra.Core.Dialogue
{
    // One displayed line in a script. The prototype runs nodes in list order;
    // NodeId is a stable identifier (it'll key the localisation lookup and the
    // branch/choice jumps that arrive later — neither is built yet).
    [Serializable]
    public class DialogueNode
    {
        [HideInInspector, SerializeField] private int _nodeId;
        [SpeakerId, SerializeField] private string _speakerId;
        [SerializeField] private DialogueExpression _expression = DialogueExpression.Neutral;
        [SerializeField] private PortraitPosition _portraitPosition = PortraitPosition.Left;

        [Tooltip("Which way the portrait looks. Art faces Left by default; Right flips it horizontally (scale.x × −1).")]
        [SerializeField] private PortraitFacing _portraitFacing = PortraitFacing.Left;

        [SerializeField, TextArea(2, 5)] private string _text;

        [Tooltip("Characters per second for this line. Leave below 0 to use the global text speed.")]
        [SerializeField] private float _textSpeedOverride = -1f;

        [Tooltip("Seconds to wait, then advance on its own. Leave at 0 to wait for the player.")]
        [SerializeField] private float _autoAdvanceDelay = 0f;

        [Tooltip("Optional full-screen still shown behind this line (high-intensity 'bespoke still' moments).")]
        [SerializeField] private Sprite _fullScreenImage;

        public int NodeId => _nodeId;
        public string SpeakerId => _speakerId;
        public DialogueExpression Expression => _expression;
        public PortraitPosition PortraitPosition => _portraitPosition;
        public PortraitFacing PortraitFacing => _portraitFacing;
        public string Text => _text;
        public Sprite FullScreenImage => _fullScreenImage;

        public bool HasTextSpeedOverride => _textSpeedOverride > 0f;
        public float TextSpeedOverride => _textSpeedOverride;

        public bool AutoAdvances => _autoAdvanceDelay > 0f;
        public float AutoAdvanceDelay => _autoAdvanceDelay;

        // Kept in sync with list position by DialogueScript.OnValidate — never hand-set.
        internal void SetNodeId(int id) => _nodeId = id;

        // Builds the runtime node the runner consumes by flattening a segment + line:
        // the line supplies speaker/expression/portrait/text, the segment supplies the
        // shared background, crawl speed, and auto-advance.
        internal static DialogueNode CreateRuntime(int nodeId, DialogueLine line, DialogueSegment segment)
        {
            return new DialogueNode
            {
                _nodeId = nodeId,
                _speakerId = line.SpeakerId,
                _expression = line.Expression,
                _portraitPosition = line.PortraitPosition,
                _portraitFacing = line.PortraitFacing,
                _text = line.Text,
                _fullScreenImage = segment.Background,
                _textSpeedOverride = segment.TextSpeed,
                _autoAdvanceDelay = segment.AutoAdvanceDelay
            };
        }

        // For Testing Only! This is a bit of a code smell but it's just to avoid copy-pasting the same boilerplate in a few dozen tests.
        internal static DialogueNode CreateForTest(int nodeId, string speakerId, string text,
            DialogueExpression expression = DialogueExpression.Neutral,
            PortraitPosition position = PortraitPosition.Left,
            float textSpeedOverride = -1f, float autoAdvanceDelay = 0f,
            PortraitFacing facing = PortraitFacing.Left)
        {
            return new DialogueNode
            {
                _nodeId = nodeId,
                _speakerId = speakerId,
                _text = text,
                _expression = expression,
                _portraitPosition = position,
                _portraitFacing = facing,
                _textSpeedOverride = textSpeedOverride,
                _autoAdvanceDelay = autoAdvanceDelay
            };
        }
    }
}
