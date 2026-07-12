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
        [HideInInspector, SerializeField] private int nodeId;
        [SpeakerId, SerializeField] private string speakerId;
        [SerializeField] private DialogueExpression expression = DialogueExpression.Neutral;
        [SerializeField] private PortraitPosition portraitPosition = PortraitPosition.Left;

        [Tooltip("Which way the portrait looks. Art faces Left by default; Right flips it horizontally (scale.x × −1).")]
        [SerializeField] private PortraitFacing portraitFacing = PortraitFacing.Left;

        [SerializeField, TextArea(2, 5)] private string text;

        [Tooltip("Characters per second for this line. Leave below 0 to use the global text speed.")]
        [SerializeField] private float textSpeedOverride = -1f;

        [Tooltip("Seconds to wait, then advance on its own. Leave at 0 to wait for the player.")]
        [SerializeField] private float autoAdvanceDelay = 0f;

        [Tooltip("Optional full-screen still shown behind this line (high-intensity 'bespoke still' moments).")]
        [SerializeField] private Sprite fullScreenImage;

        public int NodeId => nodeId;
        public string SpeakerId => speakerId;
        public DialogueExpression Expression => expression;
        public PortraitPosition PortraitPosition => portraitPosition;
        public PortraitFacing PortraitFacing => portraitFacing;
        public string Text => text;
        public Sprite FullScreenImage => fullScreenImage;

        public bool HasTextSpeedOverride => textSpeedOverride > 0f;
        public float TextSpeedOverride => textSpeedOverride;

        public bool AutoAdvances => autoAdvanceDelay > 0f;
        public float AutoAdvanceDelay => autoAdvanceDelay;

        // Kept in sync with list position by DialogueScript.OnValidate — never hand-set.
        internal void SetNodeId(int id) => nodeId = id;

        // Builds the runtime node the runner consumes by flattening a segment + line:
        // the line supplies speaker/expression/portrait/text, the segment supplies the
        // shared background, crawl speed, and auto-advance.
        internal static DialogueNode CreateRuntime(int nodeId, DialogueLine line, DialogueSegment segment)
        {
            return new DialogueNode
            {
                nodeId = nodeId,
                speakerId = line.SpeakerId,
                expression = line.Expression,
                portraitPosition = line.PortraitPosition,
                portraitFacing = line.PortraitFacing,
                text = line.Text,
                fullScreenImage = segment.Background,
                textSpeedOverride = segment.TextSpeed,
                autoAdvanceDelay = segment.AutoAdvanceDelay
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
                nodeId = nodeId,
                speakerId = speakerId,
                text = text,
                expression = expression,
                portraitPosition = position,
                portraitFacing = facing,
                textSpeedOverride = textSpeedOverride,
                autoAdvanceDelay = autoAdvanceDelay
            };
        }
    }
}
