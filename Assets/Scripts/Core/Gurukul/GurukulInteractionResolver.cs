using System.Collections.Generic;
using UnityEngine;
using ProjectAstra.Core.Animation;

namespace ProjectAstra.Core.Gurukul
{
    // The verbs a prompt can show. Append only.
    public enum GurukulVerb
    {
        Talk,
        Inspect,
        Enter,
        Leave,
        Report,
        Depart
    }

    // Priority order when more than one thing is in reach — lower wins. The numbering is the ladder
    // from the spec, minus its first rung: an open menu or a running line of dialogue is settled by
    // the sub-state machine long before anything gets here.
    public enum GurukulTargetKind
    {
        Character = 0,
        ObjectiveObject = 1,
        Door = 2,
        Inspectable = 3
    }

    public readonly struct GurukulInteractionCandidate
    {
        public readonly string Id;
        public readonly GurukulTargetKind Kind;
        public readonly Vector2 FootPosition;
        public readonly GurukulVerb Verb;

        public GurukulInteractionCandidate(string id, GurukulTargetKind kind, Vector2 footPosition, GurukulVerb verb)
        {
            Id = id;
            Kind = kind;
            FootPosition = footPosition;
            Verb = verb;
        }

        public bool IsValid => !string.IsNullOrEmpty(Id);
    }

    // Decides the single thing an INTERACT press acts on.
    //
    // Reach is a box in front of her rather than a radius, which is what makes "visibly adjacent and
    // facing" enough — no tile-centre alignment, and a lateral tolerance wide enough to survive
    // normal sub-tile drift.
    //
    // Pure C# over a candidate list, so the whole priority ladder is a table-driven test.
    public static class GurukulInteractionResolver
    {
        public const float DefaultReachTiles = 1.0f;

        // A bit over half a tile, so standing slightly off to one side still counts as facing it.
        public const float DefaultLateralTolerance = 0.6f;

        private const float LineOfSightStep = 0.25f;

        // Stops the sight check short of the target, because a character's own body is solid and
        // would otherwise block the view of itself.
        private const float TargetBodyMargin = 0.4f;

        public static bool TryResolve(Vector2 playerFoot, Facing facing,
            IReadOnlyList<GurukulInteractionCandidate> candidates, GurukulCollisionMap collision,
            out GurukulInteractionCandidate chosen,
            float reach = DefaultReachTiles, float lateralTolerance = DefaultLateralTolerance)
        {
            chosen = default;
            if (candidates == null) return false;

            bool found = false;
            float bestLateral = 0f, bestForward = 0f;

            foreach (GurukulInteractionCandidate candidate in candidates)
            {
                if (!IsInReach(playerFoot, facing, candidate.FootPosition, reach, lateralTolerance,
                        out float forward, out float lateral)) continue;
                if (!HasLineOfSight(playerFoot, candidate.FootPosition, collision)) continue;

                if (found && !Beats(candidate.Kind, lateral, forward, chosen.Kind, bestLateral, bestForward)) continue;

                chosen = candidate;
                bestLateral = lateral;
                bestForward = forward;
                found = true;
            }
            return found;
        }

        // Measured in her facing's frame: how far ahead the target is, and how far off to the side.
        public static bool IsInReach(Vector2 playerFoot, Facing facing, Vector2 targetFoot,
            float reach, float lateralTolerance, out float forward, out float lateral)
        {
            Vector2 offset = targetFoot - playerFoot;
            Vector2 ahead = Cardinal.ToVector(facing);
            Vector2 side = new(-ahead.y, ahead.x);

            forward = Vector2.Dot(offset, ahead);
            lateral = Mathf.Abs(Vector2.Dot(offset, side));

            return forward > 0f && forward <= reach && lateral <= lateralTolerance;
        }

        // Kind first, then whatever is most directly in front, then whatever is nearest.
        private static bool Beats(GurukulTargetKind kind, float lateral, float forward,
            GurukulTargetKind bestKind, float bestLateral, float bestForward)
        {
            if (kind != bestKind) return kind < bestKind;
            if (!Mathf.Approximately(lateral, bestLateral)) return lateral < bestLateral;
            return forward < bestForward;
        }

        // Walls, counters and the river all break an interaction; the target's own body does not.
        private static bool HasLineOfSight(Vector2 from, Vector2 to, GurukulCollisionMap collision)
        {
            if (collision == null) return true;

            Vector2 offset = to - from;
            float distance = offset.magnitude - TargetBodyMargin;
            if (distance <= 0f) return true;

            Vector2 direction = offset.normalized;
            for (float travelled = LineOfSightStep; travelled <= distance; travelled += LineOfSightStep)
            {
                Vector2 point = from + direction * travelled;
                if (collision.IsCellBlocked(CellOf(point.x), CellOf(point.y))) return false;
            }
            return true;
        }

        private static int CellOf(float world) => Mathf.FloorToInt(world / GurukulCollisionMap.CellSize);
    }
}
