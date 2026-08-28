using System.Collections.Generic;
using UnityEngine;

namespace ProjectAstra.Core.Gurukul
{
    // How long it takes to walk somewhere, and whether it can be walked to at all.
    //
    // The spec asks for every mandatory route in a visit to be timed against a 10-12 second target
    // at the standard speed, and for anything slower to be reported to design rather than quietly
    // patched with a teleport or a per-route speed. This is what does the timing.
    //
    // Searched over the same half-tile cells movement uses, four-connected because she only ever
    // walks in cardinal directions, and testing the whole footprint at each step so a route that
    // needs her to be narrower than she is doesn't count as walkable.
    public static class WalkableRouteTimer
    {
        public static bool TryMeasureSeconds(GurukulCollisionMap map, Rect footprintOffset,
            Vector2 from, Vector2 to, out float seconds,
            float speedTilesPerSecond = GurukulMover.DefaultSpeedTilesPerSecond)
        {
            seconds = 0f;
            if (map == null || speedTilesPerSecond <= 0f) return false;

            if (!TryMeasureCells(map, footprintOffset, from, to, out int cells)) return false;

            seconds = cells * GurukulCollisionMap.CellSize / speedTilesPerSecond;
            return true;
        }

        public static bool TryMeasureCells(GurukulCollisionMap map, Rect footprintOffset,
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
        public static bool CanReachNeighbour(GurukulCollisionMap map, Rect footprintOffset,
            Vector2 from, Vector2 to, out float seconds,
            float speedTilesPerSecond = GurukulMover.DefaultSpeedTilesPerSecond)
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

        private static bool Fits(GurukulCollisionMap map, Rect footprintOffset, Vector2Int cell) =>
            !map.IsRectBlocked(GurukulMover.FootprintAt(CentreOf(cell), footprintOffset));

        private static Vector2Int CellOf(Vector2 world) => new(
            Mathf.FloorToInt(world.x / GurukulCollisionMap.CellSize),
            Mathf.FloorToInt(world.y / GurukulCollisionMap.CellSize));

        private static Vector2 CentreOf(Vector2Int cell) => new(
            (cell.x + 0.5f) * GurukulCollisionMap.CellSize,
            (cell.y + 0.5f) * GurukulCollisionMap.CellSize);
    }
}
