using System;
using UnityEngine;
using ProjectAstra.Core.Combat;

namespace ProjectAstra.Core.Audio
{
    // Maps WeaponType to its impact and crit-impact clips. Unfilled entries fall
    // back to _defaultImpact / _defaultCritImpact so a new weapon type never
    // crashes — it just plays the generic thwack until art swaps in.
    [CreateAssetMenu(menuName = "Project Astra/Audio/Weapon Audio Map", fileName = "WeaponAudioMap")]
    public class WeaponAudioMap : ScriptableObject
    {
        [Serializable]
        public struct Entry
        {
            public WeaponType weaponType;
            public AudioClip impact;
            public AudioClip critImpact;
        }

        [Header("Fallbacks (used when a per-type entry is unset)")]
        [SerializeField] private AudioClip _defaultImpact;
        [SerializeField] private AudioClip _defaultCritImpact;
        [SerializeField] private AudioClip _missWhoosh;

        [Header("Per-weapon overrides")]
        [SerializeField] private Entry[] _entries;

        public AudioClip GetImpact(WeaponType type, bool crit)
        {
            var entry = FindEntry(type);
            if (crit)
            {
                if (entry.critImpact != null) return entry.critImpact;
                if (_defaultCritImpact != null) return _defaultCritImpact;
            }
            if (entry.impact != null) return entry.impact;
            return _defaultImpact;
        }

        public AudioClip GetMiss() => _missWhoosh;

        private Entry FindEntry(WeaponType type)
        {
            if (_entries == null) return default;
            for (int i = 0; i < _entries.Length; i++)
                if (_entries[i].weaponType == type) return _entries[i];
            return default;
        }
    }
}
