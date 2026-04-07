using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace ProjectAstra.Core
{
    /// <summary>
    /// Animates a unit moving tile-by-tile along a given path. Reusable for player,
    /// enemy, and allied movement. Coroutine-driven with configurable speed.
    /// </summary>
    public class UnitMover : MonoBehaviour
    {
        [SerializeField] private float _tilesPerSecond = 8f;

        public bool IsMoving { get; private set; }

        /// <summary>
        /// Animates the unit along the path tile-by-tile. Updates unit.gridPosition at each step.
        /// Calls onComplete when finished. Optional onTileEntered fires at each intermediate tile.
        /// </summary>
        public void MoveAlongPath(TestUnit unit, List<Vector2Int> path,
            Action onComplete, Action<Vector2Int> onTileEntered = null)
        {
            if (!IsPathValid(unit, path))
            {
                onComplete?.Invoke();
                return;
            }

            StartCoroutine(AnimateMovementAlongPath(unit, path, onComplete, onTileEntered));
        }

        /// <summary>Instantly snaps unit back to a position (no animation). Used for movement undo.</summary>
        public void UndoMove(TestUnit unit, Vector2Int originalPosition)
        {
            if (unit == null) return;
            unit.gridPosition = originalPosition;
            unit.SnapToGridPosition();
        }

        private bool IsPathValid(TestUnit unit, List<Vector2Int> path)
        {
            return unit != null && path != null && path.Count > 1;
        }

        private IEnumerator AnimateMovementAlongPath(TestUnit unit, List<Vector2Int> path,
            Action onComplete, Action<Vector2Int> onTileEntered)
        {
            IsMoving = true;
            float secondsPerTile = 1f / _tilesPerSecond;

            for (int i = 1; i < path.Count; i++)
            {
                yield return AnimateSingleStep(unit, path[i - 1], path[i], secondsPerTile);

                unit.gridPosition = path[i];
                unit.SnapToGridPosition();
                onTileEntered?.Invoke(path[i]);
            }

            IsMoving = false;
            onComplete?.Invoke();
        }

        private IEnumerator AnimateSingleStep(TestUnit unit, Vector2Int from, Vector2Int to, float duration)
        {
            Vector3 fromWorld = new(from.x + 0.5f, from.y + 0.5f, 0f);
            Vector3 toWorld = new(to.x + 0.5f, to.y + 0.5f, 0f);

            float elapsed = 0f;
            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                float progress = Mathf.Clamp01(elapsed / duration);
                unit.transform.position = Vector3.Lerp(fromWorld, toWorld, progress);
                yield return null;
            }
        }
    }
}
