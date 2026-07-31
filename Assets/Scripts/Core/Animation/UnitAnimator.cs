using UnityEngine;

namespace ProjectAstra.Core.Animation
{
    // Drives a unit's Animator from how the unit actually moves on the map.
    // Lives on the sprite child, but reads the ROOT transform's movement each
    // frame — so a flyer's hover bob (which only nudges the child) never reads
    // as walking. Movement becomes run/idle plus a facing; selection is toggled
    // from outside via SetSelected. Works for player, enemy, canto, knockback,
    // and scripted moves alike, since it only watches position.
    [RequireComponent(typeof(Animator))]
    public sealed class UnitAnimator : MonoBehaviour
    {
        private static readonly int MoveXHash = Animator.StringToHash("MoveX");
        private static readonly int MoveYHash = Animator.StringToHash("MoveY");
        private static readonly int MovingHash = Animator.StringToHash("Moving");
        private static readonly int SelectedHash = Animator.StringToHash("Selected");

        private Animator animator;
        private Transform root;
        private FacingClassifier facing;
        private Vector3 lastRootPosition;
        private bool hasLastPosition;
        private bool selected;

        private void Awake()
        {
            animator = GetComponent<Animator>();
            root = transform.parent != null ? transform.parent : transform;
            facing = new FacingClassifier();
        }

        private void OnEnable() => hasLastPosition = false;   // re-sync after any toggle

        public void SetSelected(bool value) => selected = value;

        private void LateUpdate()
        {
            Vector2 delta = SampleRootMovement();
            facing.Tick(delta, Time.deltaTime);
            PushToAnimator(LocomotionState.Select(facing.Moving, facing.Facing, selected));
        }

        // First frame after enable establishes a baseline with zero delta, so a
        // spawn snap or a re-enable never flashes a spurious run.
        private Vector2 SampleRootMovement()
        {
            Vector3 current = root.position;
            if (!hasLastPosition)
            {
                lastRootPosition = current;
                hasLastPosition = true;
            }
            Vector3 delta = current - lastRootPosition;
            lastRootPosition = current;
            return new Vector2(delta.x, delta.y);
        }

        private void PushToAnimator(LocomotionParams p)
        {
            animator.SetFloat(MoveXHash, p.MoveX);
            animator.SetFloat(MoveYHash, p.MoveY);
            animator.SetBool(MovingHash, p.Moving);
            animator.SetBool(SelectedHash, p.Selected);
        }
    }
}
