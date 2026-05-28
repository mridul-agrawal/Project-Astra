using UnityEngine;
using ProjectAstra.Core.Grid;

namespace ProjectAstra.Core.Units
{
    // Instantiates a battle's units from the map's authored UnitStartPositions. Maps with no
    // authored positions are left untouched, so the legacy test map keeps its scene-placed
    // units. Invoked by MapBootstrapper right after the map is loaded.
    public class UnitSpawner : MonoBehaviour
    {
        [SerializeField] private UnitDatabase _unitDatabase;
        [Tooltip("Drawn when a unit's UnitDefinition has no MapSprite assigned.")]
        [SerializeField] private Sprite _fallbackSprite;

        public void SpawnUnits(MapData map)
        {
            if (map == null || map.UnitStartPositions.Length == 0) return;
            if (_unitDatabase == null)
            {
                Debug.LogError("[UnitSpawner] No UnitDatabase assigned — cannot spawn units.");
                return;
            }

            ClearExistingUnits();
            foreach (UnitStartPosition start in map.UnitStartPositions)
                SpawnUnit(start);
        }

        // A data-driven map owns its roster, so any units already in the scene (e.g. the test
        // map's hand-placed ones) are removed first. Immediate so they're gone before
        // TurnManager registers scene units later this same frame.
        private void ClearExistingUnits()
        {
            foreach (TestUnit existing in FindObjectsByType<TestUnit>(FindObjectsSortMode.None))
                DestroyImmediate(existing.gameObject);
        }

        private void SpawnUnit(UnitStartPosition start)
        {
            if (!_unitDatabase.TryResolve(start.unitId, out UnitDefinition definition))
            {
                Debug.LogError($"[UnitSpawner] No UnitDefinition for id '{start.unitId}' — skipping.");
                return;
            }

            var unitGO = new GameObject(definition.UnitName);
            var unit = unitGO.AddComponent<TestUnit>();
            unit.faction = FactionFromTeam(start.team);
            unit.gridPosition = start.position;
            unit.InitializeFromDefinition(definition);

            AttachSprite(unitGO, definition.MapSprite != null ? definition.MapSprite : _fallbackSprite);
            unit.SnapToGridPosition();
        }

        private static void AttachSprite(GameObject unitGO, Sprite sprite)
        {
            var spriteGO = new GameObject("UnitSprite");
            spriteGO.transform.SetParent(unitGO.transform, false);
            var renderer = spriteGO.AddComponent<SpriteRenderer>();
            renderer.sprite = sprite;
            renderer.sortingLayerName = "Units";
        }

        public static Faction FactionFromTeam(int team) => team switch
        {
            1 => Faction.Enemy,
            2 => Faction.Allied,
            _ => Faction.Player,
        };
    }
}
