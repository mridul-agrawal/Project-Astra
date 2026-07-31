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
        [SerializeField, HubRef] private ClassDefinition defaultClass;

        [Header("Inventory")]
        [Tooltip("Default starting kit, baked into the unit's inventory on spawn. A per-map UnitStartPosition override takes precedence.")]
        [SerializeField, HubRef] private InventoryLoadout defaultLoadout;

        [Header("Base Level & Base Stats")]
        [Tooltip("The level this unit is introduced at — not necessarily 1. Drives EXP scaling and the level shown in-game.")]
        [SerializeField] private int baseLevel = 1;
        [Tooltip("The unit's ACTUAL stats at its Base Level — authored directly, NOT auto-scaled from level 1. Introducing a level-5 unit? Enter its level-5 stats here and set Base Level to 5.")]
        [SerializeField] private StatArray baseStats;

        [Header("Personal Growth Rates (% chance per level-up)")]
        [Tooltip("Per stat: the % chance (0-100) to gain a point on level-up. Added to the class's growth modifier. On a hit, HP gains the class's HP-per-level; other stats gain +1. Con never grows.")]
        [SerializeField, GrowthRate] private StatArray personalGrowths;

        [Header("Map sprite (on-grid token)")]
        [SerializeField] private Sprite mapSprite;

        [Header("Map animation (optional — falls back to Map sprite when empty)")]
        [Tooltip("Override controller built from this character's frames (idle / run x4 / selected) via the Unit Animator Builder. Leave empty to render the static Map sprite instead.")]
        [SerializeField] private RuntimeAnimatorController mapAnimator;

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

        [Header("Weapon Ranks (authored — runtime seeding pending)")]
        [Tooltip("Starting proficiency per weapon type. Authored now, but not yet seeded into the runtime WeaponRankTracker at spawn (see UnitSpawner TODO).")]
        [SerializeField] private StartingWeaponRank[] startingWeaponRanks;

        [Header("Dialogue")]
        [Tooltip("Optional. Which dialogue speaker voices this unit (portrait + name) for runtime lines such as the Lord's last words. Leave empty for nameless narration.")]
        [SerializeField, HubRef] private DialogueSpeaker speaker;

        public string UnitName => unitName;
        public string UnitId => unitId;
        public ClassDefinition DefaultClass => defaultClass;
        public InventoryLoadout DefaultLoadout => defaultLoadout;
        public StatArray BaseStats => baseStats;
        public int BaseLevel => baseLevel;
        public StatArray PersonalGrowths => personalGrowths;
        public Sprite MapSprite => mapSprite;
        public RuntimeAnimatorController MapAnimator => mapAnimator;
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
        public StartingWeaponRank[] StartingWeaponRanks => startingWeaponRanks;
        public DialogueSpeaker Speaker => speaker;
    }
}
