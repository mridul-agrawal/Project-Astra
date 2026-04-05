using UnityEditor;
using UnityEngine;
using ProjectAstra.Core;

namespace ProjectAstra.Core.Editor
{
    public static class CursorSceneSetup
    {
        [MenuItem("Project Astra/Map/Setup Cursor & Test Unit in Scene")]
        public static void Setup()
        {
            var mapRenderer = Object.FindAnyObjectByType<MapRenderer>();
            if (mapRenderer == null)
            {
                Debug.LogError("CursorSceneSetup: No MapRenderer found in scene. Create the Map Grid first.");
                return;
            }

            var stateChannel = AssetDatabase.LoadAssetAtPath<GameStateEventChannel>(
                "Assets/ScriptableObjects/Core/GameStateChanged.asset");
            var terrainStatTable = AssetDatabase.LoadAssetAtPath<TerrainStatTable>(
                "Assets/ScriptableObjects/Map/TerrainStatTable.asset");
            var cursorSprite = AssetDatabase.LoadAssetAtPath<Sprite>(
                "Assets/Art/Cursor/PlaceholderCursor.png");
            var unitSprite = AssetDatabase.LoadAssetAtPath<Sprite>(
                "Assets/Art/Cursor/PlaceholderUnitCircle.png");

            if (cursorSprite == null || unitSprite == null)
            {
                Debug.LogError("CursorSceneSetup: Sprites not found. Run 'Generate Placeholder Cursor & Unit Sprites' first.");
                return;
            }

            SetupGridCursor(mapRenderer, stateChannel, terrainStatTable, cursorSprite);
            SetupTestUnit(unitSprite);
            SetupCameraController(mapRenderer);

            UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(
                UnityEditor.SceneManagement.EditorSceneManager.GetActiveScene());
            Debug.Log("GridCursor, TestUnit, and CameraController added to scene.");
        }

        private static void SetupGridCursor(MapRenderer mapRenderer,
            GameStateEventChannel stateChannel, TerrainStatTable terrainStatTable, Sprite cursorSprite)
        {
            // Don't create duplicate
            var existing = Object.FindAnyObjectByType<GridCursor>();
            if (existing != null)
            {
                Debug.Log("CursorSceneSetup: GridCursor already exists, skipping.");
                return;
            }

            var cursorGO = new GameObject("GridCursor");

            // Create child sprite
            var spriteGO = new GameObject("CursorSprite");
            spriteGO.transform.SetParent(cursorGO.transform, false);
            var sr = spriteGO.AddComponent<SpriteRenderer>();
            sr.sprite = cursorSprite;
            sr.sortingLayerName = "UIOverlay";
            sr.sortingOrder = 0;

            // Add helper components
            var highlighter = cursorGO.AddComponent<RangeHighlighter>();
            var pathArrow = cursorGO.AddComponent<PathArrowRenderer>();
            var unitMover = cursorGO.AddComponent<UnitMover>();

            // Add GridCursor and wire all references
            var cursor = cursorGO.AddComponent<GridCursor>();
            var so = new SerializedObject(cursor);
            so.FindProperty("_mapRenderer").objectReferenceValue = mapRenderer;
            so.FindProperty("_terrainStatTable").objectReferenceValue = terrainStatTable;
            so.FindProperty("_stateChangedChannel").objectReferenceValue = stateChannel;
            so.FindProperty("_spriteRenderer").objectReferenceValue = sr;
            so.FindProperty("_rangeHighlighter").objectReferenceValue = highlighter;
            so.FindProperty("_pathArrowRenderer").objectReferenceValue = pathArrow;
            so.FindProperty("_unitMover").objectReferenceValue = unitMover;
            so.ApplyModifiedPropertiesWithoutUndo();

            Undo.RegisterCreatedObjectUndo(cursorGO, "Create GridCursor");
        }

        private static void SetupTestUnit(Sprite unitSprite)
        {
            // Don't create duplicate
            var existing = Object.FindAnyObjectByType<TestUnit>();
            if (existing != null)
            {
                Debug.Log("CursorSceneSetup: TestUnit already exists, skipping.");
                return;
            }

            var unitGO = new GameObject("TestUnit");

            // Add TestUnit component
            var unit = unitGO.AddComponent<TestUnit>();
            unit.gridPosition = new Vector2Int(2, 2);
            unit.movementPoints = 3;
            unit.movementType = MovementType.Foot;
            unit.attackRangeMin = 1;
            unit.attackRangeMax = 1;

            // Create child sprite (worldPositionStays=false keeps local position at zero)
            var spriteGO = new GameObject("UnitSprite");
            spriteGO.transform.SetParent(unitGO.transform, false);
            var sr = spriteGO.AddComponent<SpriteRenderer>();
            sr.sprite = unitSprite;
            sr.sortingLayerName = "Units";
            sr.sortingOrder = 0;

            // Position at grid location
            unitGO.transform.position = new Vector3(2.5f, 2.5f, 0f);

            Undo.RegisterCreatedObjectUndo(unitGO, "Create TestUnit");
        }

        private static void SetupCameraController(MapRenderer mapRenderer)
        {
            var cam = Object.FindAnyObjectByType<Camera>();
            if (cam == null)
            {
                Debug.LogError("CursorSceneSetup: No Camera found in scene.");
                return;
            }

            // Don't create duplicate
            if (cam.GetComponent<CameraController>() != null)
            {
                Debug.Log("CursorSceneSetup: CameraController already exists, skipping.");
                return;
            }

            var gridCursor = Object.FindAnyObjectByType<GridCursor>();
            var controller = Undo.AddComponent<CameraController>(cam.gameObject);

            var so = new SerializedObject(controller);
            so.FindProperty("_gridCursor").objectReferenceValue = gridCursor;
            so.FindProperty("_mapRenderer").objectReferenceValue = mapRenderer;
            so.FindProperty("_deadzoneMarginTiles").intValue = 3;
            so.ApplyModifiedPropertiesWithoutUndo();
        }
    }
}
