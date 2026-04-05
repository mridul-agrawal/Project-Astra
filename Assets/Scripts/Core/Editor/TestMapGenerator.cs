using UnityEditor;
using UnityEngine;
using ProjectAstra.Core;

namespace ProjectAstra.Core.Editor
{
    public static class TestMapGenerator
    {
        private const string MapFolder = "Assets/ScriptableObjects/Map/Maps";

        [MenuItem("Project Astra/Map/Generate Test Map (16x16)")]
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

            string path = $"{MapFolder}/TestMap16x16.asset";
            var mapData = AssetDatabase.LoadAssetAtPath<MapData>(path);
            if (mapData == null)
            {
                mapData = ScriptableObject.CreateInstance<MapData>();
                AssetDatabase.CreateAsset(mapData, path);
            }

            var so = new SerializedObject(mapData);
            so.FindProperty("_mapName").stringValue = "Test Map 16x16";
            so.FindProperty("_width").intValue = 16;
            so.FindProperty("_height").intValue = 16;

            // Tilesets array
            var tilesetsProp = so.FindProperty("_tilesets");
            tilesetsProp.arraySize = 1;
            tilesetsProp.GetArrayElementAtIndex(0).objectReferenceValue = tileset;

            // Tile IDs: 0=Plain, 1=Forest, 2=Mountain, 3=Peak, 4=Water, 7=Road, 9=Fort, 13=Wall, 15=Rubble
            int[] groundIds = {
                // Row 0-3: Northern plains with forest patch and road
                0, 0, 0, 1, 1, 0, 0, 7, 7, 0, 0, 0, 1, 1, 0, 0,
                0, 0, 1, 1, 1, 0, 7, 7, 0, 0, 0, 1, 1, 2, 0, 0,
                0, 0, 0, 1, 0, 0, 7, 0, 0, 0, 1, 2, 2, 2, 0, 0,
                7, 7, 0, 0, 0, 7, 7, 0, 0, 1, 2, 3, 2, 1, 0, 0,
                // Row 4-7: Road system and wall barrier
                0, 7, 0, 0, 7, 7, 0, 0, 13,13,13, 0, 0, 0, 0, 0,
                0, 7, 0, 0, 7, 0, 0, 0, 0, 0, 0, 0, 0, 9, 0, 0,
                0, 7, 7, 7, 7, 0, 0, 4, 4, 4, 0, 0, 9, 9, 9, 0,
                0, 0, 0, 0, 0, 0, 4, 4, 4, 4, 4, 0, 0, 9, 0, 0,
                // Row 8-11: Central lake and fort area
                0, 0, 0, 0, 0, 0, 4, 4, 4, 4, 4, 0, 0, 0, 0, 0,
                0, 1, 0, 0, 0, 0, 0, 4, 4, 4, 0, 0, 0, 0, 1, 0,
                0, 1, 1, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 1, 1, 0,
                0, 0, 1, 0, 0, 7, 7, 7, 0, 0, 0, 0, 1, 1, 0, 0,
                // Row 12-15: Southern area with rubble and mixed terrain
                0, 0, 0, 0, 7, 7, 0, 7, 7, 0, 0, 15,15, 0, 0, 0,
                0, 0, 0, 7, 7, 0, 0, 0, 7, 7, 0, 15, 0, 0, 0, 0,
                0, 0, 7, 7, 0, 0, 0, 0, 0, 7, 7, 0, 0, 0, 0, 0,
                0, 7, 7, 0, 0, 0, 0, 0, 0, 0, 7, 7, 0, 0, 0, 0
            };

            // Overlay layer — sparse decorations
            int[] overlayIds = new int[256];
            for (int i = 0; i < 256; i++) overlayIds[i] = -1;
            overlayIds[6 * 16 + 7] = 6;   // river at (7,6)
            overlayIds[8 * 16 + 7] = 6;   // river at (7,8)
            overlayIds[9 * 16 + 8] = 6;   // river at (8,9)

            // Object layer — interactive objects
            int[] objectIds = new int[256];
            for (int i = 0; i < 256; i++) objectIds[i] = -1;
            objectIds[5 * 16 + 13] = 9;   // fort at (13,5)
            objectIds[4 * 16 + 15] = 11;  // chest at (15,4)
            objectIds[4 * 16 + 8] = 12;   // door at (8,4) adjacent to wall

            var layersProp = so.FindProperty("_layers");
            layersProp.arraySize = 3;

            SetLayerData(layersProp.GetArrayElementAtIndex(0), MapLayer.Ground, 0, groundIds);
            SetLayerData(layersProp.GetArrayElementAtIndex(1), MapLayer.Overlay, 0, overlayIds);
            SetLayerData(layersProp.GetArrayElementAtIndex(2), MapLayer.Object, 0, objectIds);

            so.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(mapData);
            AssetDatabase.SaveAssets();

            Debug.Log("Test Map 16x16 generated successfully.");
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
