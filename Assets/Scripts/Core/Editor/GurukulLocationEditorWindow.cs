using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;
using ProjectAstra.Core.Gurukul;

namespace ProjectAstra.Core.Editor
{
    // Visual authoring tool for hub rooms. A designer imports a painted PNG (which sets the room
    // size), paints where you can walk, drags out the blocking areas of tall props, and saves a
    // GurukulLocationData that the hub loads straight away.
    //
    // Written fresh rather than sharing code with the Map Editor: that one paints a per-tile terrain
    // type for a battle map, this one paints half-tile walkability for free movement. The shape of
    // the tool is deliberately the same so a designer only learns it once.
    //
    // Editing happens on in-memory working state and is written back only on Save, so painting stays
    // responsive on a room several screens wide.
    public class GurukulLocationEditorWindow : EditorWindow
    {
        private enum Tool { Walkability, Props }
        private enum Brush { Block, Clear }

        private const int TilePixels = 32;
        private const int CellsPerTile = GurukulLocationData.CellsPerTile;
        private const string ArtFolder = "Assets/Gurukul/Art";

        private GurukulLocationData target;

        // Working state — edited live, written back on Save.
        private string workId = "";
        private string workName = "";
        private Sprite workArt;
        private GameObject workProps;
        private int tileWidth, tileHeight;
        private bool[] blocked = new bool[0];
        private readonly List<GurukulPropFootprint> props = new();
        private bool dirty;

        private GurukulLocationDatabase database;
        private bool registered;

        private Tool tool = Tool.Walkability;
        private Brush brush = Brush.Block;
        private string newPropId = "";
        private Vector2Int dragStart = -Vector2Int.one;

        private float zoom = 22f;
        private Vector2 scroll;

        private int CellsWide => tileWidth * CellsPerTile;
        private int CellsHigh => tileHeight * CellsPerTile;

        [MenuItem("Project Astra/Gurukul/Location Editor")]
        public static void Open()
        {
            var window = GetWindow<GurukulLocationEditorWindow>("Location Editor");
            window.minSize = new Vector2(720, 780);
        }

        private void OnGUI()
        {
            DrawTargetField();
            if (target == null)
            {
                EditorGUILayout.HelpBox("Pick a GurukulLocationData to edit, or create one from the asset menu.", MessageType.Info);
                return;
            }

            DrawIdentityFields();
            DrawArtFields();
            DrawToolbar();
            DrawPalette();
            DrawValidation();
            DrawActions();

            scroll = EditorGUILayout.BeginScrollView(scroll);
            DrawGridCanvas();
            EditorGUILayout.EndScrollView();
        }

        // --- Header -----------------------------------------------------

        private void DrawTargetField()
        {
            EditorGUI.BeginChangeCheck();
            var picked = (GurukulLocationData)EditorGUILayout.ObjectField("Location", target, typeof(GurukulLocationData), false);
            if (EditorGUI.EndChangeCheck()) LoadTarget(picked);
        }

        private void DrawIdentityFields()
        {
            EditorGUI.BeginChangeCheck();
            workId = EditorGUILayout.TextField("Location Id", workId);
            workName = EditorGUILayout.TextField("Display Name", workName);
            if (EditorGUI.EndChangeCheck()) dirty = true;
        }

        private void DrawArtFields()
        {
            EditorGUILayout.BeginHorizontal();
            EditorGUI.BeginChangeCheck();
            workArt = (Sprite)EditorGUILayout.ObjectField("Base Art", workArt, typeof(Sprite), false);
            if (EditorGUI.EndChangeCheck()) dirty = true;

            if (GUILayout.Button("Import PNG", GUILayout.Width(90))) ImportPng();
            if (GUILayout.Button("Fit To Art", GUILayout.Width(80))) SyncSizeToArt();
            EditorGUILayout.EndHorizontal();

            EditorGUI.BeginChangeCheck();
            workProps = (GameObject)EditorGUILayout.ObjectField("Props Prefab", workProps, typeof(GameObject), false);
            if (EditorGUI.EndChangeCheck()) dirty = true;

            EditorGUILayout.LabelField("Size", $"{tileWidth} x {tileHeight} tiles  ({CellsWide} x {CellsHigh} cells)");
            zoom = EditorGUILayout.Slider("Zoom", zoom, 8f, 48f);
        }

        private void DrawToolbar()
        {
            EditorGUILayout.Space(6);
            tool = (Tool)GUILayout.Toolbar((int)tool, new[] { "Walkability", "Props" });
        }

        private void DrawPalette()
        {
            if (tool == Tool.Walkability)
            {
                brush = (Brush)GUILayout.Toolbar((int)brush, new[] { "Block", "Clear" });
                EditorGUILayout.HelpBox("Cells are half a tile. Paint the blocking base of a wall, not its whole picture. The room's outer edge already blocks.", MessageType.None);
                return;
            }

            newPropId = EditorGUILayout.TextField("New Prop Id", newPropId);
            EditorGUILayout.HelpBox("Drag on the canvas to box out a prop's blocking area — the trunk of a tree, not its canopy.", MessageType.None);
            DrawPropList();
        }

        private void DrawPropList()
        {
            for (int i = props.Count - 1; i >= 0; i--)
            {
                EditorGUILayout.BeginHorizontal();
                GurukulPropFootprint prop = props[i];

                EditorGUI.BeginChangeCheck();
                prop.propId = EditorGUILayout.TextField(prop.propId);
                prop.startsSolid = EditorGUILayout.ToggleLeft("Solid", prop.startsSolid, GUILayout.Width(60));
                if (EditorGUI.EndChangeCheck()) { props[i] = prop; dirty = true; }

                EditorGUILayout.LabelField(FormatRect(prop.footprint), GUILayout.Width(150));
                if (GUILayout.Button("x", GUILayout.Width(22))) { props.RemoveAt(i); dirty = true; }
                EditorGUILayout.EndHorizontal();
            }
        }

        private static string FormatRect(Rect rect) =>
            $"({rect.x:0.##}, {rect.y:0.##})  {rect.width:0.##} x {rect.height:0.##}";

        // --- Canvas -----------------------------------------------------

        private void DrawGridCanvas()
        {
            if (CellsWide <= 0 || CellsHigh <= 0) return;

            var rect = GUILayoutUtility.GetRect(CellsWide * zoom, CellsHigh * zoom, GUILayout.ExpandWidth(false));
            DrawArt(rect);
            DrawBlockedOverlay(rect);
            DrawGridLines(rect);
            DrawPropOutlines(rect);
            HandleCanvasInput(rect);
        }

        private void DrawArt(Rect rect)
        {
            if (workArt != null && workArt.texture != null)
                GUI.DrawTexture(rect, workArt.texture, ScaleMode.StretchToFill);
            else
                EditorGUI.DrawRect(rect, new Color(0.15f, 0.15f, 0.15f));
        }

        private void DrawBlockedOverlay(Rect rect)
        {
            var blockedTint = new Color(0.9f, 0.2f, 0.2f, 0.45f);
            for (int y = 0; y < CellsHigh; y++)
                for (int x = 0; x < CellsWide; x++)
                    if (blocked[y * CellsWide + x])
                        EditorGUI.DrawRect(CellRect(rect, x, y), blockedTint);
        }

        // Tile boundaries drawn heavier than the half-tile ones, so the eye still reads the tile
        // grid the art was painted on.
        private void DrawGridLines(Rect rect)
        {
            var fine = new Color(0f, 0f, 0f, 0.15f);
            var coarse = new Color(0f, 0f, 0f, 0.4f);

            for (int x = 0; x <= CellsWide; x++)
                EditorGUI.DrawRect(new Rect(rect.x + x * zoom, rect.y, 1, CellsHigh * zoom),
                    x % CellsPerTile == 0 ? coarse : fine);

            for (int y = 0; y <= CellsHigh; y++)
                EditorGUI.DrawRect(new Rect(rect.x, rect.y + y * zoom, CellsWide * zoom, 1),
                    y % CellsPerTile == 0 ? coarse : fine);
        }

        private void DrawPropOutlines(Rect rect)
        {
            var tint = new Color(1f, 0.85f, 0.2f, 0.55f);
            foreach (GurukulPropFootprint prop in props)
                EditorGUI.DrawRect(TileRectToCanvas(rect, prop.footprint), tint);
        }

        private void HandleCanvasInput(Rect rect)
        {
            int id = GUIUtility.GetControlID(FocusType.Passive);
            Event e = Event.current;

            switch (e.GetTypeForControl(id))
            {
                case EventType.MouseDown:
                    if (e.button != 0 || !rect.Contains(e.mousePosition)) break;
                    GUIUtility.hotControl = id;
                    GUIUtility.keyboardControl = 0;
                    BeginStroke(CellUnder(rect, e.mousePosition));
                    e.Use();
                    break;

                case EventType.MouseDrag:
                    if (GUIUtility.hotControl != id || !rect.Contains(e.mousePosition)) break;
                    ContinueStroke(CellUnder(rect, e.mousePosition));
                    e.Use();
                    break;

                case EventType.MouseUp:
                    if (GUIUtility.hotControl != id) break;
                    EndStroke(CellUnder(rect, e.mousePosition));
                    GUIUtility.hotControl = 0;
                    e.Use();
                    break;
            }
        }

        private void BeginStroke(Vector2Int cell)
        {
            dragStart = cell;
            if (tool == Tool.Walkability) PaintCell(cell);
            Repaint();
        }

        private void ContinueStroke(Vector2Int cell)
        {
            if (tool == Tool.Walkability) PaintCell(cell);
            Repaint();
        }

        private void EndStroke(Vector2Int cell)
        {
            if (tool == Tool.Props) AddPropFromDrag(dragStart, cell);
            dragStart = -Vector2Int.one;
            Repaint();
        }

        private void PaintCell(Vector2Int cell)
        {
            int index = cell.y * CellsWide + cell.x;
            if (index < 0 || index >= blocked.Length) return;
            blocked[index] = brush == Brush.Block;
            dirty = true;
        }

        private void AddPropFromDrag(Vector2Int from, Vector2Int to)
        {
            if (from.x < 0) return;

            int minX = Mathf.Min(from.x, to.x), maxX = Mathf.Max(from.x, to.x);
            int minY = Mathf.Min(from.y, to.y), maxY = Mathf.Max(from.y, to.y);

            props.Add(new GurukulPropFootprint
            {
                propId = string.IsNullOrEmpty(newPropId) ? $"prop_{props.Count}" : newPropId,
                startsSolid = true,
                footprint = new Rect(
                    minX * GurukulCollisionMap.CellSize, minY * GurukulCollisionMap.CellSize,
                    (maxX - minX + 1) * GurukulCollisionMap.CellSize,
                    (maxY - minY + 1) * GurukulCollisionMap.CellSize)
            });
            dirty = true;
        }

        // Screen y grows downward while world y grows up, so the row is flipped.
        private Vector2Int CellUnder(Rect rect, Vector2 mouse)
        {
            int x = Mathf.Clamp(Mathf.FloorToInt((mouse.x - rect.x) / zoom), 0, CellsWide - 1);
            int y = Mathf.Clamp(CellsHigh - 1 - Mathf.FloorToInt((mouse.y - rect.y) / zoom), 0, CellsHigh - 1);
            return new Vector2Int(x, y);
        }

        private Rect CellRect(Rect canvas, int cellX, int cellY) =>
            new(canvas.x + cellX * zoom, canvas.y + (CellsHigh - 1 - cellY) * zoom, zoom, zoom);

        private Rect TileRectToCanvas(Rect canvas, Rect tiles)
        {
            float cell = GurukulCollisionMap.CellSize;
            float x = canvas.x + tiles.xMin / cell * zoom;
            float topCell = CellsHigh - tiles.yMax / cell;
            return new Rect(x, canvas.y + topCell * zoom, tiles.width / cell * zoom, tiles.height / cell * zoom);
        }

        // --- Art import -------------------------------------------------

        private void ImportPng()
        {
            string source = EditorUtility.OpenFilePanel("Import location PNG", "", "png");
            if (string.IsNullOrEmpty(source)) return;

            EnsureArtFolder();
            string destination = $"{ArtFolder}/{Path.GetFileName(source)}";
            File.Copy(source, Path.Combine(Application.dataPath, "..", destination), true);
            AssetDatabase.Refresh();
            ConfigureImporter(destination);

            workArt = AssetDatabase.LoadAssetAtPath<Sprite>(destination);
            dirty = true;
            SyncSizeToArt();
        }

        private static void EnsureArtFolder()
        {
            if (!AssetDatabase.IsValidFolder("Assets/Gurukul")) AssetDatabase.CreateFolder("Assets", "Gurukul");
            if (!AssetDatabase.IsValidFolder(ArtFolder)) AssetDatabase.CreateFolder("Assets/Gurukul", "Art");
        }

        private static void ConfigureImporter(string path)
        {
            var importer = (TextureImporter)AssetImporter.GetAtPath(path);
            importer.textureType = TextureImporterType.Sprite;
            importer.spriteImportMode = SpriteImportMode.Single;
            importer.spritePixelsPerUnit = TilePixels;
            importer.filterMode = FilterMode.Point;
            importer.textureCompression = TextureImporterCompression.Uncompressed;
            importer.mipmapEnabled = false;

            var settings = new TextureImporterSettings();
            importer.ReadTextureSettings(settings);
            settings.spriteAlignment = (int)SpriteAlignment.BottomLeft;
            importer.SetTextureSettings(settings);
            importer.SaveAndReimport();
        }

        private void SyncSizeToArt()
        {
            if (workArt == null)
            {
                EditorUtility.DisplayDialog("No art", "Assign a base sprite first.", "OK");
                return;
            }

            int pixelWidth = (int)workArt.rect.width, pixelHeight = (int)workArt.rect.height;
            if (pixelWidth % TilePixels != 0 || pixelHeight % TilePixels != 0)
            {
                EditorUtility.DisplayDialog("Bad size",
                    $"Art is {pixelWidth}x{pixelHeight}px — both must be multiples of {TilePixels}.", "OK");
                return;
            }

            ResizeMask(pixelWidth / TilePixels, pixelHeight / TilePixels);
        }

        private void ResizeMask(int newTileWidth, int newTileHeight)
        {
            int newCellsWide = newTileWidth * CellsPerTile;
            int newCellsHigh = newTileHeight * CellsPerTile;
            var resized = new bool[newCellsWide * newCellsHigh];

            for (int y = 0; y < newCellsHigh && y < CellsHigh; y++)
                for (int x = 0; x < newCellsWide && x < CellsWide; x++)
                    resized[y * newCellsWide + x] = blocked[y * CellsWide + x];

            tileWidth = newTileWidth;
            tileHeight = newTileHeight;
            blocked = resized;
            dirty = true;
        }

        // --- Validation and saving --------------------------------------

        private void DrawValidation()
        {
            List<string> issues = Validate();
            if (issues.Count == 0) return;
            EditorGUILayout.HelpBox(string.Join("\n", issues), MessageType.Warning);
        }

        private List<string> Validate()
        {
            var issues = new List<string>();
            if (string.IsNullOrEmpty(workId)) issues.Add("Location Id is empty.");
            if (workArt == null) issues.Add("No base art assigned.");

            var seen = new HashSet<string>();
            foreach (GurukulPropFootprint prop in props)
            {
                if (string.IsNullOrEmpty(prop.propId)) issues.Add("A prop footprint has no id.");
                else if (!seen.Add(prop.propId)) issues.Add($"Duplicate prop id '{prop.propId}'.");
            }

            if (AllCellsBlocked()) issues.Add("Every cell is blocked — there is nowhere to stand.");
            return issues;
        }

        private bool AllCellsBlocked()
        {
            foreach (bool cell in blocked)
                if (!cell) return false;
            return blocked.Length > 0;
        }

        private void DrawActions()
        {
            EditorGUILayout.BeginHorizontal();
            if (GUILayout.Button(dirty ? "Save *" : "Save")) SaveTarget();
            using (new EditorGUI.DisabledScope(database == null || registered))
                if (GUILayout.Button("Register In Catalog")) RegisterInCatalog();
            EditorGUILayout.EndHorizontal();
        }

        private void LoadTarget(GurukulLocationData picked)
        {
            target = picked;
            dirty = false;
            if (target == null) return;

            workId = target.LocationId;
            workName = target.DisplayName;
            workArt = target.BaseArt;
            workProps = target.PropsPrefab;
            tileWidth = target.TileWidth;
            tileHeight = target.TileHeight;

            blocked = new bool[CellsWide * CellsHigh];
            for (int i = 0; i < blocked.Length && i < target.BlockedCells.Length; i++)
                blocked[i] = target.BlockedCells[i];

            props.Clear();
            props.AddRange(target.PropFootprints);

            CacheCatalog();
        }

        private void CacheCatalog()
        {
            string[] guids = AssetDatabase.FindAssets("t:GurukulLocationDatabase");
            database = guids.Length > 0
                ? AssetDatabase.LoadAssetAtPath<GurukulLocationDatabase>(AssetDatabase.GUIDToAssetPath(guids[0]))
                : null;
            registered = database != null && database.Get(workId) == target;
        }

        private void SaveTarget()
        {
            var serialized = new SerializedObject(target);
            serialized.FindProperty("locationId").stringValue = workId;
            serialized.FindProperty("displayName").stringValue = workName;
            serialized.FindProperty("baseArt").objectReferenceValue = workArt;
            serialized.FindProperty("propsPrefab").objectReferenceValue = workProps;
            serialized.FindProperty("tileWidth").intValue = tileWidth;
            serialized.FindProperty("tileHeight").intValue = tileHeight;
            WriteBlocked(serialized);
            WriteProps(serialized);
            serialized.ApplyModifiedProperties();

            EditorUtility.SetDirty(target);
            AssetDatabase.SaveAssets();
            dirty = false;
            CacheCatalog();
        }

        private void WriteBlocked(SerializedObject serialized)
        {
            SerializedProperty cells = serialized.FindProperty("blockedCells");
            cells.arraySize = blocked.Length;
            for (int i = 0; i < blocked.Length; i++)
                cells.GetArrayElementAtIndex(i).boolValue = blocked[i];
        }

        private void WriteProps(SerializedObject serialized)
        {
            SerializedProperty list = serialized.FindProperty("propFootprints");
            list.arraySize = props.Count;
            for (int i = 0; i < props.Count; i++)
            {
                SerializedProperty entry = list.GetArrayElementAtIndex(i);
                entry.FindPropertyRelative("propId").stringValue = props[i].propId;
                entry.FindPropertyRelative("startsSolid").boolValue = props[i].startsSolid;
                entry.FindPropertyRelative("footprint").rectValue = props[i].footprint;
            }
        }

        private void RegisterInCatalog()
        {
            var serialized = new SerializedObject(database);
            SerializedProperty list = serialized.FindProperty("locations");

            for (int i = 0; i < list.arraySize; i++)
                if (list.GetArrayElementAtIndex(i).objectReferenceValue == target) return;

            list.InsertArrayElementAtIndex(list.arraySize);
            list.GetArrayElementAtIndex(list.arraySize - 1).objectReferenceValue = target;
            serialized.ApplyModifiedProperties();
            EditorUtility.SetDirty(database);
            AssetDatabase.SaveAssets();
            registered = true;
        }
    }
}
