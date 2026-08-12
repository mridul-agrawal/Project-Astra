using UnityEngine;

namespace ProjectAstra.Core.UI.UnitInfo
{
    // §8 focus marker — one glow ring that slides between rows rather than a highlight per row.
    // It eases toward whatever rect it was last pointed at, and snaps instead while a held key
    // repeats so a fast scroll does not trail behind the selection.
    //
    // The marker stays parented to a single overlay container and reads its target's screen rect,
    // so rows in different groups (with different parents) need no coordinate juggling.
    [RequireComponent(typeof(RectTransform))]
    public sealed class UnitInfoFocusMarker : MonoBehaviour
    {
        [SerializeField] private float easeSeconds = 0.12f;

        private RectTransform self;
        private RectTransform target;
        private bool snapNext;

        private void Awake() => Bind();

        private void Bind()
        {
            if (self == null) self = GetComponent<RectTransform>();
        }

        public void Hide()
        {
            target = null;
            if (gameObject.activeSelf) gameObject.SetActive(false);
        }

        public void MoveTo(RectTransform destination, bool instant)
        {
            if (destination == null)
            {
                Hide();
                return;
            }

            Bind();
            bool wasHidden = !gameObject.activeSelf;
            target = destination;
            gameObject.SetActive(true);
            snapNext = instant || wasHidden;
            if (snapNext) Apply(1f);
        }

        private void LateUpdate()
        {
            if (target == null) return;

            float step = easeSeconds > 0f ? Time.unscaledDeltaTime / easeSeconds : 1f;
            Apply(snapNext ? 1f : Mathf.Clamp01(step * EaseWeight));
            snapNext = false;
        }

        // Chasing a share of the remaining distance each frame gives an ease-out for free;
        // the weight decides how big that share is.
        private const float EaseWeight = 2.2f;

        private void Apply(float weight)
        {
            Bind();
            if (!(self.parent is RectTransform frame)) return;

            Vector2 corner = TopLeftIn(frame, target);
            Vector2 size = target.rect.size;

            self.anchoredPosition = Vector2.Lerp(self.anchoredPosition, corner, weight);
            self.sizeDelta = Vector2.Lerp(self.sizeDelta, size, weight);
        }

        // Where the target's top-left corner falls inside the marker's own container. Both use a
        // top-left pivot, so the local point doubles as the anchored position.
        private static Vector2 TopLeftIn(RectTransform frame, RectTransform subject)
        {
            var corners = new Vector3[4];
            subject.GetWorldCorners(corners);
            return frame.InverseTransformPoint(corners[1]);
        }
    }
}
