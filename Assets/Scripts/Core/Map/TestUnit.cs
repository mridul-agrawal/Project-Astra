using UnityEngine;

namespace ProjectAstra.Core
{
    /// <summary>
    /// Minimal test unit for exercising cursor UNIT_SELECTED and TARGETING modes.
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

        private void Start()
        {
            SnapToGridPosition();
        }

        private void SnapToGridPosition()
        {
            transform.position = new Vector3(gridPosition.x + 0.5f, gridPosition.y + 0.5f, 0f);
        }

        private void OnValidate()
        {
            SnapToGridPosition();
        }
    }
}
