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
    /// Subscribes to GridCursor.OnCursorMoved + TurnEventChannel phase/turn events and updates
    /// the live TMP/Image widgets. Mirrors Fire Emblem GBA HUD behaviour: the Unit Card shows
    /// the unit currently under the cursor and hides when no unit is hovered.
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
        public Image PortraitImage;
        public Sprite DefaultPortrait;

        [Header("Objective Panel")]
        public TextMeshProUGUI ObjText;
        public TextMeshProUGUI TurnNum;

        [Header("Tile Info Panel")]
        public TextMeshProUGUI TileName;
        public TextMeshProUGUI StatValueDef;
        public TextMeshProUGUI StatValueAvo;

        [Header("Data Sources")]
        public TerrainStatTable StatTable;
        public TurnEventChannel TurnChannel;

        [Header("Configuration")]
        [TextArea] public string ObjectiveText = "Slay the Asura Lord";

        // ------------------------------------------------------------------
        // Runtime references discovered at Awake/Start.
        // ------------------------------------------------------------------
        private GridCursor _cursor;
        private MapRenderer _map;

        private void Awake()
        {
            _cursor = FindFirstObjectByType<GridCursor>();
            _map    = FindFirstObjectByType<MapRenderer>();

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
            // Initial refresh once the scene has booted (cursor + units + map all alive).
            if (_cursor != null) HandleCursorMoved(_cursor.GridPosition);
            var tm = TurnManager.Instance;
            SetTurn(tm != null ? tm.TurnCounter : 1);
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
            var terrain = _map != null ? _map.GetTerrainType(pos.x, pos.y) : TerrainType.Plain;
            var unit = FindUnitAt(pos);
            var moveType = unit != null ? unit.movementType : MovementType.Foot;
            SetTile(terrain, moveType);
            SetUnit(unit);
        }

        private void HandlePhaseStarted(BattlePhase phase, int turnNumber) => SetTurn(turnNumber);
        private void HandleTurnAdvanced(int turnNumber) => SetTurn(turnNumber);

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

            if (UnitName  != null) UnitName.text  = GetUnitDisplayName(unit);
            if (UnitClass != null) UnitClass.text = GetUnitClassLabel(unit);
            if (HpValue   != null) HpValue.text   = unit.currentHP + " / " + unit.maxHP;
            if (HpFill    != null)
                HpFill.fillAmount = unit.maxHP > 0 ? (float)unit.currentHP / unit.maxHP : 0f;
            if (PortraitImage != null && DefaultPortrait != null)
                PortraitImage.sprite = DefaultPortrait;
        }

        public void SetTile(TerrainType terrain, MovementType movementType)
        {
            if (TileName != null) TileName.text = ToDisplayName(terrain);

            int def = 0, avo = 0;
            if (StatTable != null)
            {
                var stats = StatTable.GetStats(terrain);
                (def, avo) = TerrainStatTable.GetTerrainBonuses(stats, movementType);
            }
            if (StatValueDef != null) StatValueDef.text = FormatStat(def);
            if (StatValueAvo != null) StatValueAvo.text = FormatStat(avo);
        }

        public void SetTurn(int turnNumber)
        {
            if (TurnNum != null) TurnNum.text = turnNumber.ToString("00");
        }

        // ------------------------------------------------------------------
        // Lookup helpers — MVP fallbacks for missing unit metadata.
        // TODO: replace with UnitDefinition lookup once the real unit pipeline lands.
        // ------------------------------------------------------------------

        private static string GetUnitDisplayName(TestUnit u) => u.gameObject.name;

        private static string GetUnitClassLabel(TestUnit u)
            => (u.movementType + " · " + u.faction).ToUpperInvariant();

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
