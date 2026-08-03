using UnityEngine;

namespace ProjectAstra.Core.Environment
{
    // Sets an Animator's playback speed once at startup, optionally to a random
    // value in a range. Drop it on a decoration to slow a lazy flag or speed up
    // a flicker, or to give a row of identical props slightly different tempos
    // so they don't feel machine-made.
    [RequireComponent(typeof(Animator))]
    public sealed class AnimatorSpeed : MonoBehaviour
    {
        [SerializeField] private float speed = 1f;
        [SerializeField] private bool randomize;
        [SerializeField] private Vector2 speedRange = new(0.8f, 1.2f);

        private void Awake()
        {
            float value = randomize ? Random.Range(speedRange.x, speedRange.y) : speed;
            GetComponent<Animator>().speed = value;
        }
    }
}
