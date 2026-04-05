using UnityEngine;

namespace ProjectAstra.Core
{
    /// <summary>
    /// Minimal test unit for exercising cursor, movement, and targeting flows.
    /// NOT a real unit system — just data + a visual on the Units layer.
    /// </summary>
    public class TestUnit : MonoBehaviour
    {
        [Header("Unit Stats")]
        public Vector2Int gridPosition = new(2, 2);
        public int movementPoints = 3;
        public MovementType movementType = MovementType.Foot;
        public int attackRangeMin = 1;
        public int attackRangeMax = 1;

        [Header("Turn State")]
        public bool hasActed;
        public Vector2Int preMovementPosition;

        private SpriteRenderer _spriteRenderer;
        private Color _normalColor;

        private void Start()
        {
            _spriteRenderer = GetComponentInChildren<SpriteRenderer>();
            if (_spriteRenderer != null)
                _normalColor = _spriteRenderer.color;
            SnapToGridPosition();
        }

        public void MarkActed()
        {
            hasActed = true;
            if (_spriteRenderer != null)
                _spriteRenderer.color = new Color(0.4f, 0.4f, 0.4f, 0.7f);
        }

        public void ResetActed()
        {
            hasActed = false;
            if (_spriteRenderer != null)
                _spriteRenderer.color = _normalColor;
        }

        public void SnapToGridPosition()
        {
            transform.position = new Vector3(gridPosition.x + 0.5f, gridPosition.y + 0.5f, 0f);
        }

        private void OnValidate()
        {
            SnapToGridPosition();
        }
    }
}
