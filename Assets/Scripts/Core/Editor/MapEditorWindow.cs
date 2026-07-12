using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;
using ProjectAstra.Core.Grid;
using ProjectAstra.Core.Units;

namespace ProjectAstra.Core.Editor
{
    // Visual authoring tool for battle maps. A designer imports a seamless PNG (which sets the
    // map size), paints terrain per cell over the art, drops units and interactive objects, and
    // saves a MapData that plugs straight into the campaign — no code, no tile ids.
    public class MapEditorWindow : EditorWindow
    {
        private enum Tool { Terrain, Units, Objects }

        private const int CellPixels = 32;

        private MapData target;
        private SerializedObject so;

        private Tool tool = Tool.Terrain;
        private TerrainType brushTerrain = TerrainType.Plain;
        private string[] unitIds = new string[0];
        private int unitIdIndex;
        private int unitTeam;
        private string objectId = "";
        private Sprite objectSprite;

        private Vector2 scroll;

        [MenuItem("Project Astra/Map/Map Editor")]
        public static void Open()
        {
            var window = GetWindow<MapEditorWindow>("Map Editor");
            window.minSize = new Vector2(520, 640);
        }

        private void OnGUI()
        {
            DrawTargetSelector();
            if (target == null)
            {
                EditorGUILayout.HelpBox("Assign a MapData to edit, or create a new one.", MessageType.Info);
                return;
            }

            so.Update();
            scroll = EditorGUILayout.BeginScrollView(scroll);
            DrawIdentityFields();
            DrawBaseArtSection();
            DrawToolbar();
            DrawPalette();
            DrawGridCanvas();
            DrawValidation();
            DrawActions();
            EditorGUILayout.EndScrollView();
            so.ApplyModifiedProperties();
        }

        // --- Target -----------------------------------------------------

        private void DrawTargetSelector()
        {
            EditorGUILayout.BeginHorizontal();
            var picked = (MapData)EditorGUILayout.ObjectField("Map", target, typeof(MapData), false);
            if (picked != target) SetTarget(picked);
            if (GUILayout.Button("New…", GUILayout.Width(60))) CreateNewMap();
            EditorGUILayout.EndHorizontal();
        }

        private void SetTarget(MapData map)
        {
            target = map;
            so = map != null ? new SerializedObject(map) : null;
            unitIds = LoadUnitIds();
            unitIdIndex = 0;
        }

        private void CreateNewMap()
        {
            string path = EditorUtility.SaveFilePanelInProject("New Map", "NewMap", "asset",
                "Where to save the new MapData", "Assets/ScriptableObjects/Map/Maps");
            if (string.IsNullOrEmpty(path)) return;

            var map = CreateInstance<MapData>();
            AssetDatabase.CreateAsset(map, path);
            AssetDatabase.SaveAssets();
            SetTarget(map);
        }

        // --- Identity ---------------------------------------------------

        private void DrawIdentityFields()
        {
            EditorGUILayout.LabelField("Identity", EditorStyles.boldLabel);
            EditorGUILayout.PropertyField(so.FindProperty("_mapName"), new GUIContent("Name"));
            EditorGUILayout.PropertyField(so.FindProperty("mapId"), new GUIContent("Map Id"));
        }

        // --- Base art ---------------------------------------------------

        private void DrawBaseArtSection()
        {
            EditorGUILayout.Space(6);
            EditorGUILayout.LabelField("Base Art", EditorStyles.boldLabel);

            var artProp = so.FindProperty("baseArt");
            EditorGUILayout.PropertyField(artProp, new GUIContent("Sprite"));
            EditorGUILayout.LabelField("Size", $"{so.FindProperty("_width").intValue} x {so.FindProperty("_height").intValue} cells");

            EditorGUILayout.BeginHorizontal();
            if (GUILayout.Button("Import PNG…")) ImportPng();
            if (GUILayout.Button("Fit Size To Art")) SyncSizeToArt();
            EditorGUILayout.EndHorizontal();
        }

        private void ImportPng()
        {
            string source = EditorUtility.OpenFilePanel("Import map PNG", "", "png");
            if (string.IsNullOrEmpty(source)) return;

            string folder = "Assets/Art/Maps";
            if (!AssetDatabase.IsValidFolder(folder)) AssetDatabase.CreateFolder("Assets/Art", "Maps");
            string dest = $"{folder}/{Path.GetFileName(source)}";
            File.Copy(source, Path.Combine(Application.dataPath, "..", dest), true);
            AssetDatabase.Refresh();
            ConfigureImporter(dest);

            so.FindProperty("baseArt").objectReferenceValue = AssetDatabase.LoadAssetAtPath<Sprite>(dest);
            so.ApplyModifiedProperties();
            SyncSizeToArt();
        }

        private static void ConfigureImporter(string path)
        {
            var importer = (TextureImporter)AssetImporter.GetAtPath(path);
            importer.textureType = TextureImporterType.Sprite;
            importer.spriteImportMode = SpriteImportMode.Single;
            importer.spritePixelsPerUnit = CellPixels;
            importer.filterMode = FilterMode.Point;
            importer.textureCompression = TextureImporterCompression.Uncompressed;
            importer.mipmapEnabled = false;
            var settings = new TextureImporterSettings();
            importer.ReadTextureSettings(settings);
            settings.spriteAlignment = (int)SpriteAlignment.BottomLeft;
            importer.SetTextureSettings(settings);
            importer.SaveAndReimport();
        }

        // Derives cell dimensions from the assigned sprite and resizes the terrain grid to match.
        private void SyncSizeToArt()
        {
            var sprite = so.FindProperty("baseArt").objectReferenceValue as Sprite;
            if (sprite == null)
            {
                EditorUtility.DisplayDialog("No art", "Assign a base sprite first.", "OK");
                return;
            }

            int pxW = (int)sprite.rect.width, pxH = (int)sprite.rect.height;
            if (pxW % CellPixels != 0 || pxH % CellPixels != 0)
            {
                EditorUtility.DisplayDialog("Bad size",
                    $"Art is {pxW}x{pxH}px — both must be multiples of {CellPixels}.", "OK");
                return;
            }

            so.FindProperty("_width").intValue = pxW / CellPixels;
            so.FindProperty("_height").intValue = pxH / CellPixels;
            ResizeTerrainToGrid();
            so.ApplyModifiedProperties();
        }

        private void ResizeTerrainToGrid()
        {
            int cells = so.FindProperty("_width").intValue * so.FindProperty("_height").intValue;
            so.FindProperty("terrain").arraySize = cells;
        }

        // --- Toolbar + palettes ----------------------------------------

        private void DrawToolbar()
        {
            EditorGUILayout.Space(6);
            tool = (Tool)GUILayout.Toolbar((int)tool, new[] { "Terrain", "Units", "Objects" });
        }

        private void DrawPalette()
        {
            switch (tool)
            {
                case Tool.Terrain: DrawTerrainPalette(); break;
                case Tool.Units: DrawUnitPalette(); break;
                case Tool.Objects: DrawObjectPalette(); break;
            }
        }

        private void DrawTerrainPalette()
        {
            EditorGUILayout.LabelField("Brush", brushTerrain.ToString());
            var terrains = (TerrainType[])System.Enum.GetValues(typeof(TerrainType));
            int perRow = 6;
            for (int i = 0; i < terrains.Length; i++)
            {
                if (i % perRow == 0) EditorGUILayout.BeginHorizontal();
                DrawTerrainSwatch(terrains[i]);
                if (i % perRow == perRow - 1 || i == terrains.Length - 1) EditorGUILayout.EndHorizontal();
            }
        }

        private void DrawTerrainSwatch(TerrainType terrain)
        {
            var prev = GUI.backgroundColor;
            GUI.backgroundColor = ColorFor(terrain);
            bool selected = brushTerrain == terrain;
            if (GUILayout.Toggle(selected, terrain.ToString(), "Button", GUILayout.Height(22)) && !selected)
                brushTerrain = terrain;
            GUI.backgroundColor = prev;
        }

        private void DrawUnitPalette()
        {
            if (unitIds.Length == 0)
            {
                EditorGUILayout.HelpBox("No UnitDatabase found, or it has no units.", MessageType.Warning);
                return;
            }
            unitIdIndex = EditorGUILayout.Popup("Unit", Mathf.Clamp(unitIdIndex, 0, unitIds.Length - 1), unitIds);
            unitTeam = EditorGUILayout.IntPopup("Team", unitTeam,
                new[] { "Player (0)", "Enemy (1)", "Allied (2)" }, new[] { 0, 1, 2 });
            EditorGUILayout.HelpBox("Click a cell to place; click a placed unit to remove it.", MessageType.None);
        }

        private void DrawObjectPalette()
        {
            objectId = EditorGUILayout.TextField("Object Id", objectId);
            objectSprite = (Sprite)EditorGUILayout.ObjectField("Sprite", objectSprite, typeof(Sprite), false);
            EditorGUILayout.HelpBox("Click a cell to place; click a placed object to remove it.", MessageType.None);
        }

        // --- Grid canvas ------------------------------------------------

        private void DrawGridCanvas()
        {
            int w = so.FindProperty("_width").intValue;
            int h = so.FindProperty("_height").intValue;
            if (w <= 0 || h <= 0) return;

            float scale = Mathf.Clamp(EditorGUIUtility.currentViewWidth - 40f, 64f, 1024f) / w;
            scale = Mathf.Min(scale, 28f);
            var rect = GUILayoutUtility.GetRect(w * scale, h * scale, GUILayout.ExpandWidth(false));

            DrawArt(rect);
            DrawTerrainOverlay(rect, w, h, scale);
            DrawGridLines(rect, w, h, scale);
            DrawUnitMarkers(rect, h, scale);
            DrawObjectMarkers(rect, h, scale);
            HandleCanvasInput(rect, w, h, scale);
        }

        private void DrawArt(Rect rect)
        {
            var sprite = so.FindProperty("baseArt").objectReferenceValue as Sprite;
            if (sprite != null && sprite.texture != null)
                GUI.DrawTexture(rect, sprite.texture, ScaleMode.StretchToFill);
            else
                EditorGUI.DrawRect(rect, new Color(0.15f, 0.15f, 0.15f));
        }

        // Terrain colours only render in Terrain mode, so the art stays visible while placing units.
        private void DrawTerrainOverlay(Rect rect, int w, int h, float scale)
        {
            if (tool != Tool.Terrain) return;
            var terrain = so.FindProperty("terrain");
            for (int y = 0; y < h; y++)
                for (int x = 0; x < w; x++)
                {
                    int index = y * w + x;
                    if (index >= terrain.arraySize) continue;
                    var c = ColorFor((TerrainType)terrain.GetArrayElementAtIndex(index).enumValueIndex);
                    c.a = 0.5f;
                    EditorGUI.DrawRect(CellRect(rect, x, y, h, scale), c);
                }
        }

        private static void DrawGridLines(Rect rect, int w, int h, float scale)
        {
            var line = new Color(0f, 0f, 0f, 0.25f);
            for (int x = 0; x <= w; x++)
                EditorGUI.DrawRect(new Rect(rect.x + x * scale, rect.y, 1, h * scale), line);
            for (int y = 0; y <= h; y++)
                EditorGUI.DrawRect(new Rect(rect.x, rect.y + y * scale, w * scale, 1), line);
        }

        private void DrawUnitMarkers(Rect rect, int h, float scale)
        {
            var units = so.FindProperty("_unitStartPositions");
            for (int i = 0; i < units.arraySize; i++)
            {
                var e = units.GetArrayElementAtIndex(i);
                var pos = e.FindPropertyRelative("position").vector2IntValue;
                var cell = CellRect(rect, pos.x, pos.y, h, scale);
                EditorGUI.DrawRect(Inset(cell, scale * 0.25f), TeamColor(e.FindPropertyRelative("team").intValue));
            }
        }

        private void DrawObjectMarkers(Rect rect, int h, float scale)
        {
            var objects = so.FindProperty("objects");
            for (int i = 0; i < objects.arraySize; i++)
            {
                var pos = objects.GetArrayElementAtIndex(i).FindPropertyRelative("position").vector2IntValue;
                var cell = CellRect(rect, pos.x, pos.y, h, scale);
                EditorGUI.DrawRect(Inset(cell, scale * 0.32f), new Color(1f, 0.9f, 0.2f, 0.9f));
            }
        }

        private void HandleCanvasInput(Rect rect, int w, int h, float scale)
        {
            var e = Event.current;
            bool paint = e.type == EventType.MouseDown || (e.type == EventType.MouseDrag && tool == Tool.Terrain);
            if (!paint || e.button != 0 || !rect.Contains(e.mousePosition)) return;

            int x = Mathf.FloorToInt((e.mousePosition.x - rect.x) / scale);
            int y = h - 1 - Mathf.FloorToInt((e.mousePosition.y - rect.y) / scale);
            if (x < 0 || x >= w || y < 0 || y >= h) return;

            ApplyToolAt(new Vector2Int(x, y), w);
            e.Use();
            Repaint();
        }

        private void ApplyToolAt(Vector2Int cell, int w)
        {
            switch (tool)
            {
                case Tool.Terrain: PaintTerrain(cell, w); break;
                case Tool.Units: ToggleUnit(cell); break;
                case Tool.Objects: ToggleObject(cell); break;
            }
        }

        private void PaintTerrain(Vector2Int cell, int w)
        {
            var terrain = so.FindProperty("terrain");
            int index = cell.y * w + cell.x;
            if (index < terrain.arraySize)
                terrain.GetArrayElementAtIndex(index).enumValueIndex = (int)brushTerrain;
        }

        private void ToggleUnit(Vector2Int cell)
        {
            var units = so.FindProperty("_unitStartPositions");
            int existing = IndexAt(units, cell);
            if (existing >= 0) { units.DeleteArrayElementAtIndex(existing); return; }
            if (unitIds.Length == 0) return;

            units.arraySize++;
            var e = units.GetArrayElementAtIndex(units.arraySize - 1);
            e.FindPropertyRelative("position").vector2IntValue = cell;
            e.FindPropertyRelative("unitId").stringValue = unitIds[unitIdIndex];
            e.FindPropertyRelative("team").intValue = unitTeam;
            e.FindPropertyRelative("loadoutOverride").objectReferenceValue = null;
        }

        private void ToggleObject(Vector2Int cell)
        {
            var objects = so.FindProperty("objects");
            int existing = IndexAt(objects, cell);
            if (existing >= 0) { objects.DeleteArrayElementAtIndex(existing); return; }

            objects.arraySize++;
            var e = objects.GetArrayElementAtIndex(objects.arraySize - 1);
            e.FindPropertyRelative("position").vector2IntValue = cell;
            e.FindPropertyRelative("objectId").stringValue = objectId;
            e.FindPropertyRelative("sprite").objectReferenceValue = objectSprite;
            e.FindPropertyRelative("overridesTerrain").boolValue = false;
        }

        private static int IndexAt(SerializedProperty array, Vector2Int cell)
        {
            for (int i = 0; i < array.arraySize; i++)
                if (array.GetArrayElementAtIndex(i).FindPropertyRelative("position").vector2IntValue == cell)
                    return i;
            return -1;
        }

        // --- Validation + actions --------------------------------------

        private void DrawValidation()
        {
            EditorGUILayout.Space(6);
            EditorGUILayout.LabelField("Validation", EditorStyles.boldLabel);
            foreach (string issue in Validate())
                EditorGUILayout.HelpBox(issue, MessageType.Warning);
            if (Validate().Count == 0)
                EditorGUILayout.HelpBox("Ready to play.", MessageType.Info);
        }

        private List<string> Validate()
        {
            var issues = new List<string>();
            int w = so.FindProperty("_width").intValue, h = so.FindProperty("_height").intValue;

            if (string.IsNullOrEmpty(so.FindProperty("mapId").stringValue)) issues.Add("Map Id is empty.");
            if (so.FindProperty("baseArt").objectReferenceValue == null) issues.Add("No base art assigned.");
            if (so.FindProperty("terrain").arraySize != w * h)
                issues.Add($"Terrain has {so.FindProperty("terrain").arraySize} cells, expected {w * h}.");

            ValidateUnits(issues, w, h);
            if (!IsRegistered()) issues.Add("Not registered in a MapCatalog (click Register).");
            return issues;
        }

        private void ValidateUnits(List<string> issues, int w, int h)
        {
            var units = so.FindProperty("_unitStartPositions");
            var database = LoadUnitDatabase();
            var seen = new HashSet<Vector2Int>();
            for (int i = 0; i < units.arraySize; i++)
            {
                var e = units.GetArrayElementAtIndex(i);
                var pos = e.FindPropertyRelative("position").vector2IntValue;
                string id = e.FindPropertyRelative("unitId").stringValue;
                if (pos.x < 0 || pos.x >= w || pos.y < 0 || pos.y >= h) issues.Add($"Unit '{id}' is off the map at {pos}.");
                if (!seen.Add(pos)) issues.Add($"Two units share cell {pos}.");
                if (database != null && !database.TryResolve(id, out _)) issues.Add($"Unit id '{id}' is not in the UnitDatabase.");
            }
        }

        private void DrawActions()
        {
            EditorGUILayout.Space(6);
            EditorGUILayout.BeginHorizontal();
            if (GUILayout.Button("Save")) SaveTarget();
            if (GUILayout.Button("Register In Catalog")) RegisterInCatalog();
            EditorGUILayout.EndHorizontal();
        }

        private void SaveTarget()
        {
            so.ApplyModifiedProperties();
            EditorUtility.SetDirty(target);
            AssetDatabase.SaveAssets();
        }

        private void RegisterInCatalog()
        {
            var catalog = LoadMapCatalog();
            if (catalog == null) { EditorUtility.DisplayDialog("No catalog", "No MapCatalog asset found.", "OK"); return; }

            var cso = new SerializedObject(catalog);
            var maps = cso.FindProperty("_maps");
            if (IndexOfObject(maps, target) < 0)
            {
                maps.arraySize++;
                maps.GetArrayElementAtIndex(maps.arraySize - 1).objectReferenceValue = target;
                cso.ApplyModifiedProperties();
                EditorUtility.SetDirty(catalog);
                AssetDatabase.SaveAssets();
            }
        }

        private bool IsRegistered()
        {
            var catalog = LoadMapCatalog();
            if (catalog == null) return false;
            var maps = new SerializedObject(catalog).FindProperty("_maps");
            return IndexOfObject(maps, target) >= 0;
        }

        private static int IndexOfObject(SerializedProperty array, Object value)
        {
            for (int i = 0; i < array.arraySize; i++)
                if (array.GetArrayElementAtIndex(i).objectReferenceValue == value) return i;
            return -1;
        }

        // --- Asset lookups + helpers -----------------------------------

        private static UnitDatabase LoadUnitDatabase() => LoadFirst<UnitDatabase>();
        private static MapCatalog LoadMapCatalog() => LoadFirst<MapCatalog>();

        private static T LoadFirst<T>() where T : Object
        {
            foreach (string guid in AssetDatabase.FindAssets($"t:{typeof(T).Name}"))
                return AssetDatabase.LoadAssetAtPath<T>(AssetDatabase.GUIDToAssetPath(guid));
            return null;
        }

        private static string[] LoadUnitIds()
        {
            var database = LoadUnitDatabase();
            if (database == null) return new string[0];
            var ids = new List<string>();
            foreach (var unit in database.Units)
                if (unit != null && !string.IsNullOrEmpty(unit.UnitId)) ids.Add(unit.UnitId);
            return ids.ToArray();
        }

        private static Rect CellRect(Rect canvas, int x, int y, int h, float scale) =>
            new(canvas.x + x * scale, canvas.y + (h - 1 - y) * scale, scale, scale);

        private static Rect Inset(Rect r, float by) => new(r.x + by, r.y + by, r.width - 2 * by, r.height - 2 * by);

        private static Color TeamColor(int team) => team switch
        {
            1 => new Color(0.9f, 0.25f, 0.25f),
            2 => new Color(0.3f, 0.8f, 0.4f),
            _ => new Color(0.3f, 0.55f, 0.95f),
        };

        private static Color ColorFor(TerrainType t) => TerrainColors[(int)t];

        private static readonly Color[] TerrainColors =
        {
            new(0.13f, 0.55f, 0.13f), // Plain
            new(0.00f, 0.39f, 0.00f), // Forest
            new(0.55f, 0.54f, 0.54f), // Mountain
            new(0.41f, 0.41f, 0.41f), // Peak
            new(0.12f, 0.56f, 1.00f), // Water
            new(0.00f, 0.00f, 0.55f), // Sea
            new(0.39f, 0.58f, 0.93f), // River
            new(0.82f, 0.71f, 0.55f), // Road
            new(1.00f, 0.65f, 0.00f), // Village
            new(0.70f, 0.13f, 0.13f), // Fort
            new(0.55f, 0.27f, 0.07f), // Gate
            new(1.00f, 0.84f, 0.00f), // Chest
            new(0.63f, 0.32f, 0.18f), // Door
            new(0.31f, 0.31f, 0.31f), // Wall
            new(0.47f, 0.47f, 0.47f), // DestructibleWall
            new(0.66f, 0.66f, 0.66f), // Rubble
            new(0.93f, 0.79f, 0.69f), // Sand
            new(0.08f, 0.08f, 0.08f), // Void
            new(0.58f, 0.00f, 0.83f), // Throne
        };
    }
}
