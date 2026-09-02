using System;
using System.Collections.Generic;
using UnityEngine;

namespace ProjectAstra.Core.Dialogue
{
    // One entry inside a segment. Usually a spoken line, carrying only what changes
    // line-to-line: who speaks, their expression, where their portrait sits and which way
    // it faces, and the text. Background, crawl speed, and auto-advance come from the
    // parent segment; the portrait sprite + name are resolved from the speaker DB.
    //
    // It can also be a Choice, a Jump or a Signal, in which case most of the fields below
    // are blank and the ones under that kind's heading are the ones that matter. One class
    // per kind is how HubEventAction already handles the same problem, and it is what
    // Unity can serialize without a polymorphic list.
    [Serializable]
    public class DialogueLine
    {
        [SpeakerId, SerializeField] private string speakerId;
        [SerializeField] private DialogueExpression expression = DialogueExpression.Neutral;
        [SerializeField] private PortraitPosition portraitPosition = PortraitPosition.Left;

        [Tooltip("Which way the portrait looks. Art faces Left by default; Right flips it horizontally.")]
        [SerializeField] private PortraitFacing portraitFacing = PortraitFacing.Left;

        [SerializeField, TextArea(2, 5)] private string text;

        [Header("Flow")]
        [Tooltip("What this entry does. Line speaks; the rest branch, jump or announce.")]
        [SerializeField] private DialogueNodeKind kind = DialogueNodeKind.Line;

        [Tooltip("Optional name for this entry so a Choice or a Jump can target it.")]
        [SerializeField] private string label;

        [Tooltip("Choice only: the options to offer, in authored order.")]
        [SerializeField] private List<DialogueOption> options = new();

        [Tooltip("Choice only: may CANCEL back out? A required answer must not be bypassed.")]
        [SerializeField] private bool allowCancel;

        [Tooltip("Choice only: where CANCEL goes. Blank ends the script.")]
        [SerializeField] private string cancelTargetLabel;

        [Tooltip("Jump only: the label to continue at.")]
        [SerializeField] private string targetLabel;

        [Tooltip("Signal only: the id to announce.")]
        [SerializeField] private string signalId;

        public DialogueNodeKind Kind => kind;
        public string Label => label;
        public IReadOnlyList<DialogueOption> Options => options;
        public bool AllowCancel => allowCancel;
        public string CancelTargetLabel => cancelTargetLabel;
        public string TargetLabel => targetLabel;
        public string SignalId => signalId;

        public string SpeakerId => speakerId;
        public DialogueExpression Expression => expression;
        public PortraitPosition PortraitPosition => portraitPosition;
        public PortraitFacing PortraitFacing => portraitFacing;
        public string Text => text;

        internal static DialogueLine Create(string speakerId, string text,
            DialogueExpression expression = DialogueExpression.Neutral,
            PortraitPosition position = PortraitPosition.Left,
            PortraitFacing facing = PortraitFacing.Left)
        {
            return new DialogueLine
            {
                speakerId = speakerId,
                text = text,
                expression = expression,
                portraitPosition = position,
                portraitFacing = facing
            };
        }

        internal static DialogueLine CreateChoice(string label, bool allowCancel,
            string cancelTargetLabel, params DialogueOption[] options)
        {
            return new DialogueLine
            {
                kind = DialogueNodeKind.Choice,
                label = label,
                allowCancel = allowCancel,
                cancelTargetLabel = cancelTargetLabel,
                options = new List<DialogueOption>(options)
            };
        }

        internal static DialogueLine CreateJump(string label, string targetLabel) =>
            new() { kind = DialogueNodeKind.Jump, label = label, targetLabel = targetLabel };

        internal static DialogueLine CreateSignal(string label, string signalId) =>
            new() { kind = DialogueNodeKind.Signal, label = label, signalId = signalId };
    }
}
