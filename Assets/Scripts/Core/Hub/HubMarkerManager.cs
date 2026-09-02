using System.Collections.Generic;
using TMPro;
using UnityEngine;
using ProjectAstra.Core.UI.Hub.Marker;

using ProjectAstra.Core.Hub.Interaction;

namespace ProjectAstra.Core.Hub
{
    // Points at everything the active objective still needs.
    //
    // One marker per target, drawn one of two ways: over its head while it is on screen, and as an
    // arrow on the screen edge while it isn't. They are the same thing to a player — "go here" —
    // so they are decided together rather than by two systems that could disagree.
    //
    // World markers are parented to what they mark, so a character who walks or is relocated carries
    // theirs along — the trick the battle map's HP bars use, and the reason nothing chases a moving
    // target.
    // Runs after HubCameraController, which moves in LateUpdate at 100. Reading the camera before it
    // has settled would decide what is off screen from where the camera was last frame — which on
    // the first frame is the origin, so every target would briefly read as off screen at once.
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

        private void Awake()
        {
            if (cameraRig == null) cameraRig = FindFirstObjectByType<HubCameraController>();
        }

        private void LateUpdate()
        {
            offScreen.Clear();

            HubObjectiveData active = ActiveObjectiveWhileExploring();
            if (active == null)
            {
                ClearAll();
                edgeIndicators?.Render(offScreen);
                return;
            }

            SyncMarkers(HubProgressService.Instance.Objectives, active);
            edgeIndicators?.Render(offScreen);
        }

        // Wayfinding belongs to free exploration. During a conversation or an event the spec wants
        // the screen clear, and a marker must never draw over a dialogue box.
        private HubObjectiveData ActiveObjectiveWhileExploring()
        {
            if (HubControlGate.Instance != null && !HubControlGate.Instance.AcceptsMovement) return null;
            return HubProgressService.Instance?.Objectives?.ActiveObjective;
        }

        private void SyncMarkers(ObjectiveSequenceRunner objectives, HubObjectiveData active)
        {
            foreach (string targetId in active.MarkerTargetIds)
            {
                if (!objectives.IsMarkerTargetOutstanding(targetId))
                {
                    Remove(targetId);
                    continue;
                }

                Transform anchor = AnchorFor(targetId);
                if (anchor == null) continue;

                if (TrySolveEdgeIndicator(anchor.position, out EdgeIndicator indicator))
                {
                    offScreen.Add(indicator);
                    Remove(targetId);
                    continue;
                }

                if (!markers.ContainsKey(targetId)) markers[targetId] = BuildMarker(anchor);
            }

            DropMarkersNotInThisObjective(active);
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
        private void DropMarkersNotInThisObjective(HubObjectiveData active)
        {
            stale.Clear();
            foreach (string shown in markers.Keys)
                if (System.Array.IndexOf(active.MarkerTargetIds, shown) < 0) stale.Add(shown);

            foreach (string targetId in stale) Remove(targetId);
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
