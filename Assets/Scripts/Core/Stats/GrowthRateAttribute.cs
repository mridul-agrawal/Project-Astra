using UnityEngine;

namespace ProjectAstra.Core.Stats
{
    // Marks a StatArray field as growth rates — a 0-100 % chance per stat. The StatArray drawer
    // renders these cells as 0-100 sliders instead of raw int fields.
    public class GrowthRateAttribute : PropertyAttribute { }
}
