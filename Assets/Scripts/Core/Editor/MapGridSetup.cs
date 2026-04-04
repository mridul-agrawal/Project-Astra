using UnityEditor;
using UnityEngine;
using UnityEngine.Tilemaps;

namespace ProjectAstra.Core.Editor
{
    public static class MapGridSetup
    {
        private static readonly string[] LayerNames = { "Ground", "Overlay", "Object", "Units", "UIOverlay" };

        [MenuItem("Project Astra/Map/Create Map Grid")]
        public static void CreateMapGrid()
        {
            var gridGO = new GameObject("MapGrid");
            var grid = gridGO.AddComponent<Grid>();
            grid.cellSize = new Vector3(1f, 1f, 0f);

            for (int i = 0; i < LayerNames.Length; i++)
            {
                var layerGO = new GameObject(LayerNames[i]);
                layerGO.transform.SetParent(gridGO.transform);

                var tilemap = layerGO.AddComponent<Tilemap>();
                var renderer = layerGO.AddComponent<TilemapRenderer>();
                renderer.sortingLayerName = LayerNames[i];
                renderer.sortingOrder = 0;
            }

            var mapRenderer = gridGO.AddComponent<MapRenderer>();

            // Auto-wire tilemap references via SerializedObject
            var so = new SerializedObject(mapRenderer);
            var tilemapsProp = so.FindProperty("_tilemaps");
            tilemapsProp.arraySize = LayerNames.Length;
            for (int i = 0; i < LayerNames.Length; i++)
            {
                var tilemap = gridGO.transform.GetChild(i).GetComponent<Tilemap>();
                tilemapsProp.GetArrayElementAtIndex(i).objectReferenceValue = tilemap;
            }

            // Wire error tile if it exists
            var errorTile = AssetDatabase.LoadAssetAtPath<TileBase>(
                "Assets/Art/Tilesets/Placeholder/Tiles/ErrorTile.asset");
            if (errorTile != null)
                so.FindProperty("_errorTile").objectReferenceValue = errorTile;

            so.ApplyModifiedPropertiesWithoutUndo();

            Selection.activeGameObject = gridGO;
            Undo.RegisterCreatedObjectUndo(gridGO, "Create Map Grid");
            Debug.Log("Map Grid created with 5 tilemap layers and MapRenderer.");
        }
    }
}
