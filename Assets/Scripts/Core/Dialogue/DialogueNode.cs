using System;
using System.Collections.Generic;
using UnityEngine;

namespace ProjectAstra.Core.Dialogue
{
    // One step of a script as the runner consumes it: an authored line with its segment's
    // background, speed and auto-advance already folded in. Most are spoken lines; a Choice,
    // a Jump or a Signal carries its own payload instead and leaves the rest blank.
    //
    // The runner walks these in list order until a Choice or a Jump sends it to a label.
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

        [SerializeField] private DialogueNodeKind kind = DialogueNodeKind.Line;
        [SerializeField] private string label;
        [SerializeField] private List<DialogueOption> options = new();
        [SerializeField] private bool allowCancel;
        [SerializeField] private string cancelTargetLabel;
        [SerializeField] private string targetLabel;
        [SerializeField] private string signalId;

        public DialogueNodeKind Kind => kind;
        public string Label => label;
        public IReadOnlyList<DialogueOption> Options => options;
        public bool AllowCancel => allowCancel;
        public string CancelTargetLabel => cancelTargetLabel;
        public string TargetLabel => targetLabel;
        public string SignalId => signalId;

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
                autoAdvanceDelay = segment.AutoAdvanceDelay,
                kind = line.Kind,
                label = line.Label,
                options = new List<DialogueOption>(line.Options ?? (IReadOnlyList<DialogueOption>)Array.Empty<DialogueOption>()),
                allowCancel = line.AllowCancel,
                cancelTargetLabel = line.CancelTargetLabel,
                targetLabel = line.TargetLabel,
                signalId = line.SignalId
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

        internal static DialogueNode CreateLabelledLineForTest(int nodeId, string speakerId,
            string text, string label)
        {
            DialogueNode node = CreateForTest(nodeId, speakerId, text);
            node.label = label;
            return node;
        }

        internal static DialogueNode CreateChoiceForTest(int nodeId, string label,
            bool allowCancel = false, string cancelTargetLabel = null, params DialogueOption[] options)
        {
            return new DialogueNode
            {
                nodeId = nodeId,
                kind = DialogueNodeKind.Choice,
                label = label,
                allowCancel = allowCancel,
                cancelTargetLabel = cancelTargetLabel,
                options = new List<DialogueOption>(options)
            };
        }

        internal static DialogueNode CreateJumpForTest(int nodeId, string label, string targetLabel) =>
            new() { nodeId = nodeId, kind = DialogueNodeKind.Jump, label = label, targetLabel = targetLabel };

        internal static DialogueNode CreateSignalForTest(int nodeId, string label, string signalId) =>
            new() { nodeId = nodeId, kind = DialogueNodeKind.Signal, label = label, signalId = signalId };
    }
}
