using System;
using UnityEngine;

namespace ProjectAstra.Core.UI.UnitInfo
{
    // The nine stats shown on the screen (MOVE included, though it isn't a StatArray slot).
    public enum StatKey { Strength, Magic, Skill, Speed, Defense, Resist, Con, Luck, Move }

    // Per-stat display metadata — label, icon, and the footer description line.
    // Data-driven so a designer edits copy/icons without touching code.
    [CreateAssetMenu(fileName = "StatInfoTable", menuName = "Project Astra/UI/Stat Info Table")]
    public sealed class StatInfoTable : ScriptableObject
    {
        [Serializable]
        public struct Entry
        {
            public StatKey key;
            public string label;
            public Sprite icon;
            [TextArea] public string description;
        }

        [SerializeField] private Entry[] entries;

        public Entry Get(StatKey key)
        {
            if (entries != null)
                foreach (var e in entries)
                    if (e.key == key) return e;
            return new Entry { key = key, label = key.ToString().ToUpper() };
        }
    }
}
