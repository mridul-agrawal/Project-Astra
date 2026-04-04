using UnityEditor;
using UnityEngine;
using ProjectAstra.Core;

namespace ProjectAstra.Core.Editor
{
    public static class TestMapGenerator
    {
        private const string MapFolder = "Assets/ScriptableObjects/Map/Maps";

        [MenuItem("Project Astra/Map/Generate Test Map (4x4)")]
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

            string path = $"{MapFolder}/TestMap4x4.asset";
            var mapData = AssetDatabase.LoadAssetAtPath<MapData>(path);
            if (mapData == null)
            {
                mapData = ScriptableObject.CreateInstance<MapData>();
                AssetDatabase.CreateAsset(mapData, path);
            }

            var so = new SerializedObject(mapData);
            so.FindProperty("_mapName").stringValue = "Test Map 4x4";
            so.FindProperty("_width").intValue = 4;
            so.FindProperty("_height").intValue = 4;

            // Tilesets array
            var tilesetsProp = so.FindProperty("_tilesets");
            tilesetsProp.arraySize = 1;
            tilesetsProp.GetArrayElementAtIndex(0).objectReferenceValue = tileset;

            // Ground layer — mixed terrain
            int[] groundIds = {
                0, 0, 1, 2,  // row 0: plain, plain, forest, mountain
                0, 7, 7, 1,  // row 1: plain, road, road, forest
                4, 4, 0, 0,  // row 2: water, water, plain, plain
                0, 0, 9, 0   // row 3: plain, plain, fort, plain
            };

            // Overlay layer — sparse decorations
            int[] overlayIds = {
                -1, -1, -1, -1,
                -1, -1, -1, -1,
                 6, -1, -1, -1,  // river overlay on row 2
                -1, -1, -1, -1
            };

            // Object layer — a chest and a door
            int[] objectIds = {
                -1, -1, -1, -1,
                -1, -1, -1, -1,
                -1, -1, -1, 11,  // chest at (3,2)
                -1, 12, -1, -1   // door at (1,3)
            };

            var layersProp = so.FindProperty("_layers");
            layersProp.arraySize = 3;

            SetLayerData(layersProp.GetArrayElementAtIndex(0), MapLayer.Ground, 0, groundIds);
            SetLayerData(layersProp.GetArrayElementAtIndex(1), MapLayer.Overlay, 0, overlayIds);
            SetLayerData(layersProp.GetArrayElementAtIndex(2), MapLayer.Object, 0, objectIds);

            so.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(mapData);
            AssetDatabase.SaveAssets();

            Debug.Log("Test Map 4x4 generated successfully.");
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
