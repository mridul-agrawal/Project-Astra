using UnityEngine;

namespace ProjectAstra.Core.Units
{
    // Scene bootstrap that exposes the active UnitVisualSettings to static callers
    // (TestUnit's acted tint). Mirrors CombatAnimationSettingsRef. Place one per
    // battle scene; the serialized asset is shared across it.
    public class UnitVisualSettingsRef : MonoBehaviour
    {
        [SerializeField] private UnitVisualSettings asset;

        public static UnitVisualSettings Current { get; private set; }

        private void Awake() { if (asset != null) Current = asset; }
        private void OnDestroy() { if (Current == asset) Current = null; }
    }
}
