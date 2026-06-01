using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace ProjectAstra.Core.UI.CombatAnimation
{
    // Ref-holder on the rebuilt CombatAnimation overlay prefab root. The
    // CombatPlaybackController reads these to drive the per-hit phase
    // coroutines. Public fields mirror the CombatForecastRefs pattern.
    public class CombatSceneRefs : MonoBehaviour
    {
        [Header("Fighter slots")]
        public CombatFighterView LeftFighter;   // Attacker
        public CombatFighterView RightFighter;  // Defender

        [Header("Scene chrome")]
        public Image TerrainBackground;         // Full-screen behind everything; sprite set per defender's terrain (Phase F)
        public Image DimOverlay;                // Optional darkening Image over the terrain bg
        public Image FullScreenFlash;           // White (or tinted) Image, alpha 0 — used by crit flash

        [Header("Optional labels")]
        public TextMeshProUGUI HeaderBanner;    // Optional title / scene banner
    }
}
