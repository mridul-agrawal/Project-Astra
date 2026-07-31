using UnityEngine;
using ProjectAstra.Core.Animation;

namespace ProjectAstra.Core.Grid
{
    // Builds one runtime GameObject from an authored EnvironmentDecoration: a
    // SpriteRenderer (seeded with the preview frame, tiled for base-layer patches),
    // an Animator playing the loop, and the ambient / waypoint behaviours configured
    // from data. The "first frame" work is baked into previewSprite by the editor,
    // so this stays free of editor-only AnimationUtility.
    public static class EnvironmentDecorationSpawner
    {
        public static GameObject Spawn(EnvironmentDecoration deco, Transform parent)
        {
            var go = new GameObject(string.IsNullOrEmpty(deco.id) ? "Decoration" : deco.id);
            go.transform.SetParent(parent, false);
            go.transform.localPosition = new Vector3(deco.position.x, deco.position.y, 0f);

            AttachRenderer(go, deco);
            AttachAnimator(go, deco);
            return go;
        }

        private static void AttachRenderer(GameObject go, EnvironmentDecoration deco)
        {
            var renderer = go.AddComponent<SpriteRenderer>();
            renderer.sprite = deco.previewSprite;
            renderer.sortingLayerName = string.IsNullOrEmpty(deco.sortingLayer) ? "Object" : deco.sortingLayer;
            renderer.sortingOrder = deco.sortingOrder;

            if (deco.tileSize != Vector2.zero)
            {
                renderer.drawMode = SpriteDrawMode.Tiled;
                renderer.size = deco.tileSize;
            }
        }

        private static void AttachAnimator(GameObject go, EnvironmentDecoration deco)
        {
            var animator = go.AddComponent<Animator>();
            animator.runtimeAnimatorController = deco.animator;
            animator.applyRootMotion = false;

            go.AddComponent<AmbientAnimator>().Configure(deco.phaseOffset, deco.useUnscaledTime);

            if (deco.moves)
                go.AddComponent<WaypointMover>().Configure(
                    new Vector3(deco.waypointOffset.x, deco.waypointOffset.y, 0f), deco.moveSpeed, deco.flipToFaceTravel);
        }
    }
}
