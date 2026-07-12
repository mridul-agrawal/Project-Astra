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
        [SerializeField] private Sprite background;

        [Tooltip("Characters per second for this segment's lines. Leave below 0 to use the global text speed.")]
        [SerializeField] private float textSpeed = -1f;

        [Tooltip("Seconds before lines auto-advance. Leave at 0 to wait for the player.")]
        [SerializeField] private float autoAdvanceDelay = 0f;

        [SerializeField] private List<DialogueLine> lines = new();

        public Sprite Background => background;
        public float TextSpeed => textSpeed;
        public float AutoAdvanceDelay => autoAdvanceDelay;
        public IReadOnlyList<DialogueLine> Lines => lines;

        internal static DialogueSegment Create(Sprite background, float textSpeed,
            float autoAdvanceDelay, params DialogueLine[] lines)
        {
            return new DialogueSegment
            {
                background = background,
                textSpeed = textSpeed,
                autoAdvanceDelay = autoAdvanceDelay,
                lines = new List<DialogueLine>(lines)
            };
        }
    }
}
