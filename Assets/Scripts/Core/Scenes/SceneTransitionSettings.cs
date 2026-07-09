using UnityEngine;

namespace ProjectAstra.Core.Scenes
{
    // Functional (non-visual) tuning for scene-transition fades. Kept out of the ScreenFader
    // prefab on purpose: timing is functional data, not a look, so a designer can tune it here
    // without opening the overlay. The overlay's appearance (colour, contents) lives on the
    // prefab; how long the fade takes lives here.
    [CreateAssetMenu(fileName = "SceneTransitionSettings", menuName = "Project Astra/Core/Scene Transition Settings")]
    public class SceneTransitionSettings : ScriptableObject
    {
        [Tooltip("Seconds for the fade-out, and again for the fade-in (each direction, not the total).")]
        [SerializeField] private float fadeDuration = 0.35f;

        public float FadeDuration => fadeDuration;
    }
}
