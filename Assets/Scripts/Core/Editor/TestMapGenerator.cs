using UnityEditor;
using UnityEngine;
using ProjectAstra.Core;

namespace ProjectAstra.Core.Editor
{
    public static class TestMapGenerator
    {
        private const string MapFolder = "Assets/ScriptableObjects/Map/Maps";

        [MenuItem("Project Astra/Map/Generate Test Map (8x8)")]
        public static void GenerateTestMap()
        {
            if (!AssetDatabase.IsValidFolder(MapFolder))
            {
                AssetDatabase.CreateFolder("Assets/ScriptableObjects/Map", "Maps");
            }

            var tileset = AssetDatabase.LoadAssetAtPath<TilesetDefinition>(
                "Assets/ScriptableObjects/Map/Tilesets/PlaceholderTileset.asset");
            if (tileset == null)
            {
                Debug.LogError("TestMapGenerator: PlaceholderTileset not found. Run 'Generate Placeholder Tileset' first.");
                return;
            }

            string path = $"{MapFolder}/TestMap8x8.asset";
            var mapData = AssetDatabase.LoadAssetAtPath<MapData>(path);
            if (mapData == null)
            {
                mapData = ScriptableObject.CreateInstance<MapData>();
                AssetDatabase.CreateAsset(mapData, path);
            }

            var so = new SerializedObject(mapData);
            so.FindProperty("_mapName").stringValue = "Test Map 8x8";
            so.FindProperty("_width").intValue = 8;
            so.FindProperty("_height").intValue = 8;

            // Tilesets array
            var tilesetsProp = so.FindProperty("_tilesets");
            tilesetsProp.arraySize = 1;
            tilesetsProp.GetArrayElementAtIndex(0).objectReferenceValue = tileset;

            // Ground layer — varied terrain for testing pathfinding and movement
            // Tile IDs: 0=Plain, 1=Forest, 2=Mountain, 3=Peak, 4=Water, 7=Road, 9=Fort, 13=Wall, 16=Sand
            int[] groundIds = {
                0, 0, 1, 1, 0, 0, 7, 0,   // row 0: plains, forest patch, road
                0, 0, 1, 2, 0, 7, 7, 0,   // row 1: forest into mountain, road
                7, 7, 0, 2, 0, 7, 0, 15,  // row 2: road, mountains, rubble
                7, 0, 0, 13, 0, 0, 0, 15, // row 3: road, wall barrier, rubble
                0, 0, 4, 4, 4, 0, 0, 0,   // row 4: water lake
                0, 1, 4, 4, 4, 1, 9, 0,   // row 5: lake with forest, fort
                0, 1, 0, 0, 0, 1, 0, 0,   // row 6: forest corridor
                0, 0, 0, 7, 7, 0, 0, 0    // row 7: road through center
            };

            // Overlay layer — river edges along water
            int[] overlayIds = new int[64];
            for (int i = 0; i < 64; i++) overlayIds[i] = -1;
            overlayIds[2 * 8 + 2] = 6;  // river overlay at (2,2)
            overlayIds[4 * 8 + 2] = 6;  // river overlay at (2,4)

            // Object layer — interactive objects
            int[] objectIds = new int[64];
            for (int i = 0; i < 64; i++) objectIds[i] = -1;
            objectIds[5 * 8 + 6] = 9;   // fort structure at (6,5)
            objectIds[3 * 8 + 7] = 11;  // chest at (7,3)
            objectIds[3 * 8 + 3] = 12;  // door at (3,3) — adjacent to wall

            var layersProp = so.FindProperty("_layers");
            layersProp.arraySize = 3;

            SetLayerData(layersProp.GetArrayElementAtIndex(0), MapLayer.Ground, 0, groundIds);
            SetLayerData(layersProp.GetArrayElementAtIndex(1), MapLayer.Overlay, 0, overlayIds);
            SetLayerData(layersProp.GetArrayElementAtIndex(2), MapLayer.Object, 0, objectIds);

            so.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(mapData);
            AssetDatabase.SaveAssets();

            Debug.Log("Test Map 8x8 generated successfully.");
        }

        private static void SetLayerData(SerializedProperty layerProp, MapLayer layer, int tilesetIndex, int[] ids)
        {
            layerProp.FindPropertyRelative("layer").enumValueIndex = (int)layer;
            layerProp.FindPropertyRelative("tilesetIndex").intValue = tilesetIndex;

            var tileIdsProp = layerProp.FindPropertyRelative("tileIds");
            tileIdsProp.arraySize = ids.Length;
            for (int i = 0; i < ids.Length; i++)
                tileIdsProp.GetArrayElementAtIndex(i).intValue = ids[i];
        }
    }
}
