using UnityEngine;

namespace ProjectAstra.Core.Dialogue
{
    // Global, tweakable dialogue knobs. For now just the text-crawl speed; the
    // Settings-menu slider that drives it at runtime is a later hookup.
    [CreateAssetMenu(fileName = "DialogueSettings", menuName = "Project Astra/Dialogue/Dialogue Settings")]
    public class DialogueSettings : ScriptableObject
    {
        private const float MinCharsPerSecond = 1f;

        [Tooltip("Default text-crawl speed in characters per second, when a line doesn't override it.")]
        [SerializeField] private float _charsPerSecond = 40f;

        public float CharsPerSecond => Mathf.Max(MinCharsPerSecond, _charsPerSecond);
    }
}
