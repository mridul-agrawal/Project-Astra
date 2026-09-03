using UnityEngine;
using ProjectAstra.Core.Animation;

namespace ProjectAstra.Core.Hub.Interaction
{
    // Everything an interactable needs that has nothing to do with what it is.
    public abstract class InteractableBehaviour : MonoBehaviour, IInteractable
    {
        [Tooltip("Off for something reachable from any side — a noticeboard, a wide counter.")]
        [SerializeField] private bool requiresFacing = true;

        [Tooltip("Off for something visible over an obstacle it stands behind.")]
        [SerializeField] private bool requiresLineOfSight = true;

        private PlayerInteractionController registeredWith;

        // Not a RequireComponent, because Collider2D is abstract and Unity cannot add one for you.
        private void Awake()
        {
            if (GetComponent<Collider2D>() == null)
                Debug.LogError($"[{GetType().Name}] has no trigger collider, so nothing can reach it.", this);
        }

        public abstract HubVerb Verb { get; }
        public abstract bool IsAvailable { get; }
        public abstract InteractionPriority Priority { get; }
        public virtual Vector2 InteractionPoint => transform.position;

        protected static HubWorldFlags Flags => HubVisitService.Instance?.Flags;

        // Close enough is already answered by the trigger. This is the rest of it, and it is
        // virtual so a thing with its own idea of reach simply says so.
        public virtual bool CanReach(InteractorPose player)
        {
            if (requiresFacing && !InteractionReachRules.IsFacing(player, InteractionPoint)) return false;

            return !requiresLineOfSight ||
                   InteractionReachRules.HasLineOfSight(player.Position, InteractionPoint,
                       HubLocationService.Instance?.Collision);
        }

        public abstract void Interact(InteractorPose player);

        private void OnTriggerEnter2D(Collider2D other) => RegisterWith(ControllerOn(other));

        private void OnTriggerExit2D(Collider2D other)
        {
            if (ControllerOn(other) != null) Deregister();
        }

        // A room is destroyed wholesale on a doorway, and Unity does not raise an exit for a
        // trigger that stops existing — so leaving is said here too. Virtual because Unity calls
        // only the most-derived declaration of a message, so a subclass has to chain to this.
        protected virtual void OnDisable() => Deregister();

        private void RegisterWith(PlayerInteractionController controller)
        {
            if (controller == null || controller == registeredWith) return;

            registeredWith = controller;
            controller.Add(this);
        }

        private void Deregister()
        {
            registeredWith?.Remove(this);
            registeredWith = null;
        }

        private static PlayerInteractionController ControllerOn(Collider2D other) =>
            other != null ? other.GetComponentInParent<HubPlayerController>()?.Interaction : null;
    }
}
