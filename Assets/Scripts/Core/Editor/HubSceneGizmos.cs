using UnityEditor;
using UnityEngine;
using ProjectAstra.Core.Hub;
using ProjectAstra.Core.Hub.Interaction;

namespace ProjectAstra.Core.Editor
{
    // Draws what a designer needs to see about a room that its art does not show: where it ends,
    // what blocks, and what she can walk up to.
    [InitializeOnLoad]
    public static class HubSceneGizmos
    {
        private static readonly Color ExtentColour = new(0.45f, 0.75f, 1f, 0.9f);
        private static readonly Color BlockingColour = new(1f, 0.35f, 0.3f, 0.85f);
        private static readonly Color BlockingFill = new(1f, 0.35f, 0.3f, 0.12f);
        private static readonly Color ReachColour = new(1f, 0.82f, 0.25f, 0.9f);
        private static readonly Color ReachFill = new(1f, 0.82f, 0.25f, 0.08f);
        private static readonly Color SpawnColour = new(0.4f, 0.95f, 0.6f, 0.95f);

        private static GUIStyle labelStyle;

        static HubSceneGizmos() => SceneView.duringSceneGui += Draw;

        private static void Draw(SceneView view)
        {
            if (Application.isPlaying) return;

            HubRoom room = HubEditing.EditingRoom;
            if (room == null || SceneVisibilityManager.instance.IsHidden(room.gameObject)) return;

            if (HubEditing.Shows(HubEditing.Overlay.Extent)) DrawExtent(room);
            if (HubEditing.Shows(HubEditing.Overlay.Blocking)) DrawBlocking(room);
            if (HubEditing.Shows(HubEditing.Overlay.Interaction)) DrawInteractions(room);
        }

        // The room's own edge: the camera never shows past it and she cannot walk out of it.
        private static void DrawExtent(HubRoom room)
        {
            if (room.Location == null) return;
            Rect bounds = room.Location.Bounds;

            Handles.color = ExtentColour;
            Handles.DrawSolidRectangleWithOutline(Corners(bounds), Color.clear, ExtentColour);
            Label(new Vector3(bounds.xMin, bounds.yMax + 0.35f),
                $"{room.LocationId}   {bounds.width:0}×{bounds.height:0} tiles", ExtentColour);
        }

        private static void DrawBlocking(HubRoom room)
        {
            int solid = LayerMask.NameToLayer(PhysicsSolidSpace.SolidLayer);

            foreach (Collider2D collider in room.GetComponentsInChildren<Collider2D>(true))
            {
                if (collider.isTrigger || collider.gameObject.layer != solid) continue;

                Bounds b = collider.bounds;
                Handles.DrawSolidRectangleWithOutline(
                    Corners(new Rect(b.min.x, b.min.y, b.size.x, b.size.y)), BlockingFill, BlockingColour);
            }
        }

        // Where she has to be standing, and what the prompt would say. Decoration draws nothing, so
        // what is alive in a room reads at a glance.
        private static void DrawInteractions(HubRoom room)
        {
            foreach (InteractableBehaviour interactable in room.GetComponentsInChildren<InteractableBehaviour>(true))
            {
                Vector3 at = interactable.InteractionPoint;

                Handles.color = ReachFill;
                Handles.DrawSolidDisc(at, Vector3.forward, InteractionReachRules.DefaultReachTiles);
                Handles.color = ReachColour;
                Handles.DrawWireDisc(at, Vector3.forward, InteractionReachRules.DefaultReachTiles);

                Label(at + Vector3.up * (InteractionReachRules.DefaultReachTiles + 0.2f),
                    $"{interactable.Verb}  ·  {interactable.name}", ReachColour);
            }
        }

        public static void DrawSpawn(Vector2 at, string caption)
        {
            Handles.color = SpawnColour;
            Handles.DrawWireDisc(at, Vector3.forward, 0.4f);
            Handles.DrawLine(at + Vector2.left * 0.55f, at + Vector2.right * 0.55f);
            Handles.DrawLine(at + Vector2.down * 0.55f, at + Vector2.up * 0.55f);
            Label(new Vector3(at.x, at.y - 0.9f), caption, SpawnColour);
        }

        private static Vector3[] Corners(Rect r) => new Vector3[]
        {
            new(r.xMin, r.yMin), new(r.xMax, r.yMin), new(r.xMax, r.yMax), new(r.xMin, r.yMax)
        };

        private static void Label(Vector3 at, string text, Color colour)
        {
            labelStyle ??= new GUIStyle(EditorStyles.miniLabel) { fontSize = 10 };
            labelStyle.normal.textColor = colour;
            Handles.Label(at, text, labelStyle);
        }
    }
}
