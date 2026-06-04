using UnityEngine;

namespace ProjectAstra.Core.Audio
{
    // One playable sound: the clip (or clips to pick from) and how to play it.
    [CreateAssetMenu(menuName = "Project Astra/Audio/Sound")]
    public class SoundSO : ScriptableObject
    {
        [SerializeField] private AudioClip[] clips;
        [SerializeField] private AudioBus bus = AudioBus.Sfx;

        [Range(0f, 1f)]
        [SerializeField] private float volume = 1f;

        [Tooltip("Random pitch is picked between x and y. Keep both at 1 for no variation.")]
        [SerializeField] private Vector2 pitchRange = Vector2.one;

        public AudioBus Bus => bus;
        public float Volume => volume;
        public bool HasClip => clips != null && clips.Length > 0;

        public AudioClip PickClip()
        {
            if (!HasClip) return null;
            return clips[Random.Range(0, clips.Length)];
        }

        public float PickPitch() => Random.Range(pitchRange.x, pitchRange.y);
    }
}
