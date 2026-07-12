using UnityEngine;
using ProjectAstra.Core.Stats;
using ProjectAstra.Core.Dialogue;

namespace ProjectAstra.Core.Units
{
    // Authored data for a single unit (one asset per character). Holds
    // identity, default class, base stats, growth rates, portraits, and the
    // story flags (Lord, named commander) that downstream systems key off of.
    [CreateAssetMenu(menuName = "Project Astra/Units/Unit Definition")]
    public class UnitDefinition : ScriptableObject
    {
        [Header("Identity")]
        [SerializeField] private string unitName;
        [SerializeField] private string unitId;

        [Header("Class")]
        [SerializeField] private ClassDefinition defaultClass;

        [Header("Inventory")]
        [Tooltip("Default starting kit, baked into the unit's inventory on spawn. A per-map UnitStartPosition override takes precedence.")]
        [SerializeField] private InventoryLoadout defaultLoadout;

        [Header("Base Stats (Level 1)")]
        [SerializeField] private StatArray baseStats;
        [SerializeField] private int baseLevel = 1;

        [Header("Growth Rates (0-100)")]
        [SerializeField] private StatArray personalGrowths;

        [Header("Map sprite (on-grid token)")]
        [SerializeField] private Sprite mapSprite;

        [Header("Portrait (full HP)")]
        [SerializeField] private Sprite portrait;

        [Header("Portrait Variants (optional — fallback to _portrait)")]
        [SerializeField] private Sprite woundedPortrait;     // HP < 50%
        [SerializeField] private Sprite criticalPortrait;    // HP < 25%
        [SerializeField] private Sprite deceasedPortrait;    // CurrentHP == 0
        [SerializeField] private Sprite stressedOverlay;     // USE-04 Tier >= 1 overlay

        [Header("Identity (story)")]
        [SerializeField] private PanchaBhuta affinity = PanchaBhuta.None;    // SP-03
        [SerializeField] private Personality personality = Personality.None; // CC-01, allied NPCs only

        [Header("Enemy commander metadata (UM-01 War's Ledger)")]
        [Tooltip("Mark enemy UnitDefinitions that should fire a named-enemy death event. Player characters always count as named via UnitId.")]
        [SerializeField] private bool isNamedCommander;
        [Tooltip("One-line identity line shown in the Ledger's left column when this unit dies. Example: \"Commander of the Eastern Wall, sworn to protect the border villages.\"")]
        [SerializeField, TextArea] private string oneLineIdentity;

        [Header("Lord (UM-02)")]
        [Tooltip("Exactly one player unit in the campaign should have this set. When the Lord dies, the chapter ends in Game Over regardless of other survivors.")]
        [SerializeField] private bool isLord;
        [Tooltip("2–4 authored lines played as a dialogue sequence when this unit dies. Only consulted when IsLord is true.")]
        [SerializeField, TextArea(1, 3)] private string[] lastWordsLines;

        [Header("Dialogue")]
        [Tooltip("Optional. Which dialogue speaker voices this unit (portrait + name) for runtime lines such as the Lord's last words. Leave empty for nameless narration.")]
        [SerializeField] private DialogueSpeaker speaker;

        public string UnitName => unitName;
        public string UnitId => unitId;
        public ClassDefinition DefaultClass => defaultClass;
        public InventoryLoadout DefaultLoadout => defaultLoadout;
        public StatArray BaseStats => baseStats;
        public int BaseLevel => baseLevel;
        public StatArray PersonalGrowths => personalGrowths;
        public Sprite MapSprite => mapSprite;
        public Sprite Portrait => portrait;
        public Sprite WoundedPortrait => woundedPortrait;
        public Sprite CriticalPortrait => criticalPortrait;
        public Sprite DeceasedPortrait => deceasedPortrait;
        public Sprite StressedOverlay => stressedOverlay;
        public PanchaBhuta Affinity => affinity;
        public Personality Personality => personality;
        public bool IsNamedCommander => isNamedCommander;
        public string OneLineIdentity => oneLineIdentity;
        public bool IsLord => isLord;
        public string[] LastWordsLines => lastWordsLines;
        public DialogueSpeaker Speaker => speaker;
    }
}
