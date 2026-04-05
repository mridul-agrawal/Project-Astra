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
            if (unit == null || path == null || path.Count <= 1)
            {
                onComplete?.Invoke();
                return;
            }

            StartCoroutine(MoveCoroutine(unit, path, onComplete, onTileEntered));
        }

        /// <summary>Instantly snaps unit back to a position (no animation). Used for movement undo.</summary>
        public void UndoMove(TestUnit unit, Vector2Int originalPosition)
        {
            if (unit == null) return;
            unit.gridPosition = originalPosition;
            unit.SnapToGridPosition();
        }

        private IEnumerator MoveCoroutine(TestUnit unit, List<Vector2Int> path,
            Action onComplete, Action<Vector2Int> onTileEntered)
        {
            IsMoving = true;
            float stepDuration = 1f / _tilesPerSecond;

            // Start from index 1 (index 0 is the origin where the unit already is)
            for (int i = 1; i < path.Count; i++)
            {
                Vector2Int from = path[i - 1];
                Vector2Int to = path[i];

                Vector3 fromWorld = new(from.x + 0.5f, from.y + 0.5f, 0f);
                Vector3 toWorld = new(to.x + 0.5f, to.y + 0.5f, 0f);

                float elapsed = 0f;
                while (elapsed < stepDuration)
                {
                    elapsed += Time.deltaTime;
                    float t = Mathf.Clamp01(elapsed / stepDuration);
                    unit.transform.position = Vector3.Lerp(fromWorld, toWorld, t);
                    yield return null;
                }

                // Snap to exact tile position and update grid coordinate
                unit.gridPosition = to;
                unit.SnapToGridPosition();
                onTileEntered?.Invoke(to);
            }

            IsMoving = false;
            onComplete?.Invoke();
        }
    }
}
