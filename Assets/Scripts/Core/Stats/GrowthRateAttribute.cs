using UnityEngine;

namespace ProjectAstra.Core.Stats
{
    // Marks a StatArray field to render as per-stat sliders over [Min, Max]. Used for unit personal
    // growths (0-100 % chance) and class growth modifiers (-100..100 signed deltas).
    public class GrowthRateAttribute : PropertyAttribute
    {
        public readonly int Min;
        public readonly int Max;

        public GrowthRateAttribute(int min = 0, int max = 100)
        {
            Min = min;
            Max = max;
        }
    }
}
