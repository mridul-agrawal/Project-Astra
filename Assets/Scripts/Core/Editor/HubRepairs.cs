using System.Collections.Generic;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using ProjectAstra.Core.Hub;
using ProjectAstra.Core.Hub.Interaction;

namespace ProjectAstra.Core.Editor
{
    // The fixes that are offered next to a problem.
    //
    // Only the ones with exactly one sensible answer. Anything a designer would have to decide is
    // left to them: a button that guesses is worse than no button.
    public static class HubRepairs
    {
        public static HubFix NameItAfterItsAsset(ScriptableObject asset, string field) =>
            new($"Call it '{HubAuthoring.IdFrom(asset.name)}'", () =>
            {
                var editable = new SerializedObject(asset);
                editable.FindProperty(field).stringValue = HubAuthoring.IdFrom(asset.name);
                editable.ApplyModifiedProperties();

                AssetDatabase.SaveAssets();
                HubIds.Forget();
            });

        // A door is named after the room it leads to, which is what a designer would have called it.
        public static HubFix NameDoor(DoorInteractable door, HashSet<string> taken)
        {
            string wanted = HubAuthoring.Unused(Suggested(door), taken);

            return new HubFix($"Call it '{wanted}'", () =>
            {
                var editable = new SerializedObject(door);
                editable.FindProperty("door").FindPropertyRelative("doorId").stringValue = wanted;
                editable.ApplyModifiedProperties();

                MarkTheSceneChanged(door);
                HubIds.Forget();
            });
        }

        private static string Suggested(DoorInteractable door) =>
            HubAuthoring.IdFrom(string.IsNullOrEmpty(door.TargetLocationId)
                ? $"{door.name}"
                : $"to {door.TargetLocationId}");

        // A place with no room cannot be shown at all, and an empty room is the only thing anyone
        // would make here. What goes in it is the designer's.
        public static HubFix MakeRoomFor(HubLocationData location) =>
            new("Make an empty room for it", () =>
            {
                if (HubRooms.Find(location) != null) return;

                var made = new GameObject($"Room_{location.LocationId}");
                made.AddComponent<HubRoom>().Bind(location);

                foreach (string group in new[] { "Ground", "Walls", "Props", "Doors" })
                    new GameObject(group).transform.SetParent(made.transform, false);

                Undo.RegisterCreatedObjectUndo(made, "Make a room");
                MarkTheSceneChanged(made);
                Selection.activeGameObject = made;
            });

        private static void MarkTheSceneChanged(Object part)
        {
            var component = part as Component;
            GameObject host = component != null ? component.gameObject : part as GameObject;
            if (host == null) return;

            EditorUtility.SetDirty(host);
            EditorSceneManager.MarkSceneDirty(host.scene);
        }
    }
}
