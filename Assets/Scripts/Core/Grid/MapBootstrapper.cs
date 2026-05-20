using UnityEngine;

namespace ProjectAstra.Core.Grid
{
    // Hands a MapData asset to the MapRenderer on Start. Temporary entry point used while
    // there's no scene/chapter loading system yet — replace once that lands.
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
