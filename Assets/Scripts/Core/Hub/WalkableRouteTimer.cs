using System.Collections.Generic;
using UnityEngine;

namespace ProjectAstra.Core.Hub
{
    // How long it takes to walk somewhere, and whether it can be walked to at all.
    public static class WalkableRouteTimer
    {
        public static bool TryMeasureSeconds(HubCollisionMap map, Rect footprintOffset,
            Vector2 from, Vector2 to, out float seconds,
            float speedTilesPerSecond = HubMover.DefaultSpeedTilesPerSecond)
        {
            seconds = 0f;
            if (map == null || speedTilesPerSecond <= 0f) return false;

            if (!TryMeasureCells(map, footprintOffset, from, to, out int cells)) return false;

            seconds = cells * HubCollisionMap.CellSize / speedTilesPerSecond;
            return true;
        }

        public static bool TryMeasureCells(HubCollisionMap map, Rect footprintOffset,
            Vector2 from, Vector2 to, out int cells)
        {
            cells = 0;
            Vector2Int start = CellOf(from);
            Vector2Int goal = CellOf(to);

            if (!Fits(map, footprintOffset, start)) return false;
            if (start == goal) return true;

            var distance = new Dictionary<Vector2Int, int> { [start] = 0 };
            var queue = new Queue<Vector2Int>();
            queue.Enqueue(start);

            while (queue.Count > 0)
            {
                Vector2Int here = queue.Dequeue();
                foreach (Vector2Int next in Neighbours(here))
                {
                    if (distance.ContainsKey(next) || !Fits(map, footprintOffset, next)) continue;

                    distance[next] = distance[here] + 1;
                    if (next == goal)
                    {
                        cells = distance[next];
                        return true;
                    }
                    queue.Enqueue(next);
                }
            }
            return false;
        }

        // The goal often sits inside something solid — a character's own body, a door set into a
        // wall — so reaching a cell next to it counts as arriving.
        public static bool CanReachNeighbour(HubCollisionMap map, Rect footprintOffset,
            Vector2 from, Vector2 to, out float seconds,
            float speedTilesPerSecond = HubMover.DefaultSpeedTilesPerSecond)
        {
            seconds = 0f;
            float best = float.MaxValue;

            foreach (Vector2Int neighbour in Neighbours(CellOf(to)))
            {
                Vector2 target = CentreOf(neighbour);
                if (!TryMeasureSeconds(map, footprintOffset, from, target, out float candidate, speedTilesPerSecond)) continue;
                best = Mathf.Min(best, candidate);
            }

            if (best == float.MaxValue) return false;
            seconds = best;
            return true;
        }

        private static IEnumerable<Vector2Int> Neighbours(Vector2Int cell)
        {
            yield return new Vector2Int(cell.x + 1, cell.y);
            yield return new Vector2Int(cell.x - 1, cell.y);
            yield return new Vector2Int(cell.x, cell.y + 1);
            yield return new Vector2Int(cell.x, cell.y - 1);
        }

        private static bool Fits(HubCollisionMap map, Rect footprintOffset, Vector2Int cell) =>
            !map.IsRectBlocked(HubMover.FootprintAt(CentreOf(cell), footprintOffset));

        private static Vector2Int CellOf(Vector2 world) => new(
            Mathf.FloorToInt(world.x / HubCollisionMap.CellSize),
            Mathf.FloorToInt(world.y / HubCollisionMap.CellSize));

        private static Vector2 CentreOf(Vector2Int cell) => new(
            (cell.x + 0.5f) * HubCollisionMap.CellSize,
            (cell.y + 0.5f) * HubCollisionMap.CellSize);
    }
}
