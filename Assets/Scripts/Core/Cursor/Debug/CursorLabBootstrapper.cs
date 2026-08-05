using System.Collections.Generic;
using UnityEngine;
using ProjectAstra.Core.Turn;
using ProjectAstra.Core.Units;

namespace ProjectAstra.Core.Cursor.Debugging
{
    // Stands up the cursor evaluation scenario: five allies, three of them already spent, and
    // two enemies. The point is the 3-of-5 read — whether ready and acted allies are telling
    // apart at a glance from the unit treatment plus the cursor's response alone.
    //
    // Lives only in CursorLab. Nothing in the shipped game references it.
    public class CursorLabBootstrapper : MonoBehaviour
    {
        [Header("Roster")]
        [Tooltip("Where the five player units start. The first three are pre-marked as having already acted.")]
        [SerializeField] private Vector2Int[] allyPositions =
        {
            new(3, 3), new(4, 3), new(5, 3), new(6, 3), new(7, 3),
        };

        [Tooltip("How many of the allies start spent. Kept separate from the position list so you can retune the mix without moving anyone.")]
        [Range(0, 5)][SerializeField] private int actedAllyCount = 3;

        [SerializeField] private Vector2Int[] enemyPositions = { new(10, 5), new(11, 5) };

        [Header("Definitions")]
        [Tooltip("Unit definition given to every spawned ally. Any definition works — this scenario is about the cursor, not the units.")]
        [SerializeField] private UnitDefinition allyDefinition;

        [SerializeField] private UnitDefinition enemyDefinition;

        [Tooltip("Sprite used when a definition has no map art. Leave empty to spawn units with no visible sprite.")]
        [SerializeField] private Sprite placeholderSprite;

        private readonly List<TestUnit> spawned = new();

        private void Start() => Rebuild();

        [ContextMenu("Rebuild scenario")]
        public void Rebuild()
        {
            Clear();

            for (int i = 0; i < allyPositions.Length; i++)
                spawned.Add(Spawn($"Ally_{i}", allyPositions[i], Faction.Player, allyDefinition, i < actedAllyCount));

            for (int i = 0; i < enemyPositions.Length; i++)
                spawned.Add(Spawn($"Enemy_{i}", enemyPositions[i], Faction.Enemy, enemyDefinition, false));

            RegisterWithTurnManager();
        }

        public void Clear()
        {
            foreach (var unit in spawned)
                if (unit != null) Destroy(unit.gameObject);
            spawned.Clear();
        }

        private TestUnit Spawn(string name, Vector2Int position, Faction faction,
            UnitDefinition definition, bool startsActed)
        {
            var go = new GameObject(name);
            go.transform.SetParent(transform, false);

            var sprite = new GameObject("UnitSprite");
            sprite.transform.SetParent(go.transform, false);
            var renderer = sprite.AddComponent<SpriteRenderer>();
            renderer.sprite = definition != null && definition.MapSprite != null
                ? definition.MapSprite
                : placeholderSprite;
            renderer.sortingLayerName = "Units";
            renderer.color = faction == Faction.Enemy ? new Color(1f, 0.7f, 0.7f) : Color.white;

            var unit = go.AddComponent<TestUnit>();
            unit.faction = faction;
            unit.gridPosition = position;
            unit.movementPoints = 5;
            unit.maxHP = unit.currentHP = 20;
            if (definition != null) unit.InitializeFromDefinition(definition, null);
            unit.SnapToGridPosition();

            // Deferred a frame's worth of setup: MarkActed needs the sprite renderer cached,
            // which TestUnit does in Start.
            if (startsActed) pendingActed.Add(unit);

            return unit;
        }

        private readonly List<TestUnit> pendingActed = new();

        private void LateUpdate()
        {
            if (pendingActed.Count == 0) return;

            foreach (var unit in pendingActed)
            {
                if (unit == null) continue;
                if (TurnManager.Instance != null) TurnManager.Instance.UnitRegistry.MarkActed(unit);
                else unit.MarkActed();
            }
            pendingActed.Clear();
        }

        private void RegisterWithTurnManager()
        {
            if (TurnManager.Instance == null) return;
            foreach (var unit in spawned)
                TurnManager.Instance.UnitRegistry.Register(unit, unit.faction);
        }
    }
}
