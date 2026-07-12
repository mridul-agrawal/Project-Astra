using UnityEditor;
using UnityEngine;
using ProjectAstra.Core.Grid;

namespace ProjectAstra.Core.Editor
{
    public static class MapGridSetup
    {
        [MenuItem("Project Astra/Map/Create Map Grid")]
        public static void CreateMapGrid()
        {
            var gridGO = new GameObject("MapGrid");

            var baseArtGO = new GameObject("BaseArt");
            baseArtGO.transform.SetParent(gridGO.transform, false);
            var baseRenderer = baseArtGO.AddComponent<SpriteRenderer>();
            baseRenderer.sortingLayerName = "Ground";
            baseRenderer.sortingOrder = 0;

            var objectsGO = new GameObject("Objects");
            objectsGO.transform.SetParent(gridGO.transform, false);

            var mapRenderer = gridGO.AddComponent<MapRenderer>();
            var so = new SerializedObject(mapRenderer);
            so.FindProperty("baseArtRenderer").objectReferenceValue = baseRenderer;
            so.FindProperty("objectContainer").objectReferenceValue = objectsGO.transform;
            so.ApplyModifiedPropertiesWithoutUndo();

            Selection.activeGameObject = gridGO;
            Undo.RegisterCreatedObjectUndo(gridGO, "Create Map Grid");
            Debug.Log("Map Grid created with base-art renderer and object container.");
        }
    }
}
