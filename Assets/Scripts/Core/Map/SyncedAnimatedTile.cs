using UnityEngine;
using UnityEngine.Tilemaps;

namespace ProjectAstra.Core
{
    /// <summary>
    /// Animated tile that stays globally synchronized — all instances of the same asset
    /// always display the same frame. Used for water, lava, or any repeating terrain animation
    /// where visual coherence across the map is required.
    ///
    /// Sync is achieved by pinning animationStartTime to 0 (epoch), so every instance
    /// shares the same phase. Unity's tilemap animator drives the frame cycling.
    /// </summary>
    [CreateAssetMenu(menuName = "Project Astra/Map/Synced Animated Tile")]
    public class SyncedAnimatedTile : TileBase
    {
        [SerializeField] private Sprite[] _frames;
        [SerializeField] private float _frameRate = 4f; // Frames per second, independent of game simulation

        /// <summary>Called by the tilemap to get the tile's current visual state.</summary>
        public override void GetTileData(Vector3Int position, ITilemap tilemap, ref TileData tileData)
        {
            if (!HasFrames()) return;

            tileData.sprite = _frames[GetCurrentFrameIndex()];
            tileData.colliderType = Tile.ColliderType.None;
        }

        /// <summary>
        /// Provides animation data to Unity's tilemap animation system.
        /// Returns false for static tiles (0-1 frames), which tells Unity not to animate.
        /// </summary>
        public override bool GetTileAnimationData(Vector3Int position, ITilemap tilemap,
            ref TileAnimationData tileAnimationData)
        {
            if (!HasFrames() || _frames.Length <= 1) return false;

            tileAnimationData.animatedSprites = _frames;
            tileAnimationData.animationSpeed = _frameRate;
            tileAnimationData.animationStartTime = 0f; // Epoch-pinned for global sync
            return true;
        }

        private bool HasFrames()
        {
            return _frames != null && _frames.Length > 0;
        }

        /// <summary>Derives the current frame from global time so all instances stay in lockstep.</summary>
        private int GetCurrentFrameIndex()
        {
            if (_frameRate <= 0f) return 0;
            float totalFrames = Time.time * _frameRate;
            return Mathf.FloorToInt(totalFrames) % _frames.Length;
        }
    }
}
