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
    //
    // All editing happens on fast in-memory working state; the project is scanned once (not per
    // repaint) and written back only on Save, so typing and painting stay responsive.
    public class MapEditorWindow : EditorWindow
    {
        private enum Tool { Terrain, Units, Objects }

        private const int CellPixels = 32;

        private MapData target;

        // Working state — edited live, written back on Save.
        private string workName = "";
        private string workId = "";
        private Sprite workArt;
        private int width, height;
        private TerrainType[] terrain = new TerrainType[0];
        private List<UnitStartPosition> units = new();
        private List<MapObject> objects = new();
        private bool dirty;

        // Cached project lookups (refreshed on target change / after register), never per repaint.
        private MapCatalog catalog;
        private UnitDatabase unitDb;
        private string[] unitIds = new string[0];
        private bool registered;

        private Tool tool = Tool.Terrain;
        private TerrainType brushTerrain = TerrainType.Plain;
        private int unitIdIndex;
        private int unitTeam;
        private string objectId = "";
        private Sprite objectSprite;

        private float zoom = 44f;
        private Vector2 scroll;
        private GUIStyle cellLabelStyle;

        [MenuItem("Project Astra/Map/Map Editor")]
        public static void Open()
        {
            var window = GetWindow<MapEditorWindow>("Map Editor");
            window.minSize = new Vector2(720, 820);
        }

        private void OnGUI()
        {
            EnsureStyles();
            DrawTargetSelector();
            if (target == null)
            {
                EditorGUILayout.HelpBox("Assign a MapData to edit, or create a new one.", MessageType.Info);
                return;
            }

            scroll = EditorGUILayout.BeginScrollView(scroll);
            DrawIdentityFields();
            DrawBaseArtSection();
            DrawToolbar();
            DrawPalette();
            DrawView();
            var issues = Validate();
            DrawValidation(issues);
            DrawActions();
            DrawGridCanvas();
            EditorGUILayout.EndScrollView();
        }

        private void EnsureStyles()
        {
            if (cellLabelStyle != null) return;
            cellLabelStyle = new GUIStyle(EditorStyles.miniLabel)
            {
                alignment = TextAnchor.MiddleCenter,
                fontSize = 9,
                clipping = TextClipping.Clip,
            };
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
            if (map != null) LoadWorkingState();
            CacheProjectAssets();
        }

        private void LoadWorkingState()
        {
            workName = target.MapName ?? "";
            workId = target.MapId ?? "";
            workArt = target.BaseArt;
            width = Mathf.Max(target.Width, 0);
            height = Mathf.Max(target.Height, 0);

            terrain = new TerrainType[width * height];
            var src = target.Terrain;
            if (src != null)
                for (int i = 0; i < terrain.Length && i < src.Length; i++) terrain[i] = src[i];

            units = new List<UnitStartPosition>(target.UnitStartPositions ?? new UnitStartPosition[0]);
            objects = new List<MapObject>(target.Objects ?? new MapObject[0]);
            dirty = false;
        }

        private void CacheProjectAssets()
        {
            catalog = LoadFirst<MapCatalog>();
            unitDb = LoadFirst<UnitDatabase>();
            unitIds = BuildUnitIds();
            registered = ComputeRegistered();
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
            string name = EditorGUILayout.TextField("Name", workName);
            string id = EditorGUILayout.TextField("Map Id", workId);
            if (name != workName || id != workId) { workName = name; workId = id; dirty = true; }
        }

        // --- Base art ---------------------------------------------------

        private void DrawBaseArtSection()
        {
            EditorGUILayout.Space(6);
            EditorGUILayout.LabelField("Base Art", EditorStyles.boldLabel);

            var art = (Sprite)EditorGUILayout.ObjectField("Sprite", workArt, typeof(Sprite), false);
            if (art != workArt) { workArt = art; dirty = true; }
            EditorGUILayout.LabelField("Size", $"{width} x {height} cells");

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

            workArt = AssetDatabase.LoadAssetAtPath<Sprite>(dest);
            dirty = true;
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
            if (workArt == null)
            {
                EditorUtility.DisplayDialog("No art", "Assign a base sprite first.", "OK");
                return;
            }

            int pxW = (int)workArt.rect.width, pxH = (int)workArt.rect.height;
            if (pxW % CellPixels != 0 || pxH % CellPixels != 0)
            {
                EditorUtility.DisplayDialog("Bad size",
                    $"Art is {pxW}x{pxH}px — both must be multiples of {CellPixels}.", "OK");
                return;
            }

            ResizeGrid(pxW / CellPixels, pxH / CellPixels);
        }

        private void ResizeGrid(int newW, int newH)
        {
            var resized = new TerrainType[newW * newH];
            for (int y = 0; y < newH && y < height; y++)
                for (int x = 0; x < newW && x < width; x++)
                    resized[y * newW + x] = terrain[y * width + x];
            width = newW;
            height = newH;
            terrain = resized;
            dirty = true;
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
            const int perRow = 6;
            for (int i = 0; i < terrains.Length; i++)
            {
                if (i % perRow == 0) EditorGUILayout.BeginHorizontal();
                DrawTerrainSwatch(terrains[i]);
                if (i % perRow == perRow - 1 || i == terrains.Length - 1) EditorGUILayout.EndHorizontal();
            }
        }

        private void DrawTerrainSwatch(TerrainType t)
        {
            var prev = GUI.backgroundColor;
            GUI.backgroundColor = ColorFor(t);
            bool selected = brushTerrain == t;
            if (GUILayout.Toggle(selected, t.ToString(), "Button", GUILayout.Height(22)) && !selected)
                brushTerrain = t;
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

        private void DrawView()
        {
            EditorGUILayout.Space(4);
            zoom = EditorGUILayout.Slider("Zoom (px/cell)", zoom, 16f, 96f);
        }

        // --- Grid canvas ------------------------------------------------

        private void DrawGridCanvas()
        {
            if (width <= 0 || height <= 0) return;

            var rect = GUILayoutUtility.GetRect(width * zoom, height * zoom, GUILayout.ExpandWidth(false));
            DrawArt(rect);
            if (tool == Tool.Terrain) DrawTerrainOverlay(rect);
            DrawGridLines(rect);
            DrawUnitMarkers(rect);
            DrawObjectMarkers(rect);
            HandleCanvasInput(rect);
        }

        private void DrawArt(Rect rect)
        {
            if (workArt != null && workArt.texture != null)
                GUI.DrawTexture(rect, workArt.texture, ScaleMode.StretchToFill);
            else
                EditorGUI.DrawRect(rect, new Color(0.15f, 0.15f, 0.15f));
        }

        // Terrain colours + a short label per cell — only in Terrain mode so the art stays clear
        // while placing units. Labels are clipped to their own cell.
        private void DrawTerrainOverlay(Rect rect)
        {
            bool labels = zoom >= 22f;
            for (int y = 0; y < height; y++)
                for (int x = 0; x < width; x++)
                {
                    var cell = CellRect(rect, x, y);
                    var t = terrain[y * width + x];
                    var c = ColorFor(t);
                    c.a = 0.5f;
                    EditorGUI.DrawRect(cell, c);
                    if (labels) DrawCellLabel(cell, t);
                }
        }

        private void DrawCellLabel(Rect cell, TerrainType t)
        {
            cellLabelStyle.normal.textColor = Luminance(ColorFor(t)) > 0.5f ? Color.black : Color.white;
            GUI.Label(cell, TerrainAbbrev[(int)t], cellLabelStyle);
        }

        private void DrawGridLines(Rect rect)
        {
            var line = new Color(0f, 0f, 0f, 0.25f);
            for (int x = 0; x <= width; x++)
                EditorGUI.DrawRect(new Rect(rect.x + x * zoom, rect.y, 1, height * zoom), line);
            for (int y = 0; y <= height; y++)
                EditorGUI.DrawRect(new Rect(rect.x, rect.y + y * zoom, width * zoom, 1), line);
        }

        private void DrawUnitMarkers(Rect rect)
        {
            foreach (var u in units)
                EditorGUI.DrawRect(Inset(CellRect(rect, u.position.x, u.position.y), zoom * 0.25f), TeamColor(u.team));
        }

        private void DrawObjectMarkers(Rect rect)
        {
            foreach (var o in objects)
                EditorGUI.DrawRect(Inset(CellRect(rect, o.position.x, o.position.y), zoom * 0.32f), new Color(1f, 0.9f, 0.2f, 0.9f));
        }

        // Proper hot-control capture so a single click paints, and drag keeps painting.
        private void HandleCanvasInput(Rect rect)
        {
            int id = GUIUtility.GetControlID(FocusType.Passive);
            var e = Event.current;
            switch (e.GetTypeForControl(id))
            {
                case EventType.MouseDown:
                    if (e.button == 0 && rect.Contains(e.mousePosition))
                    {
                        GUIUtility.hotControl = id;
                        GUIUtility.keyboardControl = 0;
                        ApplyToolAt(CellUnder(rect, e.mousePosition));
                        e.Use();
                    }
                    break;
                case EventType.MouseDrag:
                    if (GUIUtility.hotControl == id && tool == Tool.Terrain && rect.Contains(e.mousePosition))
                    {
                        ApplyToolAt(CellUnder(rect, e.mousePosition));
                        e.Use();
                    }
                    break;
                case EventType.MouseUp:
                    if (GUIUtility.hotControl == id) { GUIUtility.hotControl = 0; e.Use(); }
                    break;
            }
        }

        private Vector2Int CellUnder(Rect rect, Vector2 mouse)
        {
            int x = Mathf.Clamp(Mathf.FloorToInt((mouse.x - rect.x) / zoom), 0, width - 1);
            int y = Mathf.Clamp(height - 1 - Mathf.FloorToInt((mouse.y - rect.y) / zoom), 0, height - 1);
            return new Vector2Int(x, y);
        }

        private void ApplyToolAt(Vector2Int cell)
        {
            switch (tool)
            {
                case Tool.Terrain: PaintTerrain(cell); break;
                case Tool.Units: ToggleUnit(cell); break;
                case Tool.Objects: ToggleObject(cell); break;
            }
            dirty = true;
            Repaint();
        }

        private void PaintTerrain(Vector2Int cell)
        {
            int index = cell.y * width + cell.x;
            if (index >= 0 && index < terrain.Length) terrain[index] = brushTerrain;
        }

        private void ToggleUnit(Vector2Int cell)
        {
            int existing = units.FindIndex(u => u.position == cell);
            if (existing >= 0) { units.RemoveAt(existing); return; }
            if (unitIds.Length == 0) return;
            units.Add(new UnitStartPosition { position = cell, unitId = unitIds[unitIdIndex], team = unitTeam });
        }

        private void ToggleObject(Vector2Int cell)
        {
            int existing = objects.FindIndex(o => o.position == cell);
            if (existing >= 0) { objects.RemoveAt(existing); return; }
            objects.Add(new MapObject { position = cell, objectId = objectId, sprite = objectSprite });
        }

        // --- Validation + actions --------------------------------------

        private List<string> Validate()
        {
            var issues = new List<string>();
            if (string.IsNullOrEmpty(workId)) issues.Add("Map Id is empty.");
            if (workArt == null) issues.Add("No base art assigned.");
            if (terrain.Length != width * height) issues.Add($"Terrain has {terrain.Length} cells, expected {width * height}.");

            var seen = new HashSet<Vector2Int>();
            foreach (var u in units)
            {
                if (u.position.x < 0 || u.position.x >= width || u.position.y < 0 || u.position.y >= height)
                    issues.Add($"Unit '{u.unitId}' is off the map at {u.position}.");
                if (!seen.Add(u.position)) issues.Add($"Two units share cell {u.position}.");
                if (unitDb != null && !unitDb.TryResolve(u.unitId, out _))
                    issues.Add($"Unit id '{u.unitId}' is not in the UnitDatabase.");
            }

            if (!registered) issues.Add("Not registered in a MapCatalog (click Register).");
            return issues;
        }

        private void DrawValidation(List<string> issues)
        {
            EditorGUILayout.Space(6);
            EditorGUILayout.LabelField("Validation", EditorStyles.boldLabel);
            foreach (string issue in issues) EditorGUILayout.HelpBox(issue, MessageType.Warning);
            if (issues.Count == 0) EditorGUILayout.HelpBox("Ready to play.", MessageType.Info);
        }

        private void DrawActions()
        {
            EditorGUILayout.Space(6);
            EditorGUILayout.BeginHorizontal();
            if (GUILayout.Button(dirty ? "Save *" : "Save")) SaveTarget();
            using (new EditorGUI.DisabledScope(catalog == null || registered))
                if (GUILayout.Button("Register In Catalog")) RegisterInCatalog();
            EditorGUILayout.EndHorizontal();
        }

        private void SaveTarget()
        {
            var so = new SerializedObject(target);
            so.FindProperty("mapName").stringValue = workName;
            so.FindProperty("mapId").stringValue = workId;
            so.FindProperty("baseArt").objectReferenceValue = workArt;
            so.FindProperty("width").intValue = width;
            so.FindProperty("height").intValue = height;
            WriteTerrain(so);
            WriteUnits(so);
            WriteObjects(so);
            so.ApplyModifiedProperties();

            EditorUtility.SetDirty(target);
            AssetDatabase.SaveAssets();
            dirty = false;
        }

        private void WriteTerrain(SerializedObject so)
        {
            var p = so.FindProperty("terrain");
            p.arraySize = terrain.Length;
            for (int i = 0; i < terrain.Length; i++)
                p.GetArrayElementAtIndex(i).enumValueIndex = (int)terrain[i];
        }

        private void WriteUnits(SerializedObject so)
        {
            var p = so.FindProperty("unitStartPositions");
            p.arraySize = units.Count;
            for (int i = 0; i < units.Count; i++)
            {
                var e = p.GetArrayElementAtIndex(i);
                e.FindPropertyRelative("position").vector2IntValue = units[i].position;
                e.FindPropertyRelative("unitId").stringValue = units[i].unitId;
                e.FindPropertyRelative("team").intValue = units[i].team;
                e.FindPropertyRelative("loadoutOverride").objectReferenceValue = units[i].loadoutOverride;
            }
        }

        private void WriteObjects(SerializedObject so)
        {
            var p = so.FindProperty("objects");
            p.arraySize = objects.Count;
            for (int i = 0; i < objects.Count; i++)
            {
                var e = p.GetArrayElementAtIndex(i);
                e.FindPropertyRelative("position").vector2IntValue = objects[i].position;
                e.FindPropertyRelative("sprite").objectReferenceValue = objects[i].sprite;
                e.FindPropertyRelative("objectId").stringValue = objects[i].objectId;
                e.FindPropertyRelative("overridesTerrain").boolValue = objects[i].overridesTerrain;
                e.FindPropertyRelative("terrainOverride").enumValueIndex = (int)objects[i].terrainOverride;
            }
        }

        private void RegisterInCatalog()
        {
            if (catalog == null) return;
            if (dirty) SaveTarget();

            var so = new SerializedObject(catalog);
            var maps = so.FindProperty("maps");
            maps.arraySize++;
            maps.GetArrayElementAtIndex(maps.arraySize - 1).objectReferenceValue = target;
            so.ApplyModifiedProperties();
            EditorUtility.SetDirty(catalog);
            AssetDatabase.SaveAssets();
            registered = true;
        }

        private bool ComputeRegistered()
        {
            if (catalog == null || target == null) return false;
            var maps = new SerializedObject(catalog).FindProperty("maps");
            for (int i = 0; i < maps.arraySize; i++)
                if (maps.GetArrayElementAtIndex(i).objectReferenceValue == target) return true;
            return false;
        }

        // --- Asset lookups + helpers -----------------------------------

        private string[] BuildUnitIds()
        {
            if (unitDb == null) return new string[0];
            var ids = new List<string>();
            foreach (var unit in unitDb.Units)
                if (unit != null && !string.IsNullOrEmpty(unit.UnitId)) ids.Add(unit.UnitId);
            return ids.ToArray();
        }

        private static T LoadFirst<T>() where T : Object
        {
            foreach (string guid in AssetDatabase.FindAssets($"t:{typeof(T).Name}"))
                return AssetDatabase.LoadAssetAtPath<T>(AssetDatabase.GUIDToAssetPath(guid));
            return null;
        }

        private Rect CellRect(Rect canvas, int x, int y) =>
            new(canvas.x + x * zoom, canvas.y + (height - 1 - y) * zoom, zoom, zoom);

        private static Rect Inset(Rect r, float by) => new(r.x + by, r.y + by, r.width - 2 * by, r.height - 2 * by);

        private static float Luminance(Color c) => 0.299f * c.r + 0.587f * c.g + 0.114f * c.b;

        private static Color TeamColor(int team) => team switch
        {
            1 => new Color(0.9f, 0.25f, 0.25f),
            2 => new Color(0.3f, 0.8f, 0.4f),
            _ => new Color(0.3f, 0.55f, 0.95f),
        };

        private static Color ColorFor(TerrainType t) => TerrainColors[(int)t];

        private static readonly string[] TerrainAbbrev =
        {
            "PLN", "FOR", "MTN", "PEK", "WTR", "SEA", "RIV", "ROD", "VIL", "FRT",
            "GAT", "CHS", "DOR", "WAL", "DWL", "RUB", "SND", "VOI", "THR",
        };

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
