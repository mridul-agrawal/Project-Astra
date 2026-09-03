using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;
using ProjectAstra.Core;
using ProjectAstra.Core.Dialogue;
using ProjectAstra.Core.Hub;
using ProjectAstra.Core.Hub.Events;
using ProjectAstra.Core.Quests;

namespace ProjectAstra.Core.Editor
{
    // Makes the hub content a field is asking for, from the field that is asking for it.
    //
    // A new thing is put beside the catalog that indexes it and added to that catalog, so it is
    // reachable the moment it exists rather than after somebody remembers to register it.
    public static class HubAssets
    {
        // What each kind of name is made of, and what has to know about it afterwards.
        private readonly struct Recipe
        {
            public readonly Type Asset;
            public readonly string IdField;
            public readonly Type Catalog;
            public readonly string ListField;

            public Recipe(Type asset, string idField, Type catalog = null, string listField = null)
            {
                Asset = asset;
                IdField = idField;
                Catalog = catalog;
                ListField = listField;
            }

            public bool IsIndexed => Catalog != null;
        }

        private static readonly Dictionary<HubIdKind, Recipe> Recipes = new()
        {
            [HubIdKind.Conversation] = new(typeof(DialogueScript), "scriptId",
                typeof(DialogueScriptCatalog), "scripts"),
            [HubIdKind.Location] = new(typeof(HubLocationData), "locationId",
                typeof(HubLocationDatabase), "locations"),
            [HubIdKind.Event] = new(typeof(HubEventData), "eventId",
                typeof(HubEventDatabase), "events"),
            [HubIdKind.Quest] = new(typeof(QuestData), "questId",
                typeof(QuestCatalog), "quests"),
            [HubIdKind.Visit] = new(typeof(HubVisitData), "visitId",
                typeof(HubVisitDatabase), "visits"),

            // A stage belongs to its quest rather than to a catalog, so it is filed beside the
            // quests and reached through the one that owns it.
            [HubIdKind.Objective] = new(typeof(QuestObjective), "objectiveId"),
        };

        public static bool CanCreate(HubIdKind kind) => Recipes.ContainsKey(kind);

        public static Type TypeOf(HubIdKind kind) =>
            Recipes.TryGetValue(kind, out Recipe recipe) ? recipe.Asset : null;

        public static UnityEngine.Object Create(HubIdKind kind, string id)
        {
            if (!Recipes.TryGetValue(kind, out Recipe recipe)) return null;

            ScriptableObject home = FindCatalogFor(recipe);
            if (home == null) return Missing(recipe);

            var made = ScriptableObject.CreateInstance(recipe.Asset);
            AssetDatabase.CreateAsset(made, PathBeside(home, id));
            Write(made, recipe.IdField, id);

            if (recipe.IsIndexed) Register(home, recipe.ListField, made);
            return Reveal(made);
        }

        // A stage is filed with the quests, which is where its catalog would be if it had one.
        private static ScriptableObject FindCatalogFor(Recipe recipe) =>
            recipe.IsIndexed ? One(recipe.Catalog) : One(typeof(QuestCatalog));

        private static string PathBeside(UnityEngine.Object neighbour, string id)
        {
            string folder = Path.GetDirectoryName(AssetDatabase.GetAssetPath(neighbour)).Replace('\\', '/');
            return AssetDatabase.GenerateUniqueAssetPath($"{folder}/{id}.asset");
        }

        private static void Write(ScriptableObject asset, string field, string value)
        {
            var editable = new SerializedObject(asset);
            editable.FindProperty(field).stringValue = value;
            editable.ApplyModifiedPropertiesWithoutUndo();
        }

        // Appended, never rewritten: a catalog holds whatever a designer has put in it, and
        // rebuilding one from code would quietly drop their entries.
        public static void Register(ScriptableObject catalog, string listName, UnityEngine.Object entry)
        {
            var editable = new SerializedObject(catalog);
            SerializedProperty list = editable.FindProperty(listName);

            list.arraySize++;
            list.GetArrayElementAtIndex(list.arraySize - 1).objectReferenceValue = entry;
            editable.ApplyModifiedProperties();

            EditorUtility.SetDirty(catalog);
        }

        private static UnityEngine.Object Reveal(UnityEngine.Object asset)
        {
            AssetDatabase.SaveAssets();
            HubIds.Forget();
            EditorGUIUtility.PingObject(asset);
            return asset;
        }

        private static UnityEngine.Object Missing(Recipe recipe)
        {
            string what = ObjectNames.NicifyVariableName((recipe.Catalog ?? typeof(QuestCatalog)).Name);
            Debug.LogError($"[HubAssets] There is no {what} in the project, so there is nowhere to put this.");
            return null;
        }

        private static ScriptableObject One(Type type) =>
            AssetDatabase.FindAssets($"t:{type.Name}")
                .Select(guid => AssetDatabase.LoadAssetAtPath(AssetDatabase.GUIDToAssetPath(guid), type))
                .OfType<ScriptableObject>()
                .FirstOrDefault();
    }
}
