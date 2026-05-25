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
        [SerializeField] private int _nodeId;
        [SerializeField] private string _speakerId;
        [SerializeField] private DialogueExpression _expression = DialogueExpression.Neutral;
        [SerializeField] private PortraitPosition _portraitPosition = PortraitPosition.Left;
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
        public string Text => _text;
        public Sprite FullScreenImage => _fullScreenImage;

        public bool HasTextSpeedOverride => _textSpeedOverride > 0f;
        public float TextSpeedOverride => _textSpeedOverride;

        public bool AutoAdvances => _autoAdvanceDelay > 0f;
        public float AutoAdvanceDelay => _autoAdvanceDelay;

        // For Testing Only! This is a bit of a code smell but it's just to avoid copy-pasting the same boilerplate in a few dozen tests.
        internal static DialogueNode CreateForTest(int nodeId, string speakerId, string text,
            DialogueExpression expression = DialogueExpression.Neutral,
            PortraitPosition position = PortraitPosition.Left,
            float textSpeedOverride = -1f, float autoAdvanceDelay = 0f)
        {
            return new DialogueNode
            {
                _nodeId = nodeId,
                _speakerId = speakerId,
                _text = text,
                _expression = expression,
                _portraitPosition = position,
                _textSpeedOverride = textSpeedOverride,
                _autoAdvanceDelay = autoAdvanceDelay
            };
        }
    }
}
