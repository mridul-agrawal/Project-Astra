using ProjectAstra.Core.Cursor;
using ProjectAstra.Core.Events;
using ProjectAstra.Core.Turn;
using ProjectAstra.Core.Units;
using UnityEngine;

namespace ProjectAstra.Core.UI.BattleMap.HUD
{
    // Controller for the Unit Card: watches the cursor, reads the unit under it,
    // and pushes a UnitCardModel to the View. Holds all the game coupling so the
    // View stays presentation-only. Owns its own phase visibility, so the panel
    // can be enabled/disabled independently of the other two.
    public sealed class UnitCardViewModel : MonoBehaviour
    {
        public UnitCardView View;      // wired by BattleMapHUDBuilder
        public Sprite DefaultPortrait; // wired by BattleMapHUDBuilder

        private GridCursor cursor;

        private void Awake()
        {
            cursor = FindFirstObjectByType<GridCursor>();
            if (cursor != null) cursor.OnCursorMoved += HandleCursorMoved;
            EventService.Instance.SubscribePhaseStarted(HandlePhaseStarted);
        }

        private void Start()
        {
            var tm = TurnManager.Instance;
            ApplyPhase(tm != null ? tm.CurrentPhase : BattlePhase.PlayerPhase);
        }

        private void OnDestroy()
        {
            if (cursor != null) cursor.OnCursorMoved -= HandleCursorMoved;
            if (EventService.Instance != null)
                EventService.Instance.UnsubscribePhaseStarted(HandlePhaseStarted);
        }

        private void HandleCursorMoved(Vector2Int pos)
        {
            // Per spec, the card only tracks the cursor during Player Phase.
            if (!IsPlayerPhase) return;
            if (View != null) View.Render(BuildModel(pos));
        }

        private void HandlePhaseStarted(BattlePhase phase, int turnNumber) => ApplyPhase(phase);

        private void ApplyPhase(BattlePhase phase)
        {
            if (phase == BattlePhase.PlayerPhase)
            {
                if (cursor != null) HandleCursorMoved(cursor.GridPosition);
            }
            else if (View != null)
            {
                View.Render(UnitCardModel.Empty()); // force-hide during non-Player phases
            }
        }

        private UnitCardModel BuildModel(Vector2Int pos)
        {
            var unit = FindUnitAt(pos);
            if (unit == null) return UnitCardModel.Empty();

            // Prefer UnitInstance (UnitDefinition asset + runtime state) as the canonical
            // stat source; fall back to the legacy TestUnit fields for placeholder scenes.
            var inst = unit.UnitInstance;
            return new UnitCardModel
            {
                HasUnit   = true,
                Name      = ResolveUnitName(unit),
                ClassName = ResolveClassName(unit),
                CurrentHP = inst != null ? inst.CurrentHP : unit.currentHP,
                MaxHP     = inst != null ? inst.MaxHP     : unit.maxHP,
                Weapon    = unit.equippedWeapon.IsEmpty ? "Unarmed" : unit.equippedWeapon.name,
                Faction   = unit.faction,
                Portrait  = DefaultPortrait,
            };
        }

        private bool IsPlayerPhase
        {
            get
            {
                var tm = TurnManager.Instance;
                return tm == null || tm.CurrentPhase == BattlePhase.PlayerPhase;
            }
        }

        private static string ResolveUnitName(TestUnit u)
        {
            var def = u.UnitInstance != null ? u.UnitInstance.Definition : null;
            if (def != null && !string.IsNullOrEmpty(def.UnitName)) return def.UnitName;
            return u.gameObject.name;
        }

        private static string ResolveClassName(TestUnit u)
        {
            // CurrentClass reflects promotion; fall back to a movement/faction tag
            // when the unit has no UnitDefinition bound.
            var cls = u.UnitInstance != null ? u.UnitInstance.CurrentClass : null;
            if (cls != null && !string.IsNullOrEmpty(cls.ClassName)) return cls.ClassName;
            return (u.movementType + " · " + u.faction).ToUpperInvariant();
        }

        private static TestUnit FindUnitAt(Vector2Int pos)
        {
            var all = FindObjectsByType<TestUnit>(FindObjectsSortMode.None);
            for (int i = 0; i < all.Length; i++)
                if (all[i].gridPosition == pos) return all[i];
            return null;
        }
    }
}
