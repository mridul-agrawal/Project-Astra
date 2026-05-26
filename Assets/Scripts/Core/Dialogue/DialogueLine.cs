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
        [SpeakerId, SerializeField] private string _speakerId;
        [SerializeField] private DialogueExpression _expression = DialogueExpression.Neutral;
        [SerializeField] private PortraitPosition _portraitPosition = PortraitPosition.Left;

        [Tooltip("Which way the portrait looks. Art faces Left by default; Right flips it horizontally.")]
        [SerializeField] private PortraitFacing _portraitFacing = PortraitFacing.Left;

        [SerializeField, TextArea(2, 5)] private string _text;

        public string SpeakerId => _speakerId;
        public DialogueExpression Expression => _expression;
        public PortraitPosition PortraitPosition => _portraitPosition;
        public PortraitFacing PortraitFacing => _portraitFacing;
        public string Text => _text;

        internal static DialogueLine CreateForTest(string speakerId, string text,
            DialogueExpression expression = DialogueExpression.Neutral,
            PortraitPosition position = PortraitPosition.Left,
            PortraitFacing facing = PortraitFacing.Left)
        {
            return new DialogueLine
            {
                _speakerId = speakerId,
                _text = text,
                _expression = expression,
                _portraitPosition = position,
                _portraitFacing = facing
            };
        }
    }
}
