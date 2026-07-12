using System;
using System.Collections.Generic;
using UnityEngine;

namespace ProjectAstra.Core.Dialogue
{
    // A character that can speak in dialogue: a name label plus a portrait per
    // expression. Kept separate from UnitDefinition on purpose — some speakers
    // (the narrator, the fleeing child) are never playable units, and unit
    // portraits are HP-based, not emotion-based.
    [CreateAssetMenu(fileName = "DialogueSpeaker", menuName = "Project Astra/Dialogue/Speaker")]
    public class DialogueSpeaker : ScriptableObject
    {
        [Serializable]
        private struct ExpressionPortrait
        {
            public DialogueExpression Expression;
            public Sprite Sprite;
        }

        [SerializeField] private string speakerId;
        [SerializeField] private string displayName;
        [SerializeField] private List<ExpressionPortrait> portraits = new();

        public string SpeakerId => speakerId;
        public string DisplayName => displayName;

        // Returns the sprite for the asked expression, falling back to Neutral and
        // then to whatever's authored first — so a missing variant still shows a face.
        public Sprite ResolvePortrait(DialogueExpression expression)
        {
            if (TryFindPortrait(expression, out var match)) return match;
            if (TryFindPortrait(DialogueExpression.Neutral, out var neutral)) return neutral;
            return portraits.Count > 0 ? portraits[0].Sprite : null;
        }

        private bool TryFindPortrait(DialogueExpression expression, out Sprite sprite)
        {
            foreach (var entry in portraits)
            {
                if (entry.Expression != expression) continue;
                sprite = entry.Sprite;
                return sprite != null;
            }
            sprite = null;
            return false;
        }

        internal static DialogueSpeaker CreateForTest(string speakerId, string displayName)
        {
            var speaker = CreateInstance<DialogueSpeaker>();
            speaker.speakerId = speakerId;
            speaker.displayName = displayName;
            return speaker;
        }
    }
}
