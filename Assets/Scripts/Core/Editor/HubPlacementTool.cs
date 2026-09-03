using UnityEditor;
using UnityEngine;
using ProjectAstra.Core.Hub;

namespace ProjectAstra.Core.Editor
{
    // Turns an armed palette entry into a click in the scene: the thing follows the mouse, and
    // clicking leaves it there.
    [InitializeOnLoad]
    public static class HubPlacementTool
    {
        private static readonly Color GhostTint = new(1f, 1f, 1f, 0.65f);
        private static readonly Color FootprintColour = new(1f, 0.35f, 0.3f, 0.9f);

        static HubPlacementTool() => SceneView.duringSceneGui += Handle;

        private static void Handle(SceneView view)
        {
            HubPalette.Entry entry = HubSceneOverlay.Armed;
            HubRoom room = HubEditing.EditingRoom;
            if (entry == null || room == null || Application.isPlaying) return;

            Event input = Event.current;
            Vector3 at = HubPlacement.SnapToPixel(MouseInWorld(input));

            DrawGhost(entry, at);

            // Claims the click before the scene's own selection does, so placing never selects the
            // ground behind what is being placed.
            if (input.type == EventType.Layout)
                HandleUtility.AddDefaultControl(GUIUtility.GetControlID(FocusType.Passive));

            if (IsPlacingClick(input))
            {
                HubPlacement.Place(entry, room, at);
                if (!input.shift) HubSceneOverlay.Disarm();
                input.Use();
            }
            else if (IsCancel(input))
            {
                HubSceneOverlay.Disarm();
                input.Use();
            }

            view.Repaint();
        }

        // Holding shift keeps the entry armed, for laying down a row of fence posts.
        private static bool IsPlacingClick(Event input) =>
            input.type == EventType.MouseDown && input.button == 0 && !input.alt;

        private static bool IsCancel(Event input) =>
            (input.type == EventType.KeyDown && input.keyCode == KeyCode.Escape) ||
            (input.type == EventType.MouseDown && input.button == 1);

        private static Vector2 MouseInWorld(Event input)
        {
            Ray ray = HandleUtility.GUIPointToWorldRay(input.mousePosition);
            if (Mathf.Approximately(ray.direction.z, 0f)) return ray.origin;

            return ray.origin + ray.direction * (-ray.origin.z / ray.direction.z);
        }

        private static void DrawGhost(HubPalette.Entry entry, Vector3 at)
        {
            Sprite sprite = PreviewSprite(entry);
            if (sprite != null) DrawSprite(sprite, HubPlacement.OriginFor(entry, at));

            if (entry.blocks && entry.prefab == null) DrawFootprint(entry, at);
        }

        private static Sprite PreviewSprite(HubPalette.Entry entry)
        {
            if (entry.sprite != null) return entry.sprite;

            var renderer = entry.prefab != null ? entry.prefab.GetComponentInChildren<SpriteRenderer>() : null;
            return renderer != null ? renderer.sprite : null;
        }

        private static void DrawSprite(Sprite sprite, Vector3 origin)
        {
            Bounds local = sprite.bounds;
            Vector2 topLeft = HandleUtility.WorldToGUIPoint(origin + new Vector3(local.min.x, local.max.y));
            Vector2 bottomRight = HandleUtility.WorldToGUIPoint(origin + new Vector3(local.max.x, local.min.y));

            Handles.BeginGUI();
            GUI.color = GhostTint;
            GUI.DrawTextureWithTexCoords(
                Rect.MinMaxRect(topLeft.x, topLeft.y, bottomRight.x, bottomRight.y),
                sprite.texture, TexCoordsOf(sprite), true);
            GUI.color = Color.white;
            Handles.EndGUI();
        }

        // Where this sprite sits inside its texture, so one packed into a sheet still previews right.
        private static Rect TexCoordsOf(Sprite sprite)
        {
            Rect area = sprite.textureRect;
            Texture texture = sprite.texture;

            return new Rect(area.x / texture.width, area.y / texture.height,
                area.width / texture.width, area.height / texture.height);
        }

        private static void DrawFootprint(HubPalette.Entry entry, Vector3 at)
        {
            Vector2 size = entry.footprint;
            var box = new Rect(at.x - size.x * 0.5f, at.y, size.x, size.y);

            Handles.color = FootprintColour;
            Handles.DrawSolidRectangleWithOutline(new[]
            {
                new Vector3(box.xMin, box.yMin), new Vector3(box.xMax, box.yMin),
                new Vector3(box.xMax, box.yMax), new Vector3(box.xMin, box.yMax)
            }, new Color(1f, 0.35f, 0.3f, 0.15f), FootprintColour);
        }
    }
}
