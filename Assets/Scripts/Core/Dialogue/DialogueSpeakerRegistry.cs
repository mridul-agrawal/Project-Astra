using System.Collections.Generic;
using UnityEngine;

namespace ProjectAstra.Core.Dialogue
{
    // The lookup from a node's SpeakerId string to its DialogueSpeaker asset.
    // One shared asset the DialogueService references; authors drop every speaker in.
    [CreateAssetMenu(fileName = "DialogueSpeakerRegistry", menuName = "Project Astra/Dialogue/Speaker Registry")]
    public class DialogueSpeakerRegistry : ScriptableObject
    {
        // SpeakerId reserved for narration/system lines — resolves to no portrait.
        public const string NarratorId = "NARRATOR";

        [SerializeField] private List<DialogueSpeaker> _speakers = new();

        private Dictionary<string, DialogueSpeaker> _byId;

        public bool TryResolve(string speakerId, out DialogueSpeaker speaker)
        {
            EnsureIndexBuilt();
            return _byId.TryGetValue(speakerId ?? string.Empty, out speaker);
        }

        public static bool IsNarrator(string speakerId) => speakerId == NarratorId;

        private void EnsureIndexBuilt()
        {
            if (_byId != null) return;
            _byId = new Dictionary<string, DialogueSpeaker>();
            foreach (var speaker in _speakers)
                if (speaker != null && !string.IsNullOrEmpty(speaker.SpeakerId))
                    _byId[speaker.SpeakerId] = speaker;
        }

        internal static DialogueSpeakerRegistry CreateForTest(params DialogueSpeaker[] speakers)
        {
            var registry = CreateInstance<DialogueSpeakerRegistry>();
            registry._speakers = new List<DialogueSpeaker>(speakers);
            return registry;
        }
    }
}
