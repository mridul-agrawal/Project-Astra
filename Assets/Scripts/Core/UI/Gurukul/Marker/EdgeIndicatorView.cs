using System.Collections.Generic;
using TMPro;
using UnityEngine;

namespace ProjectAstra.Core.UI.Gurukul.Marker
{
    public readonly struct EdgeIndicator
    {
        public readonly Vector2 CanvasPosition;
        public readonly Vector2 Direction;

        public EdgeIndicator(Vector2 canvasPosition, Vector2 direction)
        {
            CanvasPosition = canvasPosition;
            Direction = direction;
        }
    }

    // Points at the objective targets that are currently off screen.
    //
    // Kept restrained on purpose: the spec allows an edge indicator but rules out a minimap, a
    // compass, distance numbers and a route line, so this is an arrow on the edge and nothing else.
    public sealed class EdgeIndicatorView : MonoBehaviour
    {
        public GameObject content;
        public RectTransform indicatorRoot;
        public GameObject indicatorTemplate;

        private readonly List<RectTransform> pool = new();

        public void Render(IReadOnlyList<EdgeIndicator> indicators)
        {
            bool any = indicators.Count > 0;
            if (content != null) content.SetActive(any);
            if (!any || indicatorTemplate == null || indicatorRoot == null) return;

            EnsurePool(indicators.Count);
            for (int i = 0; i < pool.Count; i++) Place(pool[i], i < indicators.Count ? indicators[i] : (EdgeIndicator?)null);
        }

        // The arrow is rotated to point outward along the same ray the solver used, so it reads as
        // "that way" rather than just "something is over there".
        private static void Place(RectTransform arrow, EdgeIndicator? indicator)
        {
            arrow.gameObject.SetActive(indicator.HasValue);
            if (!indicator.HasValue) return;

            arrow.anchoredPosition = indicator.Value.CanvasPosition;

            float degrees = Mathf.Atan2(indicator.Value.Direction.y, indicator.Value.Direction.x) * Mathf.Rad2Deg;
            arrow.localRotation = Quaternion.Euler(0f, 0f, degrees - 90f);
        }

        private void EnsurePool(int needed)
        {
            while (pool.Count < needed)
            {
                GameObject clone = Instantiate(indicatorTemplate, indicatorRoot);
                clone.name = $"EdgeIndicator_{pool.Count}";
                clone.SetActive(true);
                pool.Add(clone.GetComponent<RectTransform>());
            }
        }
    }
}
