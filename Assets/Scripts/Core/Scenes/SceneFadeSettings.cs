using UnityEngine;

namespace ProjectAstra.Core.Scenes
{
    [CreateAssetMenu(fileName = "SceneFadeSettings", menuName = "Project Astra/Core/Scene Fade Settings")]
    public class SceneFadeSettings : ScriptableObject
    {
        [SerializeField] private float fadeDuration = 0.35f;

        public float FadeDuration => fadeDuration;
    }
}
