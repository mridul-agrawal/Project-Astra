using UnityEngine;

namespace ProjectAstra.Core
{
    [CreateAssetMenu(menuName = "Project Astra/Units/Unit Definition")]
    public class UnitDefinition : ScriptableObject
    {
        [Header("Identity")]
        [SerializeField] private string _unitName;
        [SerializeField] private string _unitId;

        [Header("Class")]
        [SerializeField] private ClassDefinition _defaultClass;

        [Header("Base Stats (Level 1)")]
        [SerializeField] private StatArray _baseStats;
        [SerializeField] private int _baseLevel = 1;

        [Header("Growth Rates (0-100)")]
        [SerializeField] private StatArray _personalGrowths;

        [Header("Portrait (full HP)")]
        [SerializeField] private Sprite _portrait;

        [Header("Portrait Variants (optional — fallback to _portrait)")]
        [SerializeField] private Sprite _woundedPortrait;     // HP < 50%
        [SerializeField] private Sprite _criticalPortrait;    // HP < 25%
        [SerializeField] private Sprite _deceasedPortrait;    // CurrentHP == 0
        [SerializeField] private Sprite _stressedOverlay;     // USE-04 Tier >= 1 overlay

        [Header("Identity (story)")]
        [SerializeField] private PanchaBhuta _affinity = PanchaBhuta.None;    // SP-03
        [SerializeField] private Personality _personality = Personality.None; // CC-01, allied NPCs only

        [Header("Enemy commander metadata (UM-01 War's Ledger)")]
        [Tooltip("Mark enemy UnitDefinitions that should fire a named-enemy death event. Player characters always count as named via UnitId.")]
        [SerializeField] private bool _isNamedCommander;
        [Tooltip("One-line identity line shown in the Ledger's left column when this unit dies. Example: \"Commander of the Eastern Wall, sworn to protect the border villages.\"")]
        [SerializeField, TextArea] private string _oneLineIdentity;

        [Header("Lord (UM-02)")]
        [Tooltip("Exactly one player unit in the campaign should have this set. When the Lord dies, the chapter ends in Game Over regardless of other survivors.")]
        [SerializeField] private bool _isLord;
        [Tooltip("2–4 authored lines played as a dialogue sequence when this unit dies. Only consulted when IsLord is true.")]
        [SerializeField, TextArea(1, 3)] private string[] _lastWordsLines;

        public string UnitName => _unitName;
        public string UnitId => _unitId;
        public ClassDefinition DefaultClass => _defaultClass;
        public StatArray BaseStats => _baseStats;
        public int BaseLevel => _baseLevel;
        public StatArray PersonalGrowths => _personalGrowths;
        public Sprite Portrait => _portrait;
        public Sprite WoundedPortrait => _woundedPortrait;
        public Sprite CriticalPortrait => _criticalPortrait;
        public Sprite DeceasedPortrait => _deceasedPortrait;
        public Sprite StressedOverlay => _stressedOverlay;
        public PanchaBhuta Affinity => _affinity;
        public Personality Personality => _personality;
        public bool IsNamedCommander => _isNamedCommander;
        public string OneLineIdentity => _oneLineIdentity;
        public bool IsLord => _isLord;
        public string[] LastWordsLines => _lastWordsLines;
    }
}
