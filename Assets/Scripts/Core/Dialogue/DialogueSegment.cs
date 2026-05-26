using System;
using System.Collections.Generic;
using UnityEngine;

namespace ProjectAstra.Core.Dialogue
{
    // A run of lines that share a background, text-crawl speed, and auto-advance
    // timing. Authors set those three once here instead of repeating them on every
    // line; speaker / expression / portrait still change per line inside. When the
    // background (or speed/auto-advance) needs to change, start a new segment.
    [Serializable]
    public class DialogueSegment
    {
        [Tooltip("Scene shown behind every line in this segment. Start a new segment when it changes.")]
        [SerializeField] private Sprite _background;

        [Tooltip("Characters per second for this segment's lines. Leave below 0 to use the global text speed.")]
        [SerializeField] private float _textSpeed = -1f;

        [Tooltip("Seconds before lines auto-advance. Leave at 0 to wait for the player.")]
        [SerializeField] private float _autoAdvanceDelay = 0f;

        [SerializeField] private List<DialogueLine> _lines = new();

        public Sprite Background => _background;
        public float TextSpeed => _textSpeed;
        public float AutoAdvanceDelay => _autoAdvanceDelay;
        public IReadOnlyList<DialogueLine> Lines => _lines;

        internal static DialogueSegment CreateForTest(Sprite background, float textSpeed,
            float autoAdvanceDelay, params DialogueLine[] lines)
        {
            return new DialogueSegment
            {
                _background = background,
                _textSpeed = textSpeed,
                _autoAdvanceDelay = autoAdvanceDelay,
                _lines = new List<DialogueLine>(lines)
            };
        }
    }
}
