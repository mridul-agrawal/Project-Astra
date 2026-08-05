using UnityEngine;

namespace ProjectAstra.Core.Cursor
{
    // One of the eight cursor elements: a pooled GameObject plus a SpriteRenderer, and the
    // morph that carries it from wherever it currently is to wherever the modules now want it.
    //
    // A morph always restarts from the CURRENT interpolated pose, never from the previous
    // target. That is the whole trick behind scrubbing across a row of units at held-repeat
    // speed without queueing or flicker — a retarget mid-flight is just a new start pose.
    public class CursorPiece
    {
        private readonly Transform pieceTransform;
        private readonly SpriteRenderer pieceRenderer;

        private CursorPose current = CursorPose.Hidden;
        private CursorPose morphFrom = CursorPose.Hidden;
        private CursorPose morphTo = CursorPose.Hidden;
        private float morphElapsed;
        private float morphDuration;
        private int arcDirection;

        public CursorPiece(Transform parent, string name, int sortingOrder)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            pieceTransform = go.transform;

            pieceRenderer = go.AddComponent<SpriteRenderer>();
            pieceRenderer.sortingLayerName = "UIOverlay";
            pieceRenderer.sortingOrder = sortingOrder;
            pieceRenderer.enabled = false;
        }

        public CursorPose Current => current;

        public void SetSprite(Sprite sprite) => pieceRenderer.sprite = sprite;

        public void SetVisible(bool visible) => pieceRenderer.enabled = visible;

        public void Destroy()
        {
            if (pieceTransform != null)
                Object.Destroy(pieceTransform.gameObject);
        }

        // arcDirection 0 blends in a straight line; +1/-1 sweeps that way around the tile.
        public void MorphTo(in CursorPose pose, float duration, int arcDirection = 0)
        {
            if (duration <= 0f)
            {
                current = pose;
                morphElapsed = morphDuration = 0f;
                return;
            }

            morphFrom = current;
            morphTo = pose;
            morphDuration = duration;
            morphElapsed = 0f;
            this.arcDirection = arcDirection;
        }

        public void Tick(float deltaTime)
        {
            if (morphDuration <= 0f) return;

            morphElapsed += deltaTime;
            float t = Mathf.Clamp01(morphElapsed / morphDuration);
            float eased = Mathf.SmoothStep(0f, 1f, t);

            current = arcDirection != 0
                ? CursorPose.PolarLerp(morphFrom, morphTo, eased, arcDirection)
                : CursorPose.Lerp(morphFrom, morphTo, eased);

            if (t >= 1f) morphDuration = 0f;
        }

        // The idle breath rides on top of the morph rather than being baked into it, so a
        // piece keeps breathing while it travels and the two never fight over the transform.
        public void Apply(Vector2 breathOffset, float breathScale)
        {
            if (!current.visible)
            {
                if (pieceRenderer.enabled) pieceRenderer.enabled = false;
                return;
            }

            if (!pieceRenderer.enabled) pieceRenderer.enabled = true;

            pieceTransform.localPosition = current.offset + breathOffset;
            pieceTransform.localRotation = Quaternion.Euler(0f, 0f, current.rotation);
            pieceTransform.localScale = Vector3.one * (current.scale * breathScale);
            pieceRenderer.color = current.tint;
        }
    }
}
