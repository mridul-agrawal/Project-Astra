using UnityEngine;

namespace ProjectAstra.Core
{
    /// <summary>
    /// Loads a MapData asset into the MapRenderer on scene start.
    /// Temporary entry point for testing — will be replaced by the scene/chapter
    /// loading system once that is built.
    /// </summary>
    public class MapBootstrapper : MonoBehaviour
    {
        [SerializeField] private MapRenderer _mapRenderer;
        [SerializeField] private MapData _mapData;

        private void Start()
        {
            if (_mapRenderer != null && _mapData != null)
                _mapRenderer.LoadMap(_mapData);
        }
    }
}
