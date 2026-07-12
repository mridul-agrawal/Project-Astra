using System;
using UnityEngine;

namespace ProjectAstra.Core.Dialogue
{
    // One spoken line inside a segment. Carries only what changes line-to-line:
    // who speaks, their expression, where their portrait sits and which way it
    // faces, and the text. Background, crawl speed, and auto-advance come from the
    // parent segment; the portrait sprite + name are resolved from the speaker DB.
    [Serializable]
    public class DialogueLine
    {
        [SpeakerId, SerializeField] private string speakerId;
        [SerializeField] private DialogueExpression expression = DialogueExpression.Neutral;
        [SerializeField] private PortraitPosition portraitPosition = PortraitPosition.Left;

        [Tooltip("Which way the portrait looks. Art faces Left by default; Right flips it horizontally.")]
        [SerializeField] private PortraitFacing portraitFacing = PortraitFacing.Left;

        [SerializeField, TextArea(2, 5)] private string text;

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
    }
}
