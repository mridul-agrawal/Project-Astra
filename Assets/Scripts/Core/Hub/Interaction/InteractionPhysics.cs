using UnityEngine;

namespace ProjectAstra.Core.Hub.Interaction
{
    // Builds the two colliders interaction needs, so their shapes are decided in one place.
    //
    // The player carries a small probe and the body that makes triggers fire at all; each
    // interactable carries the region she has to be standing in. Nothing here collides with
    // anything — walls are still HubCollisionMap's job.
    public static class InteractionPhysics
    {
        // Small enough that being "in range" means her feet are in the target's region rather than
        // her probe merely brushing it.
        private const float PlayerProbeRadius = 0.05f;

        // Kinematic because HubActor writes transform.position directly; a dynamic body would
        // fight it for control of where she is.
        public static void AttachPlayerProbe(GameObject player)
        {
            if (player.GetComponent<Rigidbody2D>() == null)
            {
                var body = player.AddComponent<Rigidbody2D>();
                body.bodyType = RigidbodyType2D.Kinematic;
                body.simulated = true;
                body.gravityScale = 0f;
            }

            if (player.GetComponent<Collider2D>() != null) return;

            var probe = player.AddComponent<CircleCollider2D>();
            probe.isTrigger = true;
            probe.radius = PlayerProbeRadius;
        }

        // The region she has to be inside for this to be a candidate. A hand-placed interactable
        // keeps whatever collider a designer drew; this is only for the ones built at runtime.
        public static CircleCollider2D AttachReachRegion(GameObject interactable, Vector2 offset,
            float radius = InteractionReachRules.DefaultReachTiles)
        {
            var existing = interactable.GetComponent<CircleCollider2D>();
            if (existing != null) return existing;

            var region = interactable.AddComponent<CircleCollider2D>();
            region.isTrigger = true;
            region.radius = radius;
            region.offset = offset;
            return region;
        }
    }
}
