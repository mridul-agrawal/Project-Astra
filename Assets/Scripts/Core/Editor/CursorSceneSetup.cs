using UnityEditor;
using UnityEngine;
using ProjectAstra.Core;
using ProjectAstra.Core.UI;

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
            SetupTurnManager(assets);
            SetupPhaseBanner(assets);
            SetupCameraController(mapRenderer);

            MarkSceneDirty();
            Debug.Log("GridCursor, TestUnits, TurnManager, PhaseBanner, and CameraController added to scene.");
        }

        private struct SceneAssets
        {
            public GameStateEventChannel stateChannel;
            public TurnEventChannel turnChannel;
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
                turnChannel = AssetDatabase.LoadAssetAtPath<TurnEventChannel>(
                    "Assets/ScriptableObjects/Core/TurnEventChannel.asset"),
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
            var actionMenu = cursorGO.AddComponent<UnitActionMenuUI>();

            var cursor = cursorGO.AddComponent<GridCursor>();
            WireGridCursorReferences(cursor, mapRenderer, assets, spriteRenderer, highlighter, pathArrow, unitMover, actionMenu);

            Undo.RegisterCreatedObjectUndo(cursorGO, "Create GridCursor");
        }

        private static void WireGridCursorReferences(GridCursor cursor, MapRenderer mapRenderer,
            SceneAssets assets, SpriteRenderer spriteRenderer,
            RangeHighlighter highlighter, PathArrowRenderer pathArrow, UnitMover unitMover, UnitActionMenuUI actionMenu)
        {
            var so = new SerializedObject(cursor);
            so.FindProperty("_mapRenderer").objectReferenceValue = mapRenderer;
            so.FindProperty("_terrainStatTable").objectReferenceValue = assets.terrainStatTable;
            so.FindProperty("_stateChangedChannel").objectReferenceValue = assets.stateChannel;
            so.FindProperty("_spriteRenderer").objectReferenceValue = spriteRenderer;
            so.FindProperty("_rangeHighlighter").objectReferenceValue = highlighter;
            so.FindProperty("_pathArrowRenderer").objectReferenceValue = pathArrow;
            so.FindProperty("_unitMover").objectReferenceValue = unitMover;
            so.FindProperty("_actionMenuUI").objectReferenceValue = actionMenu;
            so.ApplyModifiedPropertiesWithoutUndo();
        }

        private static void SetupTestUnit(Sprite unitSprite)
        {
            if (AlreadyExistsInScene<TestUnit>()) return;

            CreateUnit("PlayerUnit1", unitSprite, new Vector2Int(2, 2), Faction.Player, 3, MovementType.Foot);
            CreateUnit("PlayerUnit2", unitSprite, new Vector2Int(4, 3), Faction.Player, 4, MovementType.Foot);
            CreateUnit("PlayerUnit3", unitSprite, new Vector2Int(3, 1), Faction.Player, 5, MovementType.Mounted);
            CreateUnit("EnemyUnit1",  unitSprite, new Vector2Int(6, 5), Faction.Enemy,  3, MovementType.Foot);
            CreateUnit("EnemyUnit2",  unitSprite, new Vector2Int(7, 4), Faction.Enemy,  4, MovementType.Armoured);
        }

        private static void CreateUnit(string name, Sprite sprite, Vector2Int pos, Faction faction,
            int movementPoints, MovementType movementType)
        {
            var unitGO = new GameObject(name);

            var unit = unitGO.AddComponent<TestUnit>();
            unit.faction = faction;
            unit.gridPosition = pos;
            unit.movementPoints = movementPoints;
            unit.movementType = movementType;
            unit.attackRangeMin = 1;
            unit.attackRangeMax = 1;

            CreateChildSprite(unitGO, "UnitSprite", sprite, "Units", 0);

            unitGO.transform.position = new Vector3(pos.x + 0.5f, pos.y + 0.5f, 0f);

            Undo.RegisterCreatedObjectUndo(unitGO, $"Create {name}");
        }

        private static void SetupTurnManager(SceneAssets assets)
        {
            if (AlreadyExistsInScene<TurnManager>()) return;

            var go = new GameObject("TurnManager");
            var tm = go.AddComponent<TurnManager>();

            var so = new SerializedObject(tm);
            so.FindProperty("_turnEventChannel").objectReferenceValue = assets.turnChannel;
            so.FindProperty("_stateChangedChannel").objectReferenceValue = assets.stateChannel;
            so.FindProperty("_hasAllies").boolValue = false;
            so.ApplyModifiedPropertiesWithoutUndo();

            Undo.RegisterCreatedObjectUndo(go, "Create TurnManager");
        }

        private static void SetupPhaseBanner(SceneAssets assets)
        {
            if (AlreadyExistsInScene<PhaseBannerUI>()) return;

            var canvas = Object.FindAnyObjectByType<Canvas>();
            if (canvas == null)
            {
                var canvasGO = new GameObject("UICanvas");
                canvas = canvasGO.AddComponent<Canvas>();
                canvas.renderMode = RenderMode.ScreenSpaceOverlay;
                canvas.sortingOrder = 100;
                canvasGO.AddComponent<UnityEngine.UI.CanvasScaler>();
                canvasGO.AddComponent<UnityEngine.UI.GraphicRaycaster>();
                Undo.RegisterCreatedObjectUndo(canvasGO, "Create UICanvas");
            }

            var bannerGO = new GameObject("PhaseBannerUI");
            bannerGO.transform.SetParent(canvas.transform, false);
            var banner = bannerGO.AddComponent<PhaseBannerUI>();

            var so = new SerializedObject(banner);
            so.FindProperty("_turnEventChannel").objectReferenceValue = assets.turnChannel;
            so.ApplyModifiedPropertiesWithoutUndo();

            Undo.RegisterCreatedObjectUndo(bannerGO, "Create PhaseBannerUI");
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
