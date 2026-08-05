using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using ProjectAstra.Core.Turn;
using ProjectAstra.Core.Units;

namespace ProjectAstra.Core.Cursor.Debugging
{
    // Stands up the cursor evaluation scenario: five allies, three of them already spent, and
    // two enemies. The point is the 3-of-5 read — whether ready and acted allies tell apart at
    // a glance from the unit treatment plus the cursor's response alone.
    //
    // The lab is a copy of BattleMap, which spawns its own roster from the map asset and then
    // starts a battle that resets everyone's acted flag. So this waits for all of that to
    // finish, clears the board, and puts its own roster down — otherwise the two fight and the
    // spent units come back to life on the first phase start.
    //
    // Lives only in CursorLab. Nothing in the shipped game references it.
    public class CursorLabBootstrapper : MonoBehaviour
    {
        [Header("Roster")]
        [Tooltip("Where the five player units start. The first few are marked as already having acted.")]
        [SerializeField] private Vector2Int[] allyPositions =
        {
            new(4, 4), new(5, 4), new(6, 4), new(7, 4), new(8, 4),
        };

        [Tooltip("How many of the allies start spent. Kept separate from the position list so you can retune the mix without moving anyone.")]
        [Range(0, 5)][SerializeField] private int actedAllyCount = 3;

        [SerializeField] private Vector2Int[] enemyPositions = { new(6, 6), new(8, 6) };

        [Header("Definitions")]
        [Tooltip("Unit definition given to every spawned ally. Any definition works — this scenario is about the cursor, not the units.")]
        [SerializeField] private UnitDefinition allyDefinition;

        [SerializeField] private UnitDefinition enemyDefinition;

        [Tooltip("Sprite used when a definition has no map art.")]
        [SerializeField] private Sprite placeholderSprite;

        private readonly List<TestUnit> spawned = new();

        private void Start() => StartCoroutine(BuildAfterSceneSettles());

        [ContextMenu("Rebuild scenario")]
        public void Rebuild() => StartCoroutine(BuildAfterSceneSettles());

        // Two frames: one for the map's own spawner, one for TurnManager.StartBattle and the
        // phase-start flag reset it triggers.
        private IEnumerator BuildAfterSceneSettles()
        {
            yield return null;
            yield return null;

            ClearEveryUnitInTheScene();
            SpawnRoster();
            RegisterWithTurnManager();

            // Last, so nothing downstream resets the flags we just set.
            yield return null;
            ApplyActedFlags();

            PointCursorAtTheRoster();
        }

        private void ClearEveryUnitInTheScene()
        {
            foreach (var unit in FindObjectsByType<TestUnit>(FindObjectsSortMode.None))
                Destroy(unit.gameObject);

            spawned.Clear();
            TurnManager.Instance?.UnitRegistry.Clear();
        }

        private void SpawnRoster()
        {
            for (int i = 0; i < allyPositions.Length; i++)
                spawned.Add(Spawn($"Ally_{i}", allyPositions[i], Faction.Player, allyDefinition));

            for (int i = 0; i < enemyPositions.Length; i++)
                spawned.Add(Spawn($"Enemy_{i}", enemyPositions[i], Faction.Enemy, enemyDefinition));
        }

        private TestUnit Spawn(string name, Vector2Int position, Faction faction, UnitDefinition definition)
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
            renderer.color = faction == Faction.Enemy ? new Color(1f, 0.62f, 0.62f) : Color.white;

            var unit = go.AddComponent<TestUnit>();
            unit.faction = faction;
            unit.gridPosition = position;
            unit.movementPoints = 5;
            unit.maxHP = unit.currentHP = 20;
            if (definition != null) unit.InitializeFromDefinition(definition, null);
            unit.SnapToGridPosition();

            return unit;
        }

        private void ApplyActedFlags()
        {
            for (int i = 0; i < allyPositions.Length && i < actedAllyCount; i++)
            {
                var unit = spawned.Count > i ? spawned[i] : null;
                if (unit == null) continue;

                if (TurnManager.Instance != null) TurnManager.Instance.UnitRegistry.MarkActed(unit);
                else unit.MarkActed();
            }
        }

        // Opens on the first ready ally, so the lab starts on the state worth looking at
        // rather than on an empty corner tile.
        private void PointCursorAtTheRoster()
        {
            var cursor = FindAnyObjectByType<GridCursor>();
            if (cursor == null || actedAllyCount >= allyPositions.Length) return;

            cursor.SetPosition(allyPositions[actedAllyCount]);
        }

        private void RegisterWithTurnManager()
        {
            if (TurnManager.Instance == null) return;
            foreach (var unit in spawned)
                TurnManager.Instance.UnitRegistry.Register(unit, unit.faction);
        }
    }
}
