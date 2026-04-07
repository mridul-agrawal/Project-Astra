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

            var assets = LoadRequiredAssets();
            if (!AreSpritesLoaded(assets))
            {
                Debug.LogError("CursorSceneSetup: Sprites not found. Run 'Generate Placeholder Cursor & Unit Sprites' first.");
                return;
            }

            SetupGridCursor(mapRenderer, assets);
            SetupTestUnit(assets.unitSprite);
            SetupCameraController(mapRenderer);

            MarkSceneDirty();
            Debug.Log("GridCursor, TestUnit, and CameraController added to scene.");
        }

        private struct SceneAssets
        {
            public GameStateEventChannel stateChannel;
            public TerrainStatTable terrainStatTable;
            public Sprite cursorSprite;
            public Sprite unitSprite;
        }

        private static SceneAssets LoadRequiredAssets()
        {
            return new SceneAssets
            {
                stateChannel = AssetDatabase.LoadAssetAtPath<GameStateEventChannel>(
                    "Assets/ScriptableObjects/Core/GameStateChanged.asset"),
                terrainStatTable = AssetDatabase.LoadAssetAtPath<TerrainStatTable>(
                    "Assets/ScriptableObjects/Map/TerrainStatTable.asset"),
                cursorSprite = AssetDatabase.LoadAssetAtPath<Sprite>(
                    "Assets/Art/Cursor/PlaceholderCursor.png"),
                unitSprite = AssetDatabase.LoadAssetAtPath<Sprite>(
                    "Assets/Art/Cursor/PlaceholderUnitCircle.png"),
            };
        }

        private static bool AreSpritesLoaded(SceneAssets assets)
        {
            return assets.cursorSprite != null && assets.unitSprite != null;
        }

        private static void SetupGridCursor(MapRenderer mapRenderer, SceneAssets assets)
        {
            if (AlreadyExistsInScene<GridCursor>()) return;

            var cursorGO = new GameObject("GridCursor");

            var spriteRenderer = CreateChildSprite(cursorGO, "CursorSprite", assets.cursorSprite, "UIOverlay", 0);

            var highlighter = cursorGO.AddComponent<RangeHighlighter>();
            var pathArrow = cursorGO.AddComponent<PathArrowRenderer>();
            var unitMover = cursorGO.AddComponent<UnitMover>();

            var cursor = cursorGO.AddComponent<GridCursor>();
            WireGridCursorReferences(cursor, mapRenderer, assets, spriteRenderer, highlighter, pathArrow, unitMover);

            Undo.RegisterCreatedObjectUndo(cursorGO, "Create GridCursor");
        }

        private static void WireGridCursorReferences(GridCursor cursor, MapRenderer mapRenderer,
            SceneAssets assets, SpriteRenderer spriteRenderer,
            RangeHighlighter highlighter, PathArrowRenderer pathArrow, UnitMover unitMover)
        {
            var so = new SerializedObject(cursor);
            so.FindProperty("_mapRenderer").objectReferenceValue = mapRenderer;
            so.FindProperty("_terrainStatTable").objectReferenceValue = assets.terrainStatTable;
            so.FindProperty("_stateChangedChannel").objectReferenceValue = assets.stateChannel;
            so.FindProperty("_spriteRenderer").objectReferenceValue = spriteRenderer;
            so.FindProperty("_rangeHighlighter").objectReferenceValue = highlighter;
            so.FindProperty("_pathArrowRenderer").objectReferenceValue = pathArrow;
            so.FindProperty("_unitMover").objectReferenceValue = unitMover;
            so.ApplyModifiedPropertiesWithoutUndo();
        }

        private static void SetupTestUnit(Sprite unitSprite)
        {
            if (AlreadyExistsInScene<TestUnit>()) return;

            var unitGO = new GameObject("TestUnit");

            var unit = unitGO.AddComponent<TestUnit>();
            unit.gridPosition = new Vector2Int(2, 2);
            unit.movementPoints = 3;
            unit.movementType = MovementType.Foot;
            unit.attackRangeMin = 1;
            unit.attackRangeMax = 1;

            CreateChildSprite(unitGO, "UnitSprite", unitSprite, "Units", 0);

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

        private static bool AlreadyExistsInScene<T>() where T : Object
        {
            var existing = Object.FindAnyObjectByType<T>();
            if (existing == null) return false;

            Debug.Log($"CursorSceneSetup: {typeof(T).Name} already exists, skipping.");
            return true;
        }

        private static SpriteRenderer CreateChildSprite(GameObject parent, string childName,
            Sprite sprite, string sortingLayer, int sortingOrder)
        {
            var spriteGO = new GameObject(childName);
            spriteGO.transform.SetParent(parent.transform, false);

            var sr = spriteGO.AddComponent<SpriteRenderer>();
            sr.sprite = sprite;
            sr.sortingLayerName = sortingLayer;
            sr.sortingOrder = sortingOrder;
            return sr;
        }

        private static void MarkSceneDirty()
        {
            UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(
                UnityEditor.SceneManagement.EditorSceneManager.GetActiveScene());
        }
    }
}
