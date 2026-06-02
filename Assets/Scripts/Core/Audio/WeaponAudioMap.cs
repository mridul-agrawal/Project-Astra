using System;
using UnityEngine;
using ProjectAstra.Core.Combat;

namespace ProjectAstra.Core.Audio
{
    // Picks the right sound asset for an attacker's weapon. Each weapon type
    // has its own impact and crit-impact sounds; unfilled entries fall through
    // to the defaults so a new weapon type still plays something.
    [CreateAssetMenu(menuName = "Project Astra/Audio/Weapon Audio Map", fileName = "WeaponAudioMap")]
    public class WeaponAudioMap : ScriptableObject
    {
        [Serializable]
        public struct Entry
        {
            public WeaponType weaponType;
            public SoundSO impact;
            public SoundSO critImpact;
        }

        [Header("Fallbacks (used when a per-type entry is unset)")]
        [SerializeField] private SoundSO _defaultImpact;
        [SerializeField] private SoundSO _defaultCritImpact;
        [SerializeField] private SoundSO _missWhoosh;

        [Header("Per-weapon overrides")]
        [SerializeField] private Entry[] _entries;

        public SoundSO GetImpact(WeaponType type, bool crit)
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

        public SoundSO GetMiss() => _missWhoosh;

        private Entry FindEntry(WeaponType type)
        {
            if (_entries == null) return default;
            for (int i = 0; i < _entries.Length; i++)
                if (_entries[i].weaponType == type) return _entries[i];
            return default;
        }
    }
}
