using System.Text.RegularExpressions;
using ProjectAstra.Core;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace ProjectAstra.UI
{
    /// <summary>
    /// Runtime controller for the Battle Map HUD (Unit Card / Objective Panel / Tile Info Panel).
    /// Attached to Canvas/BattleMapHUD by BattleMapHUDBuilder; fields are wired at build time.
    ///
    /// Subscribes to GridCursor.OnCursorMoved and TurnEventChannel phase/turn events and updates
    /// the live TMP/Image widgets. Mirrors Fire Emblem GBA HUD behaviour:
    ///  - Unit Card shows the unit under the cursor and hides when no unit is hovered.
    ///  - Tile Info Panel auto-swaps between bottom-left and bottom-right based on cursor side.
    ///  - Both panels hide during non-Player phases; Objective panel stays visible.
    ///
    /// Fog-of-War visibility gating is a future task; search for the "FOG OF WAR TODO"
    /// markers when the MapVisibility system lands.
    /// </summary>
    public class BattleMapHUDController : MonoBehaviour
    {
        // ------------------------------------------------------------------
        // References — wired by BattleMapHUDBuilder at build time.
        // Public fields so the Editor-assembly builder can assign them without
        // SerializedObject gymnastics. Inspector treats them identically to
        // [SerializeField] private.
        // ------------------------------------------------------------------

        [Header("Unit Card")]
        public GameObject UnitCardRoot;
        public TextMeshProUGUI UnitName;
        public TextMeshProUGUI UnitClass;
        public TextMeshProUGUI HpValue;
        public Image HpFill;
        public TextMeshProUGUI WeaponName;
        public Image PortraitImage;
        public Sprite DefaultPortrait;

        [Header("Objective Panel")]
        public TextMeshProUGUI ObjText;
        public TextMeshProUGUI TurnNum;

        [Header("Tile Info Panel")]
        public GameObject TileInfoRoot;
        public TextMeshProUGUI TileName;
        public TextMeshProUGUI StatValueDef;
        public TextMeshProUGUI StatValueAvo;
        public TextMeshProUGUI HealValue;

        [Header("Data Sources")]
        public TerrainStatTable StatTable;
        public TurnEventChannel TurnChannel;

        [Header("Configuration")]
        [TextArea] public string ObjectiveText = "Slay the Asura Lord";

        // ------------------------------------------------------------------
        // Cached at Awake — corner bosses for faction tint, TileInfo rect for side-swap.
        // ------------------------------------------------------------------
        private GridCursor _cursor;
        private MapRenderer _map;
        private Image[] _unitCardBosses;
        private RectTransform _tileInfoRect;
        private bool _tileInfoOnLeft;

        // HP threshold colours (CSS source: mockup gradient #2e7a3a → #8de078)
        private static readonly Color HpGreen  = new Color32(0x8d, 0xe0, 0x78, 0xff);
        private static readonly Color HpYellow = new Color32(0xf0, 0xd0, 0x60, 0xff);
        private static readonly Color HpRed    = new Color32(0xe8, 0x40, 0x2a, 0xff);

        // TileInfoPanel two-position presets. Controller overrides anchor/pivot/pos
        // to flip between bottom-left and bottom-right based on cursor side.
        private const float EdgePad = 56f;
        private static readonly Vector2 TileInfoLeftPos  = new Vector2( EdgePad,  EdgePad);
        private static readonly Vector2 TileInfoRightPos = new Vector2(-EdgePad,  EdgePad);

        private void Awake()
        {
            _cursor = FindFirstObjectByType<GridCursor>();
            _map    = FindFirstObjectByType<MapRenderer>();

            CacheUnitCardBosses();
            if (TileInfoRoot != null) _tileInfoRect = TileInfoRoot.GetComponent<RectTransform>();
            // Assume the build-time default matches bottom-right so the first swap check fires cleanly.
            _tileInfoOnLeft = false;

            if (_cursor != null) _cursor.OnCursorMoved += HandleCursorMoved;
            if (TurnChannel != null)
            {
                TurnChannel.RegisterPhaseStarted(HandlePhaseStarted);
                TurnChannel.RegisterTurnAdvanced(HandleTurnAdvanced);
            }

            if (ObjText != null) ObjText.text = ObjectiveText;
        }

        private void Start()
        {
            // Apply initial phase visibility + run a first cursor refresh so the HUD
            // comes alive with whatever state the scene boots into.
            var tm = TurnManager.Instance;
            ApplyPhaseVisibility(tm != null ? tm.CurrentPhase : BattlePhase.PlayerPhase);
            SetTurn(tm != null ? tm.TurnCounter : 1);

            if (_cursor != null) HandleCursorMoved(_cursor.GridPosition);
        }

        private void OnDestroy()
        {
            if (_cursor != null) _cursor.OnCursorMoved -= HandleCursorMoved;
            if (TurnChannel != null)
            {
                TurnChannel.UnregisterPhaseStarted(HandlePhaseStarted);
                TurnChannel.UnregisterTurnAdvanced(HandleTurnAdvanced);
            }
        }

        // ------------------------------------------------------------------
        // Event handlers
        // ------------------------------------------------------------------

        private void HandleCursorMoved(Vector2Int pos)
        {
            // Player-Phase gate: per the spec, tile info only updates during Player Phase.
            var tm = TurnManager.Instance;
            if (tm != null && tm.CurrentPhase != BattlePhase.PlayerPhase) return;

            var terrain = _map != null ? _map.GetTerrainType(pos.x, pos.y) : TerrainType.Plain;
            var unit    = FindUnitAt(pos);
            var moveType = unit != null ? unit.movementType : MovementType.Foot;

            SetTile(terrain, moveType);
            SetUnit(unit);
            UpdateTileInfoSide(pos);
        }

        private void HandlePhaseStarted(BattlePhase phase, int turnNumber)
        {
            ApplyPhaseVisibility(phase);
            SetTurn(turnNumber);
        }

        private void HandleTurnAdvanced(int turnNumber) => SetTurn(turnNumber);

        // ------------------------------------------------------------------
        // Phase visibility — hide unit/tile panels during non-Player phases.
        // ------------------------------------------------------------------

        public void ApplyPhaseVisibility(BattlePhase phase)
        {
            bool playerPhase = phase == BattlePhase.PlayerPhase;
            if (TileInfoRoot != null) TileInfoRoot.SetActive(playerPhase);
            // UnitCardRoot may independently hide via SetUnit(null); during non-Player
            // phases, force it off regardless of what the cursor is over.
            if (UnitCardRoot != null && !playerPhase) UnitCardRoot.SetActive(false);
        }

        // ------------------------------------------------------------------
        // Tile Info Panel side-swap (FE GBA style — opposite the cursor).
        // ------------------------------------------------------------------

        private void UpdateTileInfoSide(Vector2Int cursorGridPos)
        {
            if (_tileInfoRect == null) return;

            int mapWidth = (_map != null && _map.CurrentMap != null) ? _map.CurrentMap.Width : 20;
            bool cursorOnLeft = cursorGridPos.x < mapWidth / 2;
            bool panelOnLeft  = !cursorOnLeft;

            if (panelOnLeft == _tileInfoOnLeft) return;
            _tileInfoOnLeft = panelOnLeft;

            if (panelOnLeft)
            {
                _tileInfoRect.anchorMin = _tileInfoRect.anchorMax = new Vector2(0, 0);
                _tileInfoRect.pivot     = new Vector2(0, 0);
                _tileInfoRect.anchoredPosition = TileInfoLeftPos;
            }
            else
            {
                _tileInfoRect.anchorMin = _tileInfoRect.anchorMax = new Vector2(1, 0);
                _tileInfoRect.pivot     = new Vector2(1, 0);
                _tileInfoRect.anchoredPosition = TileInfoRightPos;
            }
        }

        // ------------------------------------------------------------------
        // Typed setters — single-responsibility, easy to drive from tests or debug commands.
        // ------------------------------------------------------------------

        public void SetUnit(TestUnit unit)
        {
            if (unit == null)
            {
                if (UnitCardRoot != null) UnitCardRoot.SetActive(false);
                return;
            }
            if (UnitCardRoot != null) UnitCardRoot.SetActive(true);

            if (UnitName  != null) UnitName.text  = ResolveUnitName(unit);
            if (UnitClass != null) UnitClass.text = ResolveClassName(unit);
            if (HpValue   != null) HpValue.text   = unit.currentHP + " / " + unit.maxHP;

            float frac = unit.maxHP > 0 ? (float)unit.currentHP / unit.maxHP : 0f;
            if (HpFill != null)
            {
                HpFill.fillAmount = frac;
                HpFill.color      = HpColorForFraction(frac);
            }

            TintCornerBosses(FactionTint(unit.faction));

            if (WeaponName != null)
            {
                var wpn = unit.equippedWeapon;
                WeaponName.text = wpn.IsEmpty ? "Unarmed" : wpn.name;
            }

            if (PortraitImage != null && DefaultPortrait != null)
                PortraitImage.sprite = DefaultPortrait;

            // FOG OF WAR TODO: hide UnitCard entirely when unit is an unrevealed enemy
            // in a fog chapter, per R14. Requires a MapVisibility service that does not
            // yet exist in the codebase.
        }

        public void SetTile(TerrainType terrain, MovementType movementType)
        {
            if (TileName != null) TileName.text = ToDisplayName(terrain);

            int def = 0, avo = 0, heal = 0;
            if (StatTable != null)
            {
                var stats = StatTable.GetStats(terrain);
                (def, avo) = TerrainStatTable.GetTerrainBonuses(stats, movementType);
                heal = stats.healPerTurn;
            }
            if (StatValueDef != null) StatValueDef.text = FormatStat(def);
            if (StatValueAvo != null) StatValueAvo.text = FormatStat(avo);

            if (HealValue != null)
            {
                if (heal > 0)
                {
                    HealValue.gameObject.SetActive(true);
                    HealValue.text = "Heal " + FormatStat(heal) + " / turn";
                }
                else
                {
                    HealValue.gameObject.SetActive(false);
                }
            }

            // FOG OF WAR TODO: when MapVisibility says the tile is unrevealed, show
            // "???" for TileName and hide the bonus rows, per R14.
        }

        public void SetTurn(int turnNumber)
        {
            if (TurnNum != null) TurnNum.text = turnNumber.ToString("00");
        }

        // ------------------------------------------------------------------
        // Unit / terrain display helpers
        // ------------------------------------------------------------------

        private static string ResolveUnitName(TestUnit u)
        {
            var def = u.UnitInstance != null ? u.UnitInstance.Definition : null;
            if (def != null && !string.IsNullOrEmpty(def.UnitName)) return def.UnitName;
            return u.gameObject.name;
        }

        private static string ResolveClassName(TestUnit u)
        {
            var def = u.UnitInstance != null ? u.UnitInstance.Definition : null;
            var cls = def != null ? def.DefaultClass : null;
            if (cls != null && !string.IsNullOrEmpty(cls.ClassName)) return cls.ClassName;
            // Fallback when the unit has no UnitDefinition bound (TestUnit-only setup).
            return (u.movementType + " · " + u.faction).ToUpperInvariant();
        }

        private static Color HpColorForFraction(float frac)
        {
            if (frac > 0.50f) return HpGreen;
            if (frac > 0.25f) return HpYellow;
            return HpRed;
        }

        private static Color FactionTint(Faction f)
        {
            switch (f)
            {
                case Faction.Enemy:  return new Color(1.00f, 0.45f, 0.45f); // reddened gold
                case Faction.Allied: return new Color(0.55f, 0.80f, 1.00f); // bluish gold
                default:             return Color.white;                    // Player: unchanged gold
            }
        }

        private void TintCornerBosses(Color tint)
        {
            if (_unitCardBosses == null) return;
            for (int i = 0; i < _unitCardBosses.Length; i++)
                if (_unitCardBosses[i] != null) _unitCardBosses[i].color = tint;
        }

        private void CacheUnitCardBosses()
        {
            if (UnitCardRoot == null) { _unitCardBosses = System.Array.Empty<Image>(); return; }
            var all = UnitCardRoot.GetComponentsInChildren<Image>(true);
            var result = new System.Collections.Generic.List<Image>(4);
            for (int i = 0; i < all.Length; i++)
                if (all[i].gameObject.name == "CornerBoss") result.Add(all[i]);
            _unitCardBosses = result.ToArray();
        }

        private static string FormatStat(int v) => v >= 0 ? "+" + v : v.ToString();

        // "TempleFloor" -> "Temple Floor", "Plain" -> "Plain"
        private static string ToDisplayName(TerrainType t)
            => Regex.Replace(t.ToString(), "(?<!^)([A-Z])", " $1");

        private static TestUnit FindUnitAt(Vector2Int pos)
        {
            var all = FindObjectsByType<TestUnit>(FindObjectsSortMode.None);
            for (int i = 0; i < all.Length; i++)
                if (all[i].gridPosition == pos) return all[i];
            return null;
        }
    }
}
