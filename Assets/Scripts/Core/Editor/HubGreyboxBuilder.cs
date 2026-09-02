using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;
using ProjectAstra.Core.Animation;
using ProjectAstra.Core.Dialogue;
using ProjectAstra.Core.Hub;
using ProjectAstra.Core.Hub.Interaction;
using ProjectAstra.Core.Hub.Events;

namespace ProjectAstra.Core.Editor
{
    // Makes a programmer-art Hub to build against: a courtyard wider than the screen, walls to
    // bump into, a tree tall enough to walk behind, and a house to go inside.
    //
    // Everything here is placeholder. Real art drops into the same fields on the same assets with
    // no code change — the point is only that the hub systems have somewhere to run today.
    public static class HubGreyboxBuilder
    {
        private const int TilePixels = 32;

        // Twice the 15-tile view, so the camera has to scroll.
        private const int CourtyardWide = 30;
        private const int CourtyardHigh = 18;
        // At least as big as the 15 x 8.4 tiles the camera shows, or it would reveal the void
        // past the room's edges.
        private const int HouseWide = 16;
        private const int HouseHigh = 10;

        private const string Folder = "Assets/Gurukul";
        private const string ArtFolder = Folder + "/Greybox";

        // The sprite pivots bottom-left, so this shifts the interaction point to the middle of its base.
        private static readonly Vector2 TreeFootOffset = new(0.5f, 0f);
        private const string DataFolder = Folder + "/Data";
        private const string PropsPrefabPath = ArtFolder + "/GreyboxProps.prefab";

        private static readonly Color Grass = new(0.36f, 0.44f, 0.28f);
        private static readonly Color GrassLine = new(0.32f, 0.40f, 0.25f);
        private static readonly Color Floor = new(0.42f, 0.34f, 0.26f);
        private static readonly Color FloorLine = new(0.38f, 0.31f, 0.23f);
        private static readonly Color Stone = new(0.30f, 0.27f, 0.24f);
        private static readonly Color TreeTrunk = new(0.28f, 0.20f, 0.14f);
        private static readonly Color TreeCanopy = new(0.20f, 0.34f, 0.20f);

        private static readonly Vector2 TreeFootPosition = new(8f, 6f);

        // Walls in tile coordinates, measured from the bottom-left.
        private static IEnumerable<RectInt> CourtyardWalls()
        {
            foreach (RectInt edge in Border(CourtyardWide, CourtyardHigh)) yield return edge;

            // A wall across the middle with a one-tile doorway, to prove she fits through a gap her
            // own width and can't squeeze past a corner.
            yield return new RectInt(14, 2, 1, 6);
            yield return new RectInt(14, 9, 1, 7);

            yield return new RectInt(4, 12, 5, 1);       // something to walk along
            yield return new RectInt(22, 4, 3, 3);       // a block to walk around
        }

        private static IEnumerable<RectInt> HouseWalls() => Border(HouseWide, HouseHigh);

        private static IEnumerable<RectInt> Border(int wide, int high)
        {
            yield return new RectInt(0, 0, wide, 1);
            yield return new RectInt(0, high - 1, wide, 1);
            yield return new RectInt(0, 0, 1, high);
            yield return new RectInt(wide - 1, 0, 1, high);
        }

        [MenuItem("Project Astra/Hub/Build Greybox Location")]
        public static void Build()
        {
            EnsureFolders();

            Sprite courtyardArt = ImportSprite($"{ArtFolder}/greybox_courtyard.png",
                PaintRoom(CourtyardWide, CourtyardHigh, Grass, GrassLine, CourtyardWalls()));
            Sprite houseArt = ImportSprite($"{ArtFolder}/greybox_house.png",
                PaintRoom(HouseWide, HouseHigh, Floor, FloorLine, HouseWalls()));
            Sprite tree = ImportSprite($"{ArtFolder}/greybox_tree.png", PaintTree());

            GameObject props = BuildPropsPrefab(tree);

            HubLocationData courtyard = BuildCourtyard(courtyardArt, props);
            HubLocationData house = BuildHouse(houseArt);
            HubVisitData visit = BuildVisit(courtyard);

            RegisterInCatalogs(visit, courtyard, house);
            BuildEvents();

            AssetDatabase.SaveAssets();
            Selection.activeObject = courtyard;
            Debug.Log($"[HubGreybox] Built the courtyard, a house to go inside, props and visit '{visit.VisitId}'.");
        }

        // --- Art ---

        private static Texture2D PaintRoom(int tilesWide, int tilesHigh, Color fill, Color line,
            IEnumerable<RectInt> walls)
        {
            int width = tilesWide * TilePixels;
            int height = tilesHigh * TilePixels;
            var texture = new Texture2D(width, height, TextureFormat.RGBA32, false);

            for (int y = 0; y < height; y++)
                for (int x = 0; x < width; x++)
                {
                    bool onTileEdge = x % TilePixels == 0 || y % TilePixels == 0;
                    texture.SetPixel(x, y, onTileEdge ? line : fill);
                }

            foreach (RectInt wall in walls) FillTiles(texture, wall, Stone);

            texture.Apply();
            return texture;
        }

        // A 1x2-tile tree: trunk on the bottom tile, canopy on the top. Only the trunk blocks, which
        // is what lets her walk behind the canopy.
        private static Texture2D PaintTree()
        {
            var texture = new Texture2D(TilePixels, TilePixels * 2, TextureFormat.RGBA32, false);

            for (int y = 0; y < TilePixels * 2; y++)
                for (int x = 0; x < TilePixels; x++)
                {
                    bool isTrunk = y < TilePixels && x >= 12 && x < 20;
                    bool isCanopy = y >= TilePixels - 6;
                    Color color = isCanopy ? TreeCanopy : isTrunk ? TreeTrunk : Color.clear;
                    texture.SetPixel(x, y, color);
                }

            texture.Apply();
            return texture;
        }

        private static void FillTiles(Texture2D texture, RectInt tiles, Color color)
        {
            for (int ty = tiles.yMin; ty < tiles.yMax; ty++)
                for (int tx = tiles.xMin; tx < tiles.xMax; tx++)
                    for (int y = 0; y < TilePixels; y++)
                        for (int x = 0; x < TilePixels; x++)
                            texture.SetPixel(tx * TilePixels + x, ty * TilePixels + y, color);
        }

        private static Sprite ImportSprite(string path, Texture2D texture)
        {
            File.WriteAllBytes(path, texture.EncodeToPNG());
            Object.DestroyImmediate(texture);
            AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceUpdate);

            var importer = (TextureImporter)AssetImporter.GetAtPath(path);
            importer.textureType = TextureImporterType.Sprite;
            importer.spriteImportMode = SpriteImportMode.Single;
            importer.spritePixelsPerUnit = TilePixels;
            importer.filterMode = FilterMode.Point;
            importer.textureCompression = TextureImporterCompression.Uncompressed;
            importer.mipmapEnabled = false;

            // Bottom-left pivot so a sprite dropped at the origin lines up with tile (0,0).
            var settings = new TextureImporterSettings();
            importer.ReadTextureSettings(settings);
            settings.spriteAlignment = (int)SpriteAlignment.BottomLeft;
            importer.SetTextureSettings(settings);
            importer.SaveAndReimport();

            return AssetDatabase.LoadAssetAtPath<Sprite>(path);
        }

        private static GameObject BuildPropsPrefab(Sprite tree)
        {
            var root = new GameObject("GreyboxProps");
            var treeGo = new GameObject("Tree");
            treeGo.transform.SetParent(root.transform, false);
            treeGo.transform.localPosition = new Vector3(TreeFootPosition.x, TreeFootPosition.y, 0f);

            treeGo.AddComponent<SpriteRenderer>().sprite = tree;
            treeGo.AddComponent<YSortRenderer>();
            MakeTreeInspectable(treeGo);

            GameObject prefab = PrefabUtility.SaveAsPrefabAsset(root, PropsPrefabPath);
            Object.DestroyImmediate(root);
            return prefab;
        }

        // The tree doubles as something to walk up to, so the prompt has a target without needing
        // any more placeholder art. Its trigger is the region she has to be standing in.
        private static void MakeTreeInspectable(GameObject treeGo)
        {
            InteractionPhysics.AttachReachRegion(treeGo, TreeFootOffset);

            treeGo.AddComponent<InspectableInteractable>().Configure(
                "greybox_tree", HubVerb.Inspect, TreeFootOffset, "greybox_tree_look",
                gate: null, denied: null, critical: false, HubInteractableState.Available);
        }

        // --- Locations ---

        private static HubLocationData BuildCourtyard(Sprite art, GameObject props)
        {
            var location = LoadOrCreate<HubLocationData>($"{DataFolder}/Location_GreyboxCourtyard.asset");
            var serialized = new SerializedObject(location);

            WriteLocationBasics(serialized, "greybox_courtyard", "Greybox Courtyard", art, props,
                CourtyardWide, CourtyardHigh, CourtyardWalls());
            WriteTreeFootprint(serialized.FindProperty("propFootprints"));
            WriteCourtyardDoor(serialized.FindProperty("doors"));

            serialized.ApplyModifiedProperties();
            EditorUtility.SetDirty(location);
            return location;
        }

        private static HubLocationData BuildHouse(Sprite art)
        {
            var location = LoadOrCreate<HubLocationData>($"{DataFolder}/Location_GreyboxHouse.asset");
            var serialized = new SerializedObject(location);

            WriteLocationBasics(serialized, "greybox_house", "Greybox House", art, null,
                HouseWide, HouseHigh, HouseWalls());
            serialized.FindProperty("propFootprints").arraySize = 0;
            WriteHouseExit(serialized.FindProperty("doors"));

            serialized.ApplyModifiedProperties();
            EditorUtility.SetDirty(location);
            return location;
        }

        private static void WriteLocationBasics(SerializedObject serialized, string id, string name,
            Sprite art, GameObject props, int tilesWide, int tilesHigh, IEnumerable<RectInt> walls)
        {
            serialized.FindProperty("locationId").stringValue = id;
            serialized.FindProperty("displayName").stringValue = name;
            serialized.FindProperty("baseArt").objectReferenceValue = art;
            serialized.FindProperty("propsPrefab").objectReferenceValue = props;
            serialized.FindProperty("tileWidth").intValue = tilesWide;
            serialized.FindProperty("tileHeight").intValue = tilesHigh;
            WriteBlockedCells(serialized.FindProperty("blockedCells"), tilesWide, tilesHigh, walls);
        }

        // The painted walls, expanded from tiles to the half-tile cells the collision map uses.
        private static void WriteBlockedCells(SerializedProperty cells, int tilesWide, int tilesHigh,
            IEnumerable<RectInt> walls)
        {
            int cellsWide = tilesWide * HubLocationData.CellsPerTile;
            int cellsHigh = tilesHigh * HubLocationData.CellsPerTile;

            cells.arraySize = cellsWide * cellsHigh;
            for (int i = 0; i < cells.arraySize; i++)
                cells.GetArrayElementAtIndex(i).boolValue = false;

            foreach (RectInt wall in walls)
                for (int ty = wall.yMin; ty < wall.yMax; ty++)
                    for (int tx = wall.xMin; tx < wall.xMax; tx++)
                        BlockTile(cells, cellsWide, tx, ty);
        }

        private static void BlockTile(SerializedProperty cells, int cellsWide, int tileX, int tileY)
        {
            for (int dy = 0; dy < HubLocationData.CellsPerTile; dy++)
                for (int dx = 0; dx < HubLocationData.CellsPerTile; dx++)
                {
                    int cellX = tileX * HubLocationData.CellsPerTile + dx;
                    int cellY = tileY * HubLocationData.CellsPerTile + dy;
                    cells.GetArrayElementAtIndex(cellY * cellsWide + cellX).boolValue = true;
                }
        }

        // Half a tile wide and half a tile deep at the base — the trunk, not the whole picture.
        private static void WriteTreeFootprint(SerializedProperty footprints)
        {
            footprints.arraySize = 1;
            SerializedProperty entry = footprints.GetArrayElementAtIndex(0);
            entry.FindPropertyRelative("propId").stringValue = "greybox_tree";
            entry.FindPropertyRelative("startsSolid").boolValue = true;
            entry.FindPropertyRelative("footprint").rectValue =
                new Rect(TreeFootPosition.x + 0.25f, TreeFootPosition.y, 0.5f, 0.5f);
        }

        // Set into the north wall, so she walks up to it from inside the courtyard and faces it.
        private static void WriteCourtyardDoor(SerializedProperty doors)
        {
            doors.arraySize = 1;
            SerializedProperty door = doors.GetArrayElementAtIndex(0);
            door.FindPropertyRelative("doorId").stringValue = "greybox_house_door";
            door.FindPropertyRelative("position").vector2Value = new Vector2(10f, 17f);
            door.FindPropertyRelative("verb").enumValueIndex = (int)HubVerb.Enter;
            door.FindPropertyRelative("targetLocationId").stringValue = "greybox_house";
            door.FindPropertyRelative("targetSpawn").vector2Value = new Vector2(8f, 1.5f);
            door.FindPropertyRelative("targetFacing").enumValueIndex = (int)Facing.North;
            door.FindPropertyRelative("houseIdentityId").stringValue = "greybox_house_a";
        }

        // No destination, so it sends her back out through whichever door she came in by — the
        // arrangement that lets six student houses share one interior.
        private static void WriteHouseExit(SerializedProperty doors)
        {
            doors.arraySize = 1;
            SerializedProperty door = doors.GetArrayElementAtIndex(0);
            door.FindPropertyRelative("doorId").stringValue = "greybox_house_exit";
            door.FindPropertyRelative("position").vector2Value = new Vector2(8f, 0f);
            door.FindPropertyRelative("verb").enumValueIndex = (int)HubVerb.Leave;
            door.FindPropertyRelative("targetLocationId").stringValue = "";
        }

        // --- Visit ---

        private static HubVisitData BuildVisit(HubLocationData location)
        {
            var visit = LoadOrCreate<HubVisitData>($"{DataFolder}/Visit_Greybox.asset");
            var serialized = new SerializedObject(visit);

            serialized.FindProperty("visitId").stringValue = "greybox";
            serialized.FindProperty("displayName").stringValue = "Greybox";
            serialized.FindProperty("startLocationId").stringValue = location.LocationId;
            serialized.FindProperty("playerSpawn").vector2Value = new Vector2(4f, 4f);
            serialized.FindProperty("playerFacing").enumValueIndex = (int)Facing.South;
            serialized.FindProperty("openingEventId").stringValue = "greybox_opening";
            WriteTestCharacter(serialized.FindProperty("characterPlacements"), location.LocationId);
            WriteObjectives(serialized.FindProperty("objectives"));

            serialized.ApplyModifiedProperties();
            EditorUtility.SetDirty(visit);
            return visit;
        }

        // One character to walk up to. Uses an existing unit so nothing here depends on the real
        // cast, which design still owes.
        private static void WriteTestCharacter(SerializedProperty placements, string locationId)
        {
            placements.arraySize = 1;
            SerializedProperty entry = placements.GetArrayElementAtIndex(0);
            entry.FindPropertyRelative("characterId").stringValue = "arjun";
            entry.FindPropertyRelative("locationId").stringValue = locationId;
            entry.FindPropertyRelative("position").vector2Value = new Vector2(6f, 4f);
            entry.FindPropertyRelative("facing").enumValueIndex = (int)Facing.West;
            entry.FindPropertyRelative("conversationId").stringValue = "greybox_arjun_talk";
        }

        // Two stages, so the greybox exercises sequencing as well as a single objective: one keyed
        // on finishing a conversation and marked over a character, one keyed on inspecting an object
        // and marked over a prop.
        private static void WriteObjectives(SerializedProperty list)
        {
            HubObjectiveData talk = BuildObjective("greybox_talk", "Talk to Arjun",
                HubConditionKind.ConversationCompleted, "greybox_arjun_talk", "arjun");
            HubObjectiveData look = BuildObjective("greybox_look", "Look at the tree",
                HubConditionKind.ObjectInspected, "greybox_tree", "greybox_tree");

            list.arraySize = 2;
            list.GetArrayElementAtIndex(0).objectReferenceValue = talk;
            list.GetArrayElementAtIndex(1).objectReferenceValue = look;
        }

        private static HubObjectiveData BuildObjective(string objectiveId, string text,
            HubConditionKind kind, string targetId, string markerId)
        {
            var objective = LoadOrCreate<HubObjectiveData>($"{DataFolder}/Objective_{objectiveId}.asset");
            var serialized = new SerializedObject(objective);

            serialized.FindProperty("objectiveId").stringValue = objectiveId;
            serialized.FindProperty("displayText").stringValue = text;

            SerializedProperty completion = serialized.FindProperty("completion");
            completion.FindPropertyRelative("kind").enumValueIndex = (int)kind;
            completion.FindPropertyRelative("showCounter").boolValue = false;

            SerializedProperty targets = completion.FindPropertyRelative("targetIds");
            targets.arraySize = 1;
            targets.GetArrayElementAtIndex(0).stringValue = targetId;

            SerializedProperty markers = serialized.FindProperty("markerTargetIds");
            markers.arraySize = 1;
            markers.GetArrayElementAtIndex(0).stringValue = markerId;

            serialized.ApplyModifiedProperties();
            EditorUtility.SetDirty(objective);
            return objective;
        }

        // --- Placeholder conversations ---

        // --- Placeholder events ---

        // An opening beat that walks Arjun three tiles north before she gets control. Small, but it
        // exercises the whole chain: control is locked, a character walks a cardinal route through
        // the real collision map, and control comes back only once he has settled.
        private static void BuildEvents()
        {
            var opening = LoadOrCreate<HubEventData>($"{DataFolder}/Event_greybox_opening.asset");
            var serialized = new SerializedObject(opening);

            serialized.FindProperty("eventId").stringValue = "greybox_opening";
            serialized.FindProperty("trigger").enumValueIndex = (int)HubEventTrigger.VisitLoad;
            serialized.FindProperty("oneTime").boolValue = true;

            SerializedProperty actions = serialized.FindProperty("actions");
            actions.arraySize = 1;

            SerializedProperty walk = actions.GetArrayElementAtIndex(0);
            walk.FindPropertyRelative("kind").enumValueIndex = (int)HubEventActionKind.WalkCharacter;
            walk.FindPropertyRelative("targetId").stringValue = "arjun";
            walk.FindPropertyRelative("seconds").floatValue = 0f;

            SerializedProperty route = walk.FindPropertyRelative("route");
            route.arraySize = 1;
            route.GetArrayElementAtIndex(0).vector2Value = new Vector2(6f, 7f);

            serialized.ApplyModifiedProperties();
            EditorUtility.SetDirty(opening);

            AppendUnique(LoadOrCreate<HubEventDatabase>($"{DataFolder}/HubEventDatabase.asset"), "events", opening);
        }

        // --- Plumbing ---

        private static void RegisterInCatalogs(HubVisitData visit, params HubLocationData[] locations)
        {
            var locationCatalog = LoadOrCreate<HubLocationDatabase>($"{DataFolder}/HubLocationDatabase.asset");
            foreach (HubLocationData location in locations)
                AppendUnique(locationCatalog, "locations", location);

            AppendUnique(LoadOrCreate<HubVisitDatabase>($"{DataFolder}/HubVisitDatabase.asset"), "visits", visit);
        }

        private static void AppendUnique(ScriptableObject catalog, string listName, Object entry)
        {
            var serialized = new SerializedObject(catalog);
            SerializedProperty list = serialized.FindProperty(listName);

            for (int i = 0; i < list.arraySize; i++)
                if (list.GetArrayElementAtIndex(i).objectReferenceValue == entry) return;

            list.InsertArrayElementAtIndex(list.arraySize);
            list.GetArrayElementAtIndex(list.arraySize - 1).objectReferenceValue = entry;
            serialized.ApplyModifiedProperties();
            EditorUtility.SetDirty(catalog);
        }

        private static T LoadOrCreate<T>(string path) where T : ScriptableObject
        {
            var existing = AssetDatabase.LoadAssetAtPath<T>(path);
            if (existing != null) return existing;

            var created = ScriptableObject.CreateInstance<T>();
            AssetDatabase.CreateAsset(created, path);
            return created;
        }

        private static void EnsureFolders()
        {
            EnsureFolder("Assets", "Hub");
            EnsureFolder(Folder, "Greybox");
            EnsureFolder(Folder, "Data");
        }

        private static void EnsureFolder(string parent, string child)
        {
            if (!AssetDatabase.IsValidFolder($"{parent}/{child}"))
                AssetDatabase.CreateFolder(parent, child);
        }
    }
}
