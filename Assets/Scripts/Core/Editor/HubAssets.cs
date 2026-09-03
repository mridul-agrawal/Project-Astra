using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;
using ProjectAstra.Core;
using ProjectAstra.Core.Dialogue;
using ProjectAstra.Core.Hub.Events;

namespace ProjectAstra.Core.Editor
{
    // Makes the hub content a field is asking for, from the field that is asking for it.
    //
    // A new thing is put beside the catalog that indexes it and added to that catalog, so it is
    // reachable the moment it exists rather than after somebody remembers to register it.
    public static class HubAssets
    {
        public static bool CanCreate(HubIdKind kind) =>
            kind is HubIdKind.Conversation or HubIdKind.Event;

        public static Object Create(HubIdKind kind, string id) => kind switch
        {
            HubIdKind.Conversation => Conversation(id),
            HubIdKind.Event => Event(id),
            _ => null
        };

        private static Object Conversation(string id)
        {
            DialogueScriptCatalog catalog = One<DialogueScriptCatalog>();
            if (catalog == null) return Missing("Dialogue Script Catalog");

            var script = ScriptableObject.CreateInstance<DialogueScript>();
            Save(script, catalog, id);
            Write(script, "scriptId", id);
            Register(catalog, "scripts", script);

            return Reveal(script);
        }

        private static Object Event(string id)
        {
            HubEventDatabase database = One<HubEventDatabase>();
            if (database == null) return Missing("Hub Event Database");

            var authored = ScriptableObject.CreateInstance<HubEventData>();
            Save(authored, database, id);
            Write(authored, "eventId", id);
            Register(database, "events", authored);

            return Reveal(authored);
        }

        private static void Save(ScriptableObject asset, Object beside, string id)
        {
            string folder = Path.GetDirectoryName(AssetDatabase.GetAssetPath(beside)).Replace('\\', '/');
            AssetDatabase.CreateAsset(asset, AssetDatabase.GenerateUniqueAssetPath($"{folder}/{id}.asset"));
        }

        private static void Write(ScriptableObject asset, string field, string value)
        {
            var editable = new SerializedObject(asset);
            editable.FindProperty(field).stringValue = value;
            editable.ApplyModifiedPropertiesWithoutUndo();
        }

        // Appended, never rewritten: a catalog holds whatever a designer has put in it, and
        // rebuilding one from code would quietly drop their entries.
        private static void Register(ScriptableObject catalog, string listName, Object entry)
        {
            var editable = new SerializedObject(catalog);
            SerializedProperty list = editable.FindProperty(listName);

            list.arraySize++;
            list.GetArrayElementAtIndex(list.arraySize - 1).objectReferenceValue = entry;
            editable.ApplyModifiedProperties();

            EditorUtility.SetDirty(catalog);
        }

        private static Object Reveal(Object asset)
        {
            AssetDatabase.SaveAssets();
            HubIds.Forget();
            EditorGUIUtility.PingObject(asset);
            return asset;
        }

        private static Object Missing(string what)
        {
            Debug.LogError($"[HubAssets] There is no {what} in the project, so there is nowhere to put this.");
            return null;
        }

        private static T One<T>() where T : Object =>
            AssetDatabase.FindAssets($"t:{typeof(T).Name}")
                .Select(guid => AssetDatabase.LoadAssetAtPath<T>(AssetDatabase.GUIDToAssetPath(guid)))
                .FirstOrDefault(found => found != null);
    }
}
