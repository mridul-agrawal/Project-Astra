using UnityEngine;

namespace ProjectAstra.Core.Hub
{
    // A one-off "start here instead" left for the hub to find when it comes up.
    //
    // Kept in player preferences rather than in a scene or an asset, so asking to test the middle
    // of a visit is never a change anyone could commit by accident. It is taken once and forgotten,
    // so pressing Play again afterwards starts the game the way it really starts.
    public static class HubLaunchRequest
    {
        private const string VisitKey = "ProjectAstra.Hub.Launch.Visit";
        private const string StageKey = "ProjectAstra.Hub.Launch.Stage";
        private const string SpawnKey = "ProjectAstra.Hub.Launch.Spawn";
        private const string SpawnXKey = "ProjectAstra.Hub.Launch.SpawnX";
        private const string SpawnYKey = "ProjectAstra.Hub.Launch.SpawnY";

        public readonly struct Request
        {
            public readonly string VisitId;
            public readonly int Stage;
            public readonly bool HasSpawn;
            public readonly Vector2 Spawn;

            public Request(string visitId, int stage, bool hasSpawn, Vector2 spawn)
            {
                VisitId = visitId;
                Stage = stage;
                HasSpawn = hasSpawn;
                Spawn = spawn;
            }

            public bool IsSomething => !string.IsNullOrEmpty(VisitId);
        }

        public static void Set(string visitId, int stage = 0, Vector2? spawn = null)
        {
            PlayerPrefs.SetString(VisitKey, visitId ?? "");
            PlayerPrefs.SetInt(StageKey, Mathf.Max(0, stage));
            PlayerPrefs.SetInt(SpawnKey, spawn.HasValue ? 1 : 0);
            PlayerPrefs.SetFloat(SpawnXKey, spawn?.x ?? 0f);
            PlayerPrefs.SetFloat(SpawnYKey, spawn?.y ?? 0f);
            PlayerPrefs.Save();
        }

        // Reading it takes it. A request that survived into the next run would be a designer
        // wondering why the game keeps opening in the middle.
        public static Request Take()
        {
            var asked = new Request(
                PlayerPrefs.GetString(VisitKey, ""),
                PlayerPrefs.GetInt(StageKey, 0),
                PlayerPrefs.GetInt(SpawnKey, 0) == 1,
                new Vector2(PlayerPrefs.GetFloat(SpawnXKey, 0f), PlayerPrefs.GetFloat(SpawnYKey, 0f)));

            Clear();
            return asked;
        }

        public static void Clear()
        {
            foreach (string key in new[] { VisitKey, StageKey, SpawnKey, SpawnXKey, SpawnYKey })
                PlayerPrefs.DeleteKey(key);
            PlayerPrefs.Save();
        }
    }
}
