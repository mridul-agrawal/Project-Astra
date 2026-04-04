using UnityEngine;

namespace ProjectAstra.Core
{
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
