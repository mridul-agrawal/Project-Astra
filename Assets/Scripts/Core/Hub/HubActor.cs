using UnityEngine;
using ProjectAstra.Core.Animation;

namespace ProjectAstra.Core.Hub
{
    // Anyone who stands in the hub: the protagonist and every character she can walk up to.
    public sealed class HubActor : MonoBehaviour
    {
        [Tooltip("Blocking box relative to the feet, in tiles. Narrower than the sprite so she can stand close to walls without her shoulders stopping her.")]
        [SerializeField] private Rect footprintOffset = new(-0.25f, 0f, 0.5f, 0.25f);

        [SerializeField] private Facing facing = Facing.South;

        private UnitAnimator unitAnimator;
        private bool solid;
        private Rect stamped;

        // A float in tiles, measured at the feet, with the transform following it. That is the whole
        // reason this isn't TestUnit, whose position snaps back to a tile centre in half a dozen places.
        public Vector2 Position { get; private set; }
        public Facing Facing => facing;
        public Rect FootprintOffset => footprintOffset;
        public Rect Footprint => HubMover.FootprintAt(Position, footprintOffset);

        // The character this actor is playing, so conversations and objectives can name them.
        public string CharacterId { get; private set; }

        // What talking to them opens right now. Empty means they are present but not interactable.
        public string ConversationId { get; private set; }

        private void Awake() => Position = transform.position;

        private void OnEnable() => HubWorld.Register(this);

        private void OnDisable()
        {
            SetSolid(false);
            HubWorld.Unregister(this);
        }

        public void Bind(string characterId, string conversationId = null)
        {
            CharacterId = characterId;
            ConversationId = conversationId;
        }

        public void SetConversation(string conversationId) => ConversationId = conversationId;

        public void Place(Vector2 position, Facing newFacing)
        {
            SetFacing(newFacing);
            SetPosition(position);
        }

        public void SetPosition(Vector2 position)
        {
            Position = position;
            transform.position = new Vector3(position.x, position.y, transform.position.z);
            if (solid) RestampFootprint();
        }

        // Facing is driven by what the player pressed, not by where the sprite ended up — walking
        // into a wall produces no movement but must still turn her to face it.
        public void SetFacing(Facing newFacing)
        {
            facing = newFacing;
            Animator()?.SetFacingOverride(newFacing);
        }

        // Looked up on first use rather than in Awake: the factory adds this component before it
        // attaches the sprite child, so an Awake-time lookup would cache nothing and the facing
        // would never reach the animator.
        private UnitAnimator Animator()
        {
            if (unitAnimator == null) unitAnimator = GetComponentInChildren<UnitAnimator>();
            return unitAnimator;
        }

        // Characters block movement, so she can walk up to one but not through or onto it. The
        // protagonist is left non-solid — she would only ever collide with herself.
        public void SetSolid(bool value)
        {
            if (value == solid) return;
            solid = value;

            if (value) StampFootprint();
            else UnstampFootprint();
        }

        private void RestampFootprint()
        {
            UnstampFootprint();
            StampFootprint();
        }

        private void StampFootprint()
        {
            if (HubLocationService.Instance == null) return;
            stamped = Footprint;
            HubLocationService.Instance.Collision.Stamp(stamped);
        }

        private void UnstampFootprint()
        {
            if (HubLocationService.Instance == null || stamped.width <= 0f) return;
            HubLocationService.Instance.Collision.Unstamp(stamped);
            stamped = default;
        }
    }
}
