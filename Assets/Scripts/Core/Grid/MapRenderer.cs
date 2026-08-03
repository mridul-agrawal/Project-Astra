using System;
using System.Collections.Generic;
using UnityEngine;

namespace ProjectAstra.Core.Grid
{
    // Draws a map's seamless base art plus its object-layer sprites, and answers terrain
    // queries from the map's per-cell terrain grid. Runtime cell changes (future
    // destructibles) fire OnCellChanged so pathfinding can rebuild its graph.
    public class MapRenderer : MonoBehaviour
    {
        [SerializeField] private SpriteRenderer baseArtRenderer;
        [SerializeField] private Transform objectContainer;
        [SerializeField] private Transform environmentContainer;   // optional; defaults to this transform

        private MapData currentMap;
        private readonly List<GameObject> spawnedObjects = new();
        private GameObject environmentInstance;

        public MapData CurrentMap => currentMap;

        // Fires when a cell changes at runtime. Pathfinding listens so it can rebuild.
        public event Action<Vector2Int> OnCellChanged;

        public void LoadMap(MapData mapData)
        {
            if (mapData == null)
            {
                Debug.LogError("MapRenderer: Cannot load null MapData.");
                return;
            }

            currentMap = mapData;
            DrawBaseArt(mapData);
            SpawnObjects(mapData);
            SpawnEnvironment(mapData);
        }

        public TerrainType GetTerrainType(int x, int y)
        {
            if (currentMap == null) return TerrainType.Void;
            return currentMap.TerrainAt(x, y);
        }

        // Scaffold for destructibles: swap an object's cell and tell pathfinding to rebuild.
        public void NotifyCellChanged(Vector2Int cell) => OnCellChanged?.Invoke(cell);

        private void DrawBaseArt(MapData mapData)
        {
            if (baseArtRenderer == null)
            {
                Debug.LogError("MapRenderer: No base art renderer assigned.");
                return;
            }

            baseArtRenderer.sprite = mapData.BaseArt;
            baseArtRenderer.transform.localPosition = Vector3.zero;
            if (mapData.BaseArt == null)
                Debug.LogWarning($"MapRenderer: Map '{mapData.MapName}' has no base art assigned.");
        }

        private void SpawnObjects(MapData mapData)
        {
            ClearObjects();
            if (objectContainer == null || mapData.Objects == null) return;

            foreach (MapObject mapObject in mapData.Objects)
                SpawnObject(mapObject);
        }

        private void SpawnObject(MapObject mapObject)
        {
            var go = new GameObject($"Object_{mapObject.objectId}");
            go.transform.SetParent(objectContainer, false);
            go.transform.localPosition = CellCenter(mapObject.position);

            var renderer = go.AddComponent<SpriteRenderer>();
            renderer.sprite = mapObject.sprite;
            renderer.sortingLayerName = "Object";
            spawnedObjects.Add(go);
        }

        private static Vector3 CellCenter(Vector2Int cell) => new(cell.x + 0.5f, cell.y + 0.5f, 0f);

        private void ClearObjects()
        {
            foreach (var go in spawnedObjects)
                if (go != null) Destroy(go);
            spawnedObjects.Clear();
        }

        // Drops in the map's animated environment: the per-map prefab authored in the
        // Environment Builder. Its children own their own placement, animators, and
        // behaviour components (paths, timers, particles, a base-layer river patch).
        private void SpawnEnvironment(MapData mapData)
        {
            ClearEnvironment();
            if (mapData.EnvironmentPrefab == null) return;

            Transform parent = environmentContainer != null ? environmentContainer : transform;
            environmentInstance = Instantiate(mapData.EnvironmentPrefab, parent);
            environmentInstance.transform.localPosition = Vector3.zero;
        }

        private void ClearEnvironment()
        {
            if (environmentInstance != null) Destroy(environmentInstance);
            environmentInstance = null;
        }
    }
}
