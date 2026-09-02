using UnityEngine;
using ProjectAstra.Core.Animation;
using ProjectAstra.Core.Units;

namespace ProjectAstra.Core.Hub
{
    // Builds a hub actor out of a character's authored UnitDefinition.
    public static class HubActorFactory
    {
        private const string SpriteChildName = "Sprite";

        public static HubActor Create(UnitDefinition definition, Transform parent, Vector2 position,
            Facing facing, string conversationId = null, bool solid = true)
        {
            if (definition == null)
            {
                Debug.LogError("[HubActorFactory] Cannot build an actor from a missing UnitDefinition.");
                return null;
            }

            var root = new GameObject(definition.UnitName);
            root.transform.SetParent(parent, false);

            var actor = root.AddComponent<HubActor>();
            AttachSprite(root, definition);

            actor.Bind(definition.UnitId, conversationId);
            actor.Place(position, facing);

            // Placed before being made solid, so the footprint is stamped where they actually stand.
            actor.SetSolid(solid);
            return actor;
        }

        // Sprite, animator and depth sorting all live on a child so UnitAnimator can read the root's
        // movement — which is exactly what makes it work unchanged for continuous walking.
        private static void AttachSprite(GameObject root, UnitDefinition definition)
        {
            var spriteGo = new GameObject(SpriteChildName);
            spriteGo.transform.SetParent(root.transform, false);

            var renderer = spriteGo.AddComponent<SpriteRenderer>();
            renderer.sprite = definition.MapSprite;
            spriteGo.AddComponent<YSortRenderer>();

            if (definition.MapAnimator == null) return;

            var animator = spriteGo.AddComponent<Animator>();
            animator.runtimeAnimatorController = definition.MapAnimator;
            animator.applyRootMotion = false;
            spriteGo.AddComponent<UnitAnimator>();
        }
    }
}
