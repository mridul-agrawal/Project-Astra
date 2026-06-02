using UnityEngine;

namespace ProjectAstra.Core.Audio
{
    [CreateAssetMenu(menuName = "Project Astra/Audio/Sound")]
    public class SoundSO : ScriptableObject
    {
        [SerializeField] private AudioClip[] _clips;
        [SerializeField] private AudioBus _bus = AudioBus.Sfx;

        [Range(0f, 1f)]
        [SerializeField] private float _volume = 1f;

        [Tooltip("Random pitch is picked between x and y. Keep both at 1 for no variation.")]
        [SerializeField] private Vector2 _pitchRange = Vector2.one;

        [SerializeField] private bool _loop;

        public AudioBus Bus => _bus;
        public float Volume => _volume;
        public bool Loop => _loop;
        public bool HasClip => _clips != null && _clips.Length > 0;

        public AudioClip PickClip()
        {
            if (!HasClip) return null;
            return _clips[Random.Range(0, _clips.Length)];
        }

        public float PickPitch() => Random.Range(_pitchRange.x, _pitchRange.y);
    }
}
