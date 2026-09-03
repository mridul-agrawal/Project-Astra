using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEditor;
using UnityEngine;
using ProjectAstra.Core;
using ProjectAstra.Core.Hub;
using ProjectAstra.Core.Hub.Interaction;

namespace ProjectAstra.Core.Editor
{
    // Turns a placed prop into something she can walk up to, and back again.
    //
    // The reach region is built by the same code the game uses when it spawns one, so a prop made
    // interactive by hand behaves exactly like one the visit spawns.
    public static class HubAuthoring
    {
        public static bool IsInteractive(GameObject target) =>
            target != null && target.GetComponent<InteractableBehaviour>() != null;

        public static InspectableInteractable MakeInspectable(GameObject target)
        {
            if (target == null || IsInteractive(target)) return null;

            Undo.RegisterFullObjectHierarchyUndo(target, "Make it something she can look at");

            Vector2 foot = FootOf(target);
            InteractionPhysics.AttachReachRegion(target, foot);
            var inspectable = target.AddComponent<InspectableInteractable>();

            Describe(inspectable, UnusedIdFor(target), foot);
            EditorUtility.SetDirty(target);
            return inspectable;
        }

        // She walks up to where a thing stands, not to wherever its art happens to be pivoted.
        public static Vector2 FootOf(GameObject target)
        {
            var renderer = target.GetComponent<SpriteRenderer>();
            if (renderer == null || renderer.sprite == null) return Vector2.zero;

            Bounds art = renderer.sprite.bounds;
            return new Vector2(art.center.x, art.min.y);
        }

        // Takes away what MakeInspectable added, and nothing else. A collider a designer drew for
        // some other reason is left where it is.
        public static void Revert(GameObject target)
        {
            var inspectable = target != null ? target.GetComponent<InspectableInteractable>() : null;
            if (inspectable == null) return;

            Undo.RegisterFullObjectHierarchyUndo(target, "Not interactive any more");

            var reach = target.GetComponent<CircleCollider2D>();
            if (reach != null && reach.isTrigger) Object.DestroyImmediate(reach, true);

            Object.DestroyImmediate(inspectable, true);
            EditorUtility.SetDirty(target);
        }

        // The same object in another room, keeping everything it was configured with. Its name has
        // to change, because two objects answering to one name means only one is ever found.
        public static GameObject CopyInto(GameObject original, HubRoom room)
        {
            if (original == null || room == null) return null;

            var copy = Object.Instantiate(original, GroupLike(original, room));
            copy.name = original.name;

            Rename(copy);
            Undo.RegisterCreatedObjectUndo(copy, $"Send a copy to {room.LocationId}");
            EditorUtility.SetDirty(copy);
            return copy;
        }

        // Into the group it came from, so a prop stays with the props and a door with the doors.
        private static Transform GroupLike(GameObject original, HubRoom room)
        {
            Transform was = original.transform.parent;
            string wanted = was != null ? was.name : "Props";

            Transform group = room.transform.Find(wanted);
            if (group != null) return group;

            var made = new GameObject(wanted);
            made.transform.SetParent(room.transform, false);
            return made.transform;
        }

        private static void Rename(GameObject copy)
        {
            var inspectable = copy.GetComponent<InspectableInteractable>();
            if (inspectable == null) return;

            Name(inspectable, Unused(inspectable.InteractableId, HubIds.Of(HubIdKind.Interactable)));
        }

        private static void Name(InspectableInteractable inspectable, string id)
        {
            var editable = new SerializedObject(inspectable);
            editable.FindProperty("interactableId").stringValue = id;
            editable.ApplyModifiedProperties();
            HubIds.Forget();
        }

        // A name a person would recognise, in the shape the rest of the ids are in.
        public static string IdFrom(string name)
        {
            var id = new StringBuilder();
            bool lastWasBreak = true;

            foreach (char letter in name ?? "")
            {
                if (char.IsLetterOrDigit(letter))
                {
                    id.Append(char.ToLowerInvariant(letter));
                    lastWasBreak = false;
                }
                else if (!lastWasBreak)
                {
                    id.Append('_');
                    lastWasBreak = true;
                }
            }

            return id.ToString().Trim('_');
        }

        // Two identical props in one room would otherwise both answer to the same name, and only
        // one of them would ever be found.
        public static string Unused(string wanted, IEnumerable<string> taken)
        {
            var used = new HashSet<string>(taken ?? Enumerable.Empty<string>());
            if (string.IsNullOrEmpty(wanted)) wanted = "thing";
            if (!used.Contains(wanted)) return wanted;

            for (int suffix = 2; ; suffix++)
            {
                string candidate = $"{wanted}_{suffix}";
                if (!used.Contains(candidate)) return candidate;
            }
        }

        private static string UnusedIdFor(GameObject target) =>
            Unused(IdFrom(target.name), HubIds.Of(HubIdKind.Interactable));

        private static void Describe(InspectableInteractable inspectable, string id, Vector2 foot)
        {
            var editable = new SerializedObject(inspectable);
            editable.FindProperty("interactableId").stringValue = id;
            editable.FindProperty("interactionOffset").vector2Value = foot;
            editable.ApplyModifiedProperties();
            HubIds.Forget();
        }
    }
}
