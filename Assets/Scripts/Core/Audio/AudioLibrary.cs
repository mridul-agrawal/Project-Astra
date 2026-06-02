using System;
using System.Collections.Generic;
using UnityEngine;

namespace ProjectAstra.Core.Audio
{
    [CreateAssetMenu(menuName = "Project Astra/Audio/Audio Library")]
    public class AudioLibrary : ScriptableObject
    {
        [Serializable]
        private struct Entry
        {
            public SoundId id;
            public SoundSO sound;
        }

        [SerializeField] private Entry[] _entries = Array.Empty<Entry>();

        private Dictionary<SoundId, SoundSO> _lookup;

        public SoundSO Resolve(SoundId id)
        {
            EnsureBuilt();
            return _lookup.TryGetValue(id, out var sound) ? sound : null;
        }

        private void OnEnable() => _lookup = null;

        private void EnsureBuilt()
        {
            if (_lookup != null) return;
            _lookup = new Dictionary<SoundId, SoundSO>(_entries.Length);
            foreach (var entry in _entries)
                if (entry.sound != null) _lookup[entry.id] = entry.sound;
        }
    }
}
