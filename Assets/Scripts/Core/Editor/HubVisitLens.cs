using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using ProjectAstra.Core.Animation;
using ProjectAstra.Core.Hub;
using ProjectAstra.Core.Units;

namespace ProjectAstra.Core.Editor
{
    // Marks a stand-in built by the lens, and remembers whose placement it is showing.
    public sealed class HubPreviewActor : MonoBehaviour
    {
        public string CharacterId;
    }

    // Shows a room as one particular visit leaves it: who is standing where, and what they say.
    //
    // The stand-ins are built by the same factory the game uses, so what a designer drags is what
    // will be there. They are never saved — moving one writes into the visit asset instead.
    [InitializeOnLoad]
    public static class HubVisitLens
    {
        private const string VisitKey = "ProjectAstra.Hub.PreviewVisit";
        private const string PreviewRootName = "__HubPreviewCast";

        public static event System.Action Changed;

        static HubVisitLens()
        {
            EditorSceneManager.sceneOpened += (_, __) => Clear();
            EditorSceneManager.sceneClosing += (_, __) => Clear();
            EditorApplication.playModeStateChanged += _ => Clear();
            EditorApplication.projectChanged += Forget;
            HubEditing.Changed += Refresh;

            // Only when a drag ends. Every repaint would mean reading the whole cast back sixty
            // times a second to find out that nothing moved.
            SceneView.duringSceneGui += _ =>
            {
                if (Event.current.type == EventType.MouseUp) WriteBackMoves();
            };
        }

        // Which visit the room is being shown as, or null for the room on its own. Remembered per
        // project, because looking at a visit is not a change to anything.
        public static HubVisitData Visit
        {
            get
            {
                string id = EditorPrefs.GetString(VisitKey, "");
                return string.IsNullOrEmpty(id) ? null : All().FirstOrDefault(v => v.VisitId == id);
            }
            set
            {
                EditorPrefs.SetString(VisitKey, value != null ? value.VisitId : "");
                Refresh();
            }
        }

        // Searching the whole project for these is what made the Hub panel slow enough to stall the
        // editor, because the panel reads them several times a frame. Held until the project changes.
        private static HubVisitData[] visits;
        private static UnitDefinition[] cast;

        public static IEnumerable<HubVisitData> All()
        {
            if (Stale(visits)) visits = LoadVisits();
            return visits;
        }

        public static void Forget()
        {
            visits = null;
            cast = null;
        }

        // A deleted asset leaves a dead entry behind, so a list with a hole in it is read again.
        private static bool Stale(Object[] held) => held == null || held.Any(one => one == null);

        private static HubVisitData[] LoadVisits() =>
            AssetDatabase.FindAssets("t:HubVisitData")
                .Select(guid => AssetDatabase.LoadAssetAtPath<HubVisitData>(AssetDatabase.GUIDToAssetPath(guid)))
                .Where(visit => visit != null)
                .OrderBy(visit => visit.VisitId, System.StringComparer.OrdinalIgnoreCase)
                .ToArray();

        public static void Refresh()
        {
            Clear();
            Build();

            Changed?.Invoke();
            SceneView.RepaintAll();
        }

        // Anything the lens put in the room, gone. Called on every scene change because a stand-in
        // that outlived its room would be a character nobody placed.
        public static void Clear()
        {
            foreach (GameObject root in RootsInRooms()) Object.DestroyImmediate(root);

            foreach (HubPreviewActor stray in
                     Resources.FindObjectsOfTypeAll<HubPreviewActor>().Where(a => a != null))
            {
                GameObject root = RootOf(stray.gameObject);
                if (root != null) Object.DestroyImmediate(root);
            }
        }

        // A root with nobody in it has no stand-in to be found by, so the rooms are read directly.
        // One was left behind every refresh whenever a visit had no cast for the room being edited.
        private static List<GameObject> RootsInRooms()
        {
            var found = new List<GameObject>();

            foreach (HubRoom room in HubRooms.InLoadedScenes())
                foreach (Transform child in room.transform)
                    if (child.name == PreviewRootName) found.Add(child.gameObject);

            return found;
        }

        private static GameObject RootOf(GameObject actor)
        {
            Transform parent = actor.transform.parent;
            return parent != null && parent.name == PreviewRootName ? parent.gameObject : actor;
        }

        private static void Build()
        {
            HubRoom room = HubEditing.EditingRoom;
            HubVisitData visit = Visit;
            if (room == null || visit == null) return;

            Transform cast = null;
            foreach (HubCharacterPlacement placement in visit.CharacterPlacements)
                if (placement.locationId == room.LocationId) Stand(placement, cast ??= MakeCastRoot(room));
        }

        // Made only once somebody is actually going to stand in it, so a room this visit leaves
        // empty is left alone.
        private static Transform MakeCastRoot(HubRoom room)
        {
            var root = new GameObject(PreviewRootName) { hideFlags = HideFlags.DontSave };

            // Under the room rather than loose in the scene, so it dies when the scene closes
            // instead of surviving into whatever opens next.
            root.transform.SetParent(room.transform, false);
            return root.transform;
        }

        private static void Stand(HubCharacterPlacement placement, Transform cast)
        {
            UnitDefinition who = Character(placement.characterId);
            if (who == null) return;

            HubActor actor = HubActorFactory.Create(who, cast, placement.position,
                placement.facing, placement.conversationId, solid: false);
            if (actor == null) return;

            Mark(actor.gameObject, placement.characterId);
        }

        private static void Mark(GameObject actor, string characterId)
        {
            actor.hideFlags = HideFlags.DontSave;
            foreach (Transform child in actor.transform) child.gameObject.hideFlags = HideFlags.DontSave;

            // Depth sorting normally happens once the game runs, so a stand-in would sit behind the
            // floor it is standing on.
            foreach (YSortRenderer sorter in actor.GetComponentsInChildren<YSortRenderer>(true))
                sorter.Apply();

            actor.AddComponent<HubPreviewActor>().CharacterId = characterId;
        }

        private static UnitDefinition Character(string characterId)
        {
            if (Stale(cast)) cast = LoadCast();
            return cast.FirstOrDefault(unit => unit.UnitId == characterId);
        }

        private static UnitDefinition[] LoadCast() =>
            AssetDatabase.FindAssets("t:UnitDefinition")
                .Select(guid => AssetDatabase.LoadAssetAtPath<UnitDefinition>(AssetDatabase.GUIDToAssetPath(guid)))
                .Where(unit => unit != null)
                .ToArray();

        // --- Dragging one writes it down ---

        // A stand-in that has been moved is the designer saying where that character stands this
        // visit, so it is written into the visit rather than left in a scene nobody saves.
        public static void WriteBackMoves()
        {
            HubVisitData visit = Visit;
            if (visit == null) return;

            var editable = new SerializedObject(visit);
            SerializedProperty placements = editable.FindProperty("characterPlacements");
            bool moved = false;

            // FindObjectsByType skips anything flagged DontSave, which every stand-in is.
            foreach (HubPreviewActor stand in Resources.FindObjectsOfTypeAll<HubPreviewActor>())
                if (stand != null) moved |= WriteBack(stand, placements);

            if (!moved) return;

            editable.ApplyModifiedProperties();
            EditorUtility.SetDirty(visit);
        }

        private static bool WriteBack(HubPreviewActor stand, SerializedProperty placements)
        {
            SerializedProperty placement = Find(placements, stand.CharacterId);
            if (placement == null) return false;

            SerializedProperty where = placement.FindPropertyRelative("position");
            var standing = (Vector2)HubPlacement.SnapToPixel(stand.transform.position);

            if ((where.vector2Value - standing).sqrMagnitude < 0.0001f) return false;

            where.vector2Value = standing;
            return true;
        }

        private static SerializedProperty Find(SerializedProperty placements, string characterId)
        {
            for (int i = 0; i < placements.arraySize; i++)
            {
                SerializedProperty candidate = placements.GetArrayElementAtIndex(i);
                if (candidate.FindPropertyRelative("characterId").stringValue == characterId) return candidate;
            }
            return null;
        }
    }
}
