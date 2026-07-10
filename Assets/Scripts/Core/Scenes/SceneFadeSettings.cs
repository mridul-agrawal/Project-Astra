using UnityEngine;

namespace ProjectAstra.Core.Scenes
{
    [CreateAssetMenu(fileName = "SceneFadeSettings", menuName = "Project Astra/Core/Scene Fade Settings")]
    public class SceneFadeSettings : ScriptableObject
    {
        [SerializeField] private float fadeDuration = 0.35f;
        [SerializeField] private float maxFadeStep = 1f / 30f;
        [SerializeField] private float opaque = 1f;
        [SerializeField] private float transparent = 0f;

        public float FadeDuration => fadeDuration;
        public float MaxFadeStep => maxFadeStep;
        public float Opaque => opaque;
        public float Transparent => transparent;
    }
}
