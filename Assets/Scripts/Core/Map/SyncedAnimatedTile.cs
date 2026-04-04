using UnityEngine;
using UnityEngine.Tilemaps;

namespace ProjectAstra.Core
{
    [CreateAssetMenu(menuName = "Project Astra/Map/Synced Animated Tile")]
    public class SyncedAnimatedTile : TileBase
    {
        [SerializeField] private Sprite[] _frames;
        [SerializeField] private float _frameRate = 4f;

        public override void GetTileData(Vector3Int position, ITilemap tilemap, ref TileData tileData)
        {
            if (_frames == null || _frames.Length == 0) return;

            int frameIndex = GetCurrentFrameIndex();
            tileData.sprite = _frames[frameIndex];
            tileData.colliderType = Tile.ColliderType.None;
        }

        public override bool GetTileAnimationData(Vector3Int position, ITilemap tilemap,
            ref TileAnimationData tileAnimationData)
        {
            if (_frames == null || _frames.Length <= 1) return false;

            tileAnimationData.animatedSprites = _frames;
            tileAnimationData.animationSpeed = _frameRate;
            // All instances start from epoch 0 so they stay globally synced
            tileAnimationData.animationStartTime = 0f;
            return true;
        }

        private int GetCurrentFrameIndex()
        {
            if (_frames == null || _frames.Length == 0) return 0;
            if (_frameRate <= 0f) return 0;

            float time = Time.time * _frameRate;
            return Mathf.FloorToInt(time) % _frames.Length;
        }
    }
}
