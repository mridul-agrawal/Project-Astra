using UnityEngine;

namespace ProjectAstra.Core.Gurukul
{
    // Orders a sprite by how far down the screen it stands, so the protagonist passes behind a tree
    // whose base is below her and in front of one whose base is above.
    //
    // Done per renderer rather than by switching the project's transparency sort to a custom axis:
    // that setting lives in both GraphicsSettings and Renderer2D, no test guards it, and flipping it
    // would silently reorder every sprite on the battle map too.
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

        private void Awake() => spriteRenderer = GetComponent<SpriteRenderer>();

        private void Start() => Apply();

        private void LateUpdate()
        {
            if (updatesEveryFrame) Apply();
        }

        private void Apply()
        {
            spriteRenderer.sortingLayerName = sortingLayer;
            float baseline = transform.position.y + baselineOffset;
            spriteRenderer.sortingOrder = Mathf.Clamp(Mathf.RoundToInt(-baseline * StepsPerTile), -Limit, Limit);
        }
    }
}
