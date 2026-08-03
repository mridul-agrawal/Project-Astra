using UnityEngine;

namespace ProjectAstra.Core.Units
{
    // Designer-tunable look of on-map units. Edit this asset in the Inspector — no
    // code changes. Read at runtime through UnitVisualSettingsRef. Room to grow
    // (selected / enemy tints, etc.) as more unit-visual knobs are wanted.
    [CreateAssetMenu(menuName = "Project Astra/Units/Unit Visual Settings")]
    public class UnitVisualSettings : ScriptableObject
    {
        [Tooltip("Sprite colour multiplier for a unit that has already acted this turn. RGB dims/tints (0.4,0.4,0.4 = grey to 40%); alpha fades (0.7 = 70% opaque).")]
        [SerializeField] private Color actedTint = new(0.4f, 0.4f, 0.4f, 0.7f);

        public Color ActedTint => actedTint;
    }
}
