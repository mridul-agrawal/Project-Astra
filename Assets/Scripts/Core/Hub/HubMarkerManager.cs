using System.Collections.Generic;
using TMPro;
using UnityEngine;
using ProjectAstra.Core.UI.Hub.Marker;

using ProjectAstra.Core.Hub.Interaction;
using ProjectAstra.Core.Quests;

namespace ProjectAstra.Core.Hub
{
    // Points at everything the active objective still needs, over its head or on the screen edge.
    [DefaultExecutionOrder(110)]
    public sealed class HubMarkerManager : MonoBehaviour
    {
        private const string MarkerText = "!";
        private const string SortingLayer = "UIOverlay";
        private const int SortingOrder = 200;
        private const float HeightAboveFeet = 1.1f;
        private const float CanvasScale = 0.018f;
        private const float EdgeInsetCanvasPixels = 48f;

        [SerializeField] private HubCameraController cameraRig;
        [SerializeField] private EdgeIndicatorView edgeIndicators;
        [SerializeField] private Color markerColor = new(1f, 0.85f, 0.3f, 1f);

        private readonly Dictionary<string, GameObject> markers = new();
        private readonly List<EdgeIndicator> offScreen = new();
        private readonly List<string> stale = new();
        private readonly HashSet<string> shown = new();

        private void Awake()
        {
            if (cameraRig == null) cameraRig = FindFirstObjectByType<HubCameraController>();
        }

        // Runs after HubCameraController, which moves at 100. Reading the camera before it settles
        // would judge what is off screen from where it stood last frame.
        // Runs after HubCameraController, which moves at 100. Reading the camera before it settles
        // would judge what is off screen from where it stood last frame.
        private void LateUpdate()
        {
            offScreen.Clear();

            if (!ExploringFreely())
            {
                ClearAll();
                edgeIndicators?.Render(offScreen);
                return;
            }

            SyncMarkers();
            edgeIndicators?.Render(offScreen);
        }

        // Wayfinding belongs to free exploration. During a conversation or an event the spec wants
        // the screen clear, and a marker must never draw over a dialogue box.
        private static bool ExploringFreely() =>
            HubControlGate.Instance == null || HubControlGate.Instance.AcceptsMovement;

        // The quest says what is still outstanding; the condition says who to stand over. A stage
        // that has nothing in the world to point at gets an authored list instead.
        private void SyncMarkers()
        {
            QuestRunner runner = QuestManager.Instance?.Runner;
            QuestObjective active = runner?.ActiveObjective;
            if (active == null)
            {
                ClearAll();
                return;
            }

            shown.Clear();
            foreach (string targetId in Wanted(runner, active))
            {
                string anchorId = runner.MarkerFor(targetId) ?? targetId;
                shown.Add(anchorId);
                Place(anchorId);
            }
            DropMarkersNotWanted();
        }

        private IEnumerable<string> Wanted(QuestRunner runner, QuestObjective active)
        {
            if (active.MarkerTargetIds.Length > 0) return active.MarkerTargetIds;
            return runner.OutstandingTargets();
        }

        private void Place(string anchorId)
        {
            Transform anchor = AnchorFor(anchorId);
            if (anchor == null) return;

            if (TrySolveEdgeIndicator(anchor.position, out EdgeIndicator indicator))
            {
                offScreen.Add(indicator);
                Remove(anchorId);
                return;
            }

            if (!markers.ContainsKey(anchorId)) markers[anchorId] = BuildMarker(anchor);
        }

        private bool TrySolveEdgeIndicator(Vector2 worldPosition, out EdgeIndicator indicator)
        {
            indicator = default;
            if (cameraRig == null || edgeIndicators == null) return false;

            if (!EdgeIndicatorSolver.TrySolve(worldPosition, cameraRig.Centre, cameraRig.ViewSizeTiles,
                    EdgeInsetCanvasPixels, out Vector2 canvasPosition, out Vector2 direction))
                return false;

            indicator = new EdgeIndicator(canvasPosition, direction);
            return true;
        }

        // Changing objective replaces the whole set, so anything left over from the last one goes.
        private void DropMarkersNotWanted()
        {
            stale.Clear();
            foreach (string anchorId in markers.Keys)
                if (!shown.Contains(anchorId)) stale.Add(anchorId);

            foreach (string anchorId in stale) Remove(anchorId);
        }

        private static Transform AnchorFor(string targetId)
        {
            HubActor actor = HubWorld.FindActor(targetId);
            if (actor != null) return actor.transform;

            InspectableInteractable inspectable = HubWorld.FindInspectable(targetId);
            return inspectable != null ? inspectable.transform : null;
        }

        // A world-space canvas on the overlay layer, built the way WorldMarker builds the battle
        // map's cinematic symbols.
        private GameObject BuildMarker(Transform anchor)
        {
            var go = new GameObject("ObjectiveMarker");
            go.transform.SetParent(anchor, false);
            go.transform.localPosition = new Vector3(0f, HeightAboveFeet, 0f);

            var canvas = go.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.WorldSpace;
            canvas.sortingLayerName = SortingLayer;
            canvas.sortingOrder = SortingOrder;

            var rect = canvas.GetComponent<RectTransform>();
            rect.sizeDelta = new Vector2(110f, 110f);
            rect.localScale = Vector3.one * CanvasScale;

            var textGo = new GameObject("Text", typeof(RectTransform));
            textGo.transform.SetParent(go.transform, false);

            var label = textGo.AddComponent<TextMeshProUGUI>();
            label.text = MarkerText;
            label.fontSize = 90f;
            label.fontStyle = FontStyles.Bold;
            label.alignment = TextAlignmentOptions.Center;
            label.color = markerColor;
            label.outlineColor = Color.black;
            label.outlineWidth = 0.25f;
            label.raycastTarget = false;

            var textRect = textGo.GetComponent<RectTransform>();
            textRect.anchorMin = Vector2.zero;
            textRect.anchorMax = Vector2.one;
            textRect.offsetMin = Vector2.zero;
            textRect.offsetMax = Vector2.zero;
            return go;
        }

        private void Remove(string targetId)
        {
            if (!markers.TryGetValue(targetId, out GameObject marker)) return;
            if (marker != null) Destroy(marker);
            markers.Remove(targetId);
        }

        private void ClearAll()
        {
            if (markers.Count == 0) return;

            foreach (GameObject marker in markers.Values)
                if (marker != null) Destroy(marker);
            markers.Clear();
        }

        private void OnDisable() => ClearAll();
    }
}
