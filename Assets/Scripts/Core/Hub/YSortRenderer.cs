using UnityEngine;

namespace ProjectAstra.Core.Hub
{
    // Orders a sprite by how far down the screen it stands, so she can pass behind a tree.
    // Per renderer, because the project's own sort axis would reorder the battle map too.
    [RequireComponent(typeof(SpriteRenderer))]
    public sealed class YSortRenderer : MonoBehaviour
    {
        // A hundred steps per tile is finer than anything can be placed at, and keeps the order well
        // inside the 16-bit range a sorting order allows.
        private const int StepsPerTile = 100;
        private const int Limit = 30000;

        [Tooltip("Hub sprites all share one layer so they can be ordered against each other. Units is battle-only, where sprites are always in front of props.")]
        [SerializeField] private string sortingLayer = "Object";

        [Tooltip("Off for scenery that never moves — it is ordered once and then left alone.")]
        [SerializeField] private bool updatesEveryFrame = true;

        [Tooltip("Raise for a sprite whose base sits above its transform, like a wall hung on a post.")]
        [SerializeField] private float baselineOffset;

        private SpriteRenderer spriteRenderer;

        // Set when something is placed, so sorting measures from the foot of the art whatever its
        // pivot happens to be.
        public void MeasureFrom(float localBaseline) => baselineOffset = localBaseline;

        private void Awake() => spriteRenderer = GetComponent<SpriteRenderer>();

        private void Start() => Apply();

        private void LateUpdate()
        {
            if (updatesEveryFrame) Apply();
        }

        // Public so a room being composed can be ordered the moment something is placed, rather
        // than looking flat until the game runs.
        public void Apply()
        {
            SpriteRenderer target = Renderer;
            if (target == null) return;

            target.sortingLayerName = sortingLayer;
            float baseline = transform.position.y + baselineOffset;
            target.sortingOrder = Mathf.Clamp(Mathf.RoundToInt(-baseline * StepsPerTile), -Limit, Limit);
        }

        private SpriteRenderer Renderer =>
            spriteRenderer != null ? spriteRenderer : spriteRenderer = GetComponent<SpriteRenderer>();
    }
}
