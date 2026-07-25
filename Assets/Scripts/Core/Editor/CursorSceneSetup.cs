using System.IO;
using UnityEditor;
using UnityEngine;
using ProjectAstra.Core;
using ProjectAstra.Core.Camera;
using ProjectAstra.Core.Combat;
using ProjectAstra.Core.Cursor;
using ProjectAstra.Core.Grid;
using ProjectAstra.Core.Pathfinding;
using ProjectAstra.Core.Progression;
using ProjectAstra.Core.State;
using ProjectAstra.Core.Turn;
using ProjectAstra.Core.UI;
using ProjectAstra.Core.UI.ActionMenu;
using ProjectAstra.Core.UI.BattleMap;
using ProjectAstra.Core.UI.Convoy;
using ProjectAstra.Core.UI.Forecast;
using ProjectAstra.Core.UI.Inventory;
using ProjectAstra.Core.UI.Overlays;
using ProjectAstra.Core.UI.Trade;
using ProjectAstra.Core.UI.UnitInfo;
using ProjectAstra.Core.UI.WarLedger;
using ProjectAstra.Core.Units;

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
            SetupInventoryUIBindings();
            SetupWarLedgerSubsystems();

            MarkSceneDirty();
            Debug.Log("GridCursor, TestUnits, TurnManager, PhaseBanner, CameraController, and Inventory UI added to scene.");
        }

        private struct SceneAssets
        {
            public TerrainStatTable terrainStatTable;
            public Sprite cursorSprite;
            public Sprite cursorIdle;
            public Sprite cursorSelected;
            public Sprite cursorTargeting;
            public Sprite unitSprite;
        }

        private static SceneAssets LoadRequiredAssets()
        {
            return new SceneAssets
            {
                terrainStatTable = AssetDatabase.LoadAssetAtPath<TerrainStatTable>(
                    "Assets/ScriptableObjects/Map/TerrainStatTable.asset"),
                cursorSprite = AssetDatabase.LoadAssetAtPath<Sprite>(
                    "Assets/Art/Cursor/TempleBracket_Idle.png")
                    ?? AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Art/Cursor/GridCursor.png"),
                cursorIdle = AssetDatabase.LoadAssetAtPath<Sprite>(
                    "Assets/Art/Cursor/TempleBracket_Idle.png"),
                cursorSelected = AssetDatabase.LoadAssetAtPath<Sprite>(
                    "Assets/Art/Cursor/TempleBracket_Selected.png"),
                cursorTargeting = AssetDatabase.LoadAssetAtPath<Sprite>(
                    "Assets/Art/Cursor/TempleBracket_Targeting.png"),
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
            so.FindProperty("mapRenderer").objectReferenceValue = mapRenderer;
            so.FindProperty("terrainStatTable").objectReferenceValue = assets.terrainStatTable;
            so.FindProperty("spriteRenderer").objectReferenceValue = spriteRenderer;
            so.FindProperty("idleSprite").objectReferenceValue = assets.cursorIdle;
            so.FindProperty("selectedSprite").objectReferenceValue = assets.cursorSelected;
            so.FindProperty("targetingSprite").objectReferenceValue = assets.cursorTargeting;
            so.FindProperty("rangeHighlighter").objectReferenceValue = highlighter;
            so.FindProperty("pathArrowRenderer").objectReferenceValue = pathArrow;
            so.FindProperty("unitMover").objectReferenceValue = unitMover;
            // Wire to the authored SelectionMenu prefab instance already placed in the Canvas.
            so.FindProperty("actionMenuUI").objectReferenceValue =
                UnityEngine.Object.FindAnyObjectByType<SelectionMenuView>(FindObjectsInactive.Include);
            so.ApplyModifiedPropertiesWithoutUndo();
        }

        private static void SetupTestUnit(Sprite unitSprite)
        {
            if (AlreadyExistsInScene<TestUnit>()) return;

            var arjun = AssetDatabase.LoadAssetAtPath<UnitDefinition>("Assets/ScriptableObjects/Units/Characters/Arjun.asset");
            var karna = AssetDatabase.LoadAssetAtPath<UnitDefinition>("Assets/ScriptableObjects/Units/Characters/Karna.asset");
            var priya = AssetDatabase.LoadAssetAtPath<UnitDefinition>("Assets/ScriptableObjects/Units/Characters/Priya.asset");

            var pegasusKnight = AssetDatabase.LoadAssetAtPath<ClassDefinition>("Assets/ScriptableObjects/Units/Classes/PegasusKnight.asset");

            CreateUnit("PlayerUnit1", unitSprite, new Vector2Int(2, 2), Faction.Player, 3, MovementType.Foot, unitDef: arjun);
            CreateUnit("PlayerUnit2", unitSprite, new Vector2Int(4, 3), Faction.Player, 4, MovementType.Foot, unitDef: karna);
            CreateUnit("PlayerUnit3", unitSprite, new Vector2Int(3, 1), Faction.Player, 5, MovementType.Mounted, unitDef: priya, classOverride: pegasusKnight);
            CreateUnit("EnemyUnit1",  unitSprite, new Vector2Int(6, 5), Faction.Enemy,  3, MovementType.Foot);
            CreateUnit("EnemyUnit2",  unitSprite, new Vector2Int(7, 4), Faction.Enemy,  4, MovementType.Armoured);
        }

        private static void CreateUnit(string name, Sprite sprite, Vector2Int pos, Faction faction,
            int movementPoints, MovementType movementType,
            UnitDefinition unitDef = null, ClassDefinition classOverride = null)
        {
            var unitGO = new GameObject(name);

            var unit = unitGO.AddComponent<TestUnit>();
            unit.faction = faction;
            unit.isLord = unitDef != null && unitDef.IsLord;
            unit.gridPosition = pos;
            unit.movementPoints = movementPoints;
            unit.movementType = movementType;
            unit.attackRangeMin = 1;
            unit.attackRangeMax = 1;

            if (unitDef != null || classOverride != null)
            {
                var so = new SerializedObject(unit);
                if (unitDef != null) so.FindProperty("unitDefinition").objectReferenceValue = unitDef;
                if (classOverride != null) so.FindProperty("classOverride").objectReferenceValue = classOverride;
                so.ApplyModifiedPropertiesWithoutUndo();
            }

            // RequireComponent on TestUnit auto-adds UnitInventory; ensure it's present.
            var inventory = unitGO.GetComponent<UnitInventory>() ?? unitGO.AddComponent<UnitInventory>();
            SeedInventory(inventory, faction, name);

            CreateChildSprite(unitGO, "UnitSprite", sprite, "Units", 0);

            unitGO.transform.position = new Vector3(pos.x + 0.5f, pos.y + 0.5f, 0f);

            Undo.RegisterCreatedObjectUndo(unitGO, $"Create {name}");
        }

        private static void SeedInventory(UnitInventory inventory, Faction faction, string unitName)
        {
            if (faction == Faction.Player)
            {
                if (unitName == "PlayerUnit3")
                {
                    inventory.SetSlot(0, ItemFromAsset("Heal"));
                    inventory.TryAddItem(ItemFromAsset("Mend"), out _);
                    inventory.TryAddItem(ItemFromAsset("Vulnerary"), out _);
                }
                else
                {
                    inventory.SetSlot(0, ItemFromAsset("IronSword"));
                }

                if (unitName == "PlayerUnit1")
                {
                    inventory.TryAddItem(ItemFromAsset("IronAxe"), out _);
                    inventory.TryAddItem(ItemFromAsset("Vulnerary"), out _);
                    inventory.TryAddItem(ItemFromAsset("ShaktiMudrika"), out _);
                }
                else if (unitName == "PlayerUnit2")
                {
                    inventory.TryAddItem(ItemFromAsset("Vulnerary"), out _);
                }
            }
            else
            {
                inventory.SetSlot(0, ItemFromAsset("IronLance"));
            }
        }

        // Loads a stock item asset by file name (Weapons or Consumables folder) and bakes its runtime
        // slot value. Editor-only scaffolding path for seeding test-scene inventories.
        private static InventoryItem ItemFromAsset(string assetName)
        {
            ItemDefinition item = LoadStockItem(assetName);
            if (item != null) return item.ToInventoryItem();
            Debug.LogWarning($"CursorSceneSetup: stock item asset '{assetName}' not found.");
            return InventoryItem.None;
        }

        private static ItemDefinition LoadStockItem(string assetName) =>
            AssetDatabase.LoadAssetAtPath<ItemDefinition>($"Assets/ScriptableObjects/Items/Weapons/{assetName}.asset")
            ?? AssetDatabase.LoadAssetAtPath<ItemDefinition>($"Assets/ScriptableObjects/Items/Consumables/{assetName}.asset");

        // Wires the starter convoy's items, mirroring the old hardcoded IronLance/Fire/Vulnerary set.
        private static void SeedConvoyStarter(ConvoyBootstrap bootstrap)
        {
            var items = new[] { LoadStockItem("IronLance"), LoadStockItem("Fire"), LoadStockItem("Vulnerary") };
            var so = new SerializedObject(bootstrap);
            var arr = so.FindProperty("starterItems");
            arr.arraySize = items.Length;
            for (int i = 0; i < items.Length; i++)
                arr.GetArrayElementAtIndex(i).objectReferenceValue = items[i];
            so.ApplyModifiedPropertiesWithoutUndo();
        }

        private static void SetupInventoryUIBindings()
        {
            var canvas = Object.FindAnyObjectByType<Canvas>();
            if (canvas == null)
            {
                Debug.LogWarning("CursorSceneSetup: No Canvas found; inventory UI components were not attached.");
                return;
            }

            var inventoryMenu = Object.FindAnyObjectByType<InventoryMenuUI>();
            if (inventoryMenu == null)
            {
                var go = new GameObject("InventoryMenuUI");
                go.transform.SetParent(canvas.transform, false);
                inventoryMenu = go.AddComponent<InventoryMenuUI>();
                Undo.RegisterCreatedObjectUndo(go, "Create InventoryMenuUI");
            }

            // Instantiate the Indigo Codex inventory popup into the scene once — same pattern
            // as UnitInfoPanel: live GameObject sits under the canvas, disabled by default,
            // Show/Hide flips SetActive instead of re-instantiating every open.
            var popupPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(
                "Assets/UI/InventoryPopup/InventoryPopup.prefab");
            GameObject popupInstance = null;
            var existingPopup = canvas.transform.Find("InventoryPopup");
            if (existingPopup != null) popupInstance = existingPopup.gameObject;
            if (popupInstance == null && popupPrefab != null)
            {
                popupInstance = (GameObject)PrefabUtility.InstantiatePrefab(popupPrefab, canvas.transform);
                popupInstance.name = "InventoryPopup";
                popupInstance.SetActive(false);
                Undo.RegisterCreatedObjectUndo(popupInstance, "Create InventoryPopup instance");
            }

            if (popupInstance != null)
            {
                var so = new SerializedObject(inventoryMenu);
                var prop = so.FindProperty("popupInstance");
                if (prop != null && prop.objectReferenceValue != popupInstance)
                {
                    prop.objectReferenceValue = popupInstance;
                    so.ApplyModifiedPropertiesWithoutUndo();
                }
            }
            else if (popupPrefab == null)
            {
                Debug.LogWarning("CursorSceneSetup: InventoryPopup prefab missing — run " +
                    "'Project Astra/Build Inventory Popup (prefab)' to generate it.");
            }

            var confirmDialog = Object.FindAnyObjectByType<ConfirmDialogUI>();
            if (confirmDialog == null)
            {
                var go = new GameObject("ConfirmDialogUI");
                go.transform.SetParent(canvas.transform, false);
                confirmDialog = go.AddComponent<ConfirmDialogUI>();
                Undo.RegisterCreatedObjectUndo(go, "Create ConfirmDialogUI");
            }

            var toast = Object.FindAnyObjectByType<ToastNotificationUI>();
            if (toast == null)
            {
                var go = new GameObject("ToastNotificationUI");
                go.transform.SetParent(canvas.transform, false);
                toast = go.AddComponent<ToastNotificationUI>();
                Undo.RegisterCreatedObjectUndo(go, "Create ToastNotificationUI");
            }

            var fullPrompt = Object.FindAnyObjectByType<InventoryFullPromptUI>();
            if (fullPrompt == null)
            {
                var go = new GameObject("InventoryFullPromptUI");
                go.transform.SetParent(canvas.transform, false);
                fullPrompt = go.AddComponent<InventoryFullPromptUI>();
                Undo.RegisterCreatedObjectUndo(go, "Create InventoryFullPromptUI");
            }
            InventoryAcquisition.PromptHandler = fullPrompt;

            // TradeScreenUI lives directly on the (inactive) Canvas/TradeScreen prefab instance
            // that starts SetActive(false) so Show() can toggle it on. Plain
            // FindAnyObjectByType<T>() defaults to FindObjectsInactive.Exclude and returns null
            // for inactive GameObjects, which previously clobbered _tradeUI → null on every
            // setup run and killed the Trade action silently. Include inactive so it's found.
            var tradeUI = Object.FindAnyObjectByType<TradeScreenUI>(FindObjectsInactive.Include);

            var convoyUI = Object.FindAnyObjectByType<ConvoyUI>();
            if (convoyUI == null)
            {
                var go = new GameObject("ConvoyUI");
                go.transform.SetParent(canvas.transform, false);
                convoyUI = go.AddComponent<ConvoyUI>();
                Undo.RegisterCreatedObjectUndo(go, "Create ConvoyUI");
            }

            // Instantiate the Supply Convoy popup prefab into the scene once (same pattern as
            // UnitInfoPanel / InventoryPopup). ConvoyUI.Show() just toggles SetActive.
            var supplyPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(
                "Assets/UI/SupplyConvoy/SupplyConvoy.prefab");
            GameObject supplyInstance = null;
            var existingSupply = canvas.transform.Find("SupplyConvoy");
            if (existingSupply != null) supplyInstance = existingSupply.gameObject;
            if (supplyInstance == null && supplyPrefab != null)
            {
                supplyInstance = (GameObject)PrefabUtility.InstantiatePrefab(supplyPrefab, canvas.transform);
                supplyInstance.name = "SupplyConvoy";
                supplyInstance.SetActive(false);
                Undo.RegisterCreatedObjectUndo(supplyInstance, "Create SupplyConvoy instance");
            }
            if (supplyInstance != null)
            {
                var so = new SerializedObject(convoyUI);
                var prop = so.FindProperty("popupInstance");
                if (prop != null && prop.objectReferenceValue != supplyInstance)
                {
                    prop.objectReferenceValue = supplyInstance;
                    so.ApplyModifiedPropertiesWithoutUndo();
                }
            }
            else if (supplyPrefab == null)
            {
                Debug.LogWarning("CursorSceneSetup: SupplyConvoy prefab missing — run " +
                    "'Project Astra/Build Supply Convoy (prefab)' to generate it.");
            }

            // Combat Forecast — the screen is hand-authored in the scene; just resolve the
            // controller so GridCursor can be wired to it below.
            var forecastCtrl = Object.FindAnyObjectByType<CombatForecastUIController>(FindObjectsInactive.Include);

            // War's Ledger — persistent prefab instance driven by GameState.WarLedger.
            var ledgerUI = Object.FindAnyObjectByType<WarLedgerUI>();
            if (ledgerUI == null)
            {
                var lgo = new GameObject("WarLedgerUI");
                lgo.transform.SetParent(canvas.transform, false);
                ledgerUI = lgo.AddComponent<WarLedgerUI>();
                Undo.RegisterCreatedObjectUndo(lgo, "Create WarLedgerUI");
            }

            var ledgerPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(
                "Assets/UI/WarLedger/WarLedger.prefab");
            GameObject ledgerInstance = null;
            var existingLedger = canvas.transform.Find("WarLedger");
            if (existingLedger != null) ledgerInstance = existingLedger.gameObject;
            if (ledgerInstance == null && ledgerPrefab != null)
            {
                ledgerInstance = (GameObject)PrefabUtility.InstantiatePrefab(ledgerPrefab, canvas.transform);
                ledgerInstance.name = "WarLedger";
                ledgerInstance.SetActive(false);
                Undo.RegisterCreatedObjectUndo(ledgerInstance, "Create WarLedger instance");
            }
            if (ledgerInstance != null)
            {
                var so = new SerializedObject(ledgerUI);
                var prop = so.FindProperty("popupInstance");
                if (prop != null && prop.objectReferenceValue != ledgerInstance)
                {
                    prop.objectReferenceValue = ledgerInstance;
                    so.ApplyModifiedPropertiesWithoutUndo();
                }
                // GameState channel is now reached via EventService, not wired here.
            }
            else if (ledgerPrefab == null)
            {
                Debug.LogWarning("CursorSceneSetup: WarLedger prefab missing — run " +
                    "'Project Astra/Build War Ledger (prefab)' to generate it.");
            }

            // Ensure ConvoyBootstrap exists so Convoy.Current is initialized at runtime.
            if (Object.FindAnyObjectByType<ConvoyBootstrap>() == null)
            {
                var bootstrapGo = new GameObject("ConvoyBootstrap");
                var bootstrap = bootstrapGo.AddComponent<ConvoyBootstrap>();
                SeedConvoyStarter(bootstrap);
                Undo.RegisterCreatedObjectUndo(bootstrapGo, "Create ConvoyBootstrap");
            }

            var cursor = Object.FindAnyObjectByType<GridCursor>();
            if (cursor != null)
            {
                var so = new SerializedObject(cursor);
                so.FindProperty("inventoryMenuUI").objectReferenceValue = inventoryMenu;
                so.FindProperty("confirmDialogUI").objectReferenceValue = confirmDialog;
                so.FindProperty("toastUI").objectReferenceValue = toast;
                // Only overwrite _tradeUI if we actually found one — never clobber a wired
                // reference with null, since the Trade Screen Build menu item sometimes
                // wires it out-of-band and we want that wiring to survive.
                if (tradeUI != null)
                    so.FindProperty("tradeUI").objectReferenceValue = tradeUI;
                so.FindProperty("convoyUI").objectReferenceValue = convoyUI;
                var forecastProp = so.FindProperty("combatForecastUI");
                if (forecastProp != null) forecastProp.objectReferenceValue = forecastCtrl;
                so.ApplyModifiedPropertiesWithoutUndo();
            }
        }

        // ==============================================================
        // UM-01 War's Ledger — subsystem wiring
        // ==============================================================

        private static void SetupWarLedgerSubsystems()
        {
            // The death channel now comes from EventService — these components just need to exist.
            EnsureComponent<DeathRegistry>("DeathRegistry", null);
            EnsureComponent<CommitmentTracker>("CommitmentTracker", null);
            EnsureComponent<ChapterMeta>("ChapterMeta", null);
            EnsureComponent<BattleVictoryWatcher>("BattleVictoryWatcher", null);
        }

        private static T EnsureComponent<T>(string goName, System.Action<T> configure)
            where T : MonoBehaviour
        {
            var existing = Object.FindAnyObjectByType<T>(FindObjectsInactive.Include);
            if (existing != null) return existing;

            var go = new GameObject(goName);
            var c = go.AddComponent<T>();
            configure?.Invoke(c);
            Undo.RegisterCreatedObjectUndo(go, $"Create {goName}");
            return c;
        }

        private static void SetupTurnManager(SceneAssets assets)
        {
            if (AlreadyExistsInScene<TurnManager>()) return;

            var go = new GameObject("TurnManager");
            var tm = go.AddComponent<TurnManager>();

            var so = new SerializedObject(tm);
            so.FindProperty("hasAllies").boolValue = false;
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
            bannerGO.AddComponent<PhaseBannerUI>();
            // PhaseBannerUI reads its Turn channel via EventService — no wiring needed here.

            Undo.RegisterCreatedObjectUndo(bannerGO, "Create PhaseBannerUI");
        }

        private static void SetupCameraController(MapRenderer mapRenderer)
        {
            var cam = Object.FindAnyObjectByType<UnityEngine.Camera>();
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
            so.FindProperty("gridCursor").objectReferenceValue = gridCursor;
            so.FindProperty("mapRenderer").objectReferenceValue = mapRenderer;
            so.FindProperty("deadzoneMarginTiles").intValue = 3;
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
