using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace ProjectAstra.Core.Editor
{
    // Keeps an eye on the hub while it is being worked on, so a broken reference is noticed as it
    // is made rather than the next time somebody presses Play.
    //
    // Only the cheap checks. The ones that stand a room up in a scene of its own are asked for by
    // hand, in the Hub Editor.
    [InitializeOnLoad]
    public static class HubWatch
    {
        // Long enough that dragging something around does not re-check on every frame of the drag.
        private const double Settle = 0.75;

        private static List<HubProblem> found = new();
        private static double dueAt;
        private static bool waiting;

        public static event System.Action Changed;

        public static IReadOnlyList<HubProblem> Problems => found;
        public static int Count => found.Count;

        static HubWatch()
        {
            EditorApplication.hierarchyChanged += LookAgainSoon;
            Undo.postprocessModifications += _ => { LookAgainSoon(); return _; };
            EditorApplication.update += WhenItSettles;

            LookAgainSoon();
        }

        public static void LookAgainSoon()
        {
            dueAt = EditorApplication.timeSinceStartup + Settle;
            waiting = true;
        }

        // Nothing is checked while the game is running: what is wrong with the authored hub is a
        // question about the assets, not about the run.
        private static void WhenItSettles()
        {
            if (!waiting || Application.isPlaying || EditorApplication.timeSinceStartup < dueAt) return;
            waiting = false;

            List<HubProblem> now = HubProblems.CollectQuick();
            if (SameAsBefore(now)) return;

            found = now;
            Changed?.Invoke();
            SceneView.RepaintAll();
        }

        // Compared by what they say, so a re-check that finds the same things does not repaint the
        // scene view for nothing.
        private static bool SameAsBefore(List<HubProblem> now)
        {
            if (now.Count != found.Count) return false;

            for (int i = 0; i < now.Count; i++)
                if (now[i].Message != found[i].Message) return false;
            return true;
        }

        // What is wrong inside one room, for the badges drawn on the things themselves.
        public static IEnumerable<HubProblem> In(string locationId)
        {
            foreach (HubProblem problem in found)
                if (problem.Where.HasValue && problem.LocationId == locationId) yield return problem;
        }
    }
}
