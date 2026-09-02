using UnityEngine;

namespace ProjectAstra.Core.Hub
{
    // Where something in the world sits on screen, and which way to point when it is off screen.
    public static class HubScreenSpace
    {
        public const float GameplayWidth = 480f;
        public const float GameplayHeight = 270f;
        public const float CanvasWidth = 1920f;
        public const float CanvasHeight = 1080f;
        public const float PixelsPerTile = 32f;

        // Multiply a length authored in gameplay pixels to get canvas pixels.
        public const float CanvasScale = CanvasWidth / GameplayWidth;

        public static float ToCanvas(float gameplayPixels) => gameplayPixels * CanvasScale;

        // Canvas position of a world point, with the canvas origin at the bottom-left. Off-screen
        // points come back outside the canvas rect rather than clamped, so the caller can tell.
        public static Vector2 WorldToCanvas(Vector2 world, Vector2 cameraCentre, Vector2 viewSizeTiles)
        {
            Vector2 fromCorner = world - (cameraCentre - viewSizeTiles * 0.5f);
            return new Vector2(
                fromCorner.x / viewSizeTiles.x * CanvasWidth,
                fromCorner.y / viewSizeTiles.y * CanvasHeight);
        }

        public static bool IsOnScreen(Vector2 world, Vector2 cameraCentre, Vector2 viewSizeTiles)
        {
            Vector2 offset = world - cameraCentre;
            return Mathf.Abs(offset.x) <= viewSizeTiles.x * 0.5f &&
                   Mathf.Abs(offset.y) <= viewSizeTiles.y * 0.5f;
        }
    }

    // Where on the screen edge to draw a nudge toward something the player can't see.
    public static class EdgeIndicatorSolver
    {
        // Where the line from the middle of the screen toward the target crosses the edge, pulled in
        // by inset so the marker sits inside the frame rather than half off it.
        public static bool TrySolve(Vector2 target, Vector2 cameraCentre, Vector2 viewSizeTiles,
            float insetCanvasPixels, out Vector2 canvasPosition, out Vector2 direction)
        {
            canvasPosition = default;
            direction = default;

            if (HubScreenSpace.IsOnScreen(target, cameraCentre, viewSizeTiles)) return false;

            Vector2 offset = target - cameraCentre;
            if (offset.sqrMagnitude <= Mathf.Epsilon) return false;

            direction = offset.normalized;

            float halfWidth = HubScreenSpace.CanvasWidth * 0.5f - insetCanvasPixels;
            float halfHeight = HubScreenSpace.CanvasHeight * 0.5f - insetCanvasPixels;

            // Scaled to canvas space first, so the edge hit is worked out in the aspect the player
            // actually sees rather than in tiles.
            var inCanvas = new Vector2(
                offset.x / viewSizeTiles.x * HubScreenSpace.CanvasWidth,
                offset.y / viewSizeTiles.y * HubScreenSpace.CanvasHeight);

            float scale = ShortestEdgeHit(inCanvas, halfWidth, halfHeight);
            Vector2 fromCentre = inCanvas * scale;

            canvasPosition = new Vector2(
                HubScreenSpace.CanvasWidth * 0.5f + fromCentre.x,
                HubScreenSpace.CanvasHeight * 0.5f + fromCentre.y);
            return true;
        }

        // How far along the ray the first edge is hit — the nearer of the vertical and horizontal
        // crossings, so a target up and to the left lands on whichever edge it actually leaves by.
        private static float ShortestEdgeHit(Vector2 ray, float halfWidth, float halfHeight)
        {
            float horizontal = Mathf.Abs(ray.x) > Mathf.Epsilon ? halfWidth / Mathf.Abs(ray.x) : float.MaxValue;
            float vertical = Mathf.Abs(ray.y) > Mathf.Epsilon ? halfHeight / Mathf.Abs(ray.y) : float.MaxValue;
            return Mathf.Min(horizontal, vertical);
        }
    }
}
