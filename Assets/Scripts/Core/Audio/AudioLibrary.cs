using System;
using System.Collections.Generic;
using UnityEngine;

namespace ProjectAstra.Core.Audio
{
    // Maps each SoundId to the sound it plays. The one place audio assets are wired.
    [CreateAssetMenu(menuName = "Project Astra/Audio/Audio Library")]
    public class AudioLibrary : ScriptableObject
    {
        [Serializable]
        private struct SoundNode
        {
            public SoundId id;
            public SoundSO sound;
        }

        [SerializeField] private SoundNode[] soundNodes = Array.Empty<SoundNode>();

        private Dictionary<SoundId, SoundSO> soundsById;

        public SoundSO GetSound(SoundId id)
        {
            BuildLookupIfNeeded();
            return soundsById.TryGetValue(id, out var sound) ? sound : null;
        }

        private void OnEnable() => soundsById = null;

        private void BuildLookupIfNeeded()
        {
            if (soundsById != null) return;
            soundsById = new Dictionary<SoundId, SoundSO>(soundNodes.Length);
            foreach (var node in soundNodes)
                if (node.sound != null && node.id != SoundId.None) soundsById[node.id] = node.sound;
        }
    }
}
