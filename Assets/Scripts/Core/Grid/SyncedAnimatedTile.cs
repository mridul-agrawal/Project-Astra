using UnityEngine;
using UnityEngine.Tilemaps;

namespace ProjectAstra.Core.Grid
{
    // Tile asset whose frame index is driven by global time, so every instance of the same
    // asset (water, lava, ...) shows the same frame at the same moment. Animation phase is
    // pinned to epoch (animationStartTime = 0) so visual sync survives scene reloads too.
    [CreateAssetMenu(menuName = "Project Astra/Map/Synced Animated Tile")]
    public class SyncedAnimatedTile : TileBase
    {
        [SerializeField] private Sprite[] _frames;
        [SerializeField] private float _frameRate = 4f;

        public override void GetTileData(Vector3Int position, ITilemap tilemap, ref TileData tileData)
        {
            if (!HasFrames()) return;

            tileData.sprite = _frames[GetCurrentFrameIndex()];
            tileData.colliderType = Tile.ColliderType.None;
        }

        // Tells Unity's tilemap animation system which frames to cycle. Returning false for
        // static (0-1 frame) tiles disables the animation pass entirely.
        public override bool GetTileAnimationData(Vector3Int position, ITilemap tilemap,
            ref TileAnimationData tileAnimationData)
        {
            if (!HasFrames() || _frames.Length <= 1) return false;

            tileAnimationData.animatedSprites = _frames;
            tileAnimationData.animationSpeed = _frameRate;
            tileAnimationData.animationStartTime = 0f;
            return true;
        }

        private bool HasFrames() => _frames != null && _frames.Length > 0;

        private int GetCurrentFrameIndex()
        {
            if (_frameRate <= 0f) return 0;
            float totalFrames = Time.time * _frameRate;
            return Mathf.FloorToInt(totalFrames) % _frames.Length;
        }
    }
}
