using UnityEditor;
using UnityEngine;
using ProjectAstra.Core.Hub;

namespace ProjectAstra.Core.Editor
{
    // Puts a palette entry into a room already set up the way the game expects it.
    public static class HubPlacement
    {
        // Art is drawn at 32 px to the tile, so half a pixel off is visible. Everything lands on
        // that grid whatever the mouse did.
        private const float PixelsPerTile = 32f;

        public static GameObject Place(HubPalette.Entry entry, HubRoom room, Vector2 at)
        {
            if (entry == null || !entry.IsUsable || room == null) return null;

            GameObject placed = entry.prefab != null ? FromPrefab(entry) : FromSprite(entry);

            placed.transform.SetParent(GroupFor(entry, room), false);
            placed.transform.position = OriginFor(entry, SnapToPixel(at));

            Undo.RegisterCreatedObjectUndo(placed, $"Place {entry.label}");
            Selection.activeGameObject = placed;
            return placed;
        }

        public static Vector3 SnapToPixel(Vector2 at) => new(
            Mathf.Round(at.x * PixelsPerTile) / PixelsPerTile,
            Mathf.Round(at.y * PixelsPerTile) / PixelsPerTile,
            0f);

        // Where the transform goes so the art lands where the mouse is. Art is pivoted differently
        // from piece to piece, and a designer should never have to know which.
        public static Vector3 OriginFor(HubPalette.Entry entry, Vector3 at) =>
            entry.prefab != null ? at : at - (Vector3)AnchorIn(entry);

        // The point on the art the mouse is holding: a floor by its corner, so pieces tile; anything
        // else by the spot it stands on.
        private static Vector2 AnchorIn(HubPalette.Entry entry)
        {
            Bounds art = entry.sprite.bounds;

            return entry.kind == HubPalette.Kind.Ground
                ? new Vector2(art.min.x, art.min.y)
                : new Vector2(art.center.x, art.min.y);
        }

        // A reused thing keeps its link to the prefab, so fixing the prefab fixes every copy.
        private static GameObject FromPrefab(HubPalette.Entry entry) =>
            (GameObject)PrefabUtility.InstantiatePrefab(entry.prefab);

        private static GameObject FromSprite(HubPalette.Entry entry)
        {
            var built = new GameObject(entry.label);
            var renderer = built.AddComponent<SpriteRenderer>();
            renderer.sprite = entry.sprite;

            if (entry.kind == HubPalette.Kind.Ground) MakeGround(renderer);
            else MakeObject(built, renderer, entry);

            return built;
        }

        private static void MakeGround(SpriteRenderer renderer)
        {
            renderer.sortingLayerName = "Ground";
            renderer.sortingOrder = 0;
        }

        private static void MakeObject(GameObject built, SpriteRenderer renderer, HubPalette.Entry entry)
        {
            renderer.sortingLayerName = "Object";

            // Depth is measured from the base of the art rather than the pivot, so a tall tree still
            // hides her only once she is behind its trunk.
            var sorter = built.AddComponent<YSortRenderer>();
            sorter.MeasureFrom(AnchorIn(entry).y);

            if (entry.blocks) AddFootprint(built, entry);
        }

        // The box sits on the base of the art rather than around all of it, so she can walk behind a
        // tree instead of around its canopy.
        private static void AddFootprint(GameObject built, HubPalette.Entry entry)
        {
            built.layer = LayerMask.NameToLayer(PhysicsSolidSpace.SolidLayer);

            var box = built.AddComponent<BoxCollider2D>();
            box.size = entry.footprint;
            box.offset = AnchorIn(entry) + new Vector2(0f, entry.footprint.y * 0.5f);
        }

        private static Transform GroupFor(HubPalette.Entry entry, HubRoom room)
        {
            string name = entry.kind == HubPalette.Kind.Ground ? "Ground" : "Props";

            Transform group = room.transform.Find(name);
            if (group != null) return group;

            var made = new GameObject(name);
            made.transform.SetParent(room.transform, false);
            Undo.RegisterCreatedObjectUndo(made, "Add group");
            return made.transform;
        }
    }
}
