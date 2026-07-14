using System;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;
using ProjectAstra.Core.Units;
using ProjectAstra.Core.Dialogue;

namespace ProjectAstra.Core.Editor
{
    // Shared asset plumbing for the Data Hub: where each authored type lives on disk, how a new
    // asset is created (and auto-registered), project-wide discovery, and a navigate signal the
    // window listens to so an inline "+ New" jumps straight to the freshly created asset.
    public static class DataHubAssets
    {
        // Raised when the user follows a reference or creates one inline — the window switches to
        // the matching tab and selects the target.
        public static event Action<UnityEngine.Object> NavigateRequested;

        public static void RequestNavigate(UnityEngine.Object target) => NavigateRequested?.Invoke(target);

        public static string FolderFor(Type type)
        {
            if (type == typeof(UnitDefinition)) return "Assets/ScriptableObjects/Units/Characters";
            if (type == typeof(ClassDefinition)) return "Assets/ScriptableObjects/Units/Classes";
            if (type == typeof(WeaponDefinition)) return "Assets/ScriptableObjects/Items/Weapons";
            if (type == typeof(ConsumableDefinition)) return "Assets/ScriptableObjects/Items/Consumables";
            if (type == typeof(InventoryLoadout)) return "Assets/ScriptableObjects/Items/Loadouts";
            if (type == typeof(DialogueSpeaker)) return "Assets/ScriptableObjects/Dialogue/Speakers";
            return "Assets/ScriptableObjects";
        }

        public static ScriptableObject Create(Type type, string name)
        {
            string folder = FolderFor(type);
            EnsureFolder(folder);
            string safe = string.IsNullOrWhiteSpace(name) ? type.Name : name.Trim();
            string path = AssetDatabase.GenerateUniqueAssetPath($"{folder}/{safe}.asset");

            var asset = ScriptableObject.CreateInstance(type);
            AssetDatabase.CreateAsset(asset, path);
            AssetDatabase.SaveAssets();

            if (asset is UnitDefinition unit) RegisterUnit(unit);   // never silently unregistered
            return asset;
        }

        // Writes a human name into whichever "name" field this asset type owns, so a newly created
        // asset is identifiable in the catalog rather than a generic "New X". No-op for types without one.
        public static void SetDisplayName(ScriptableObject asset, string name)
        {
            if (asset == null || string.IsNullOrWhiteSpace(name)) return;
            string prop = asset switch
            {
                UnitDefinition _ => "unitName",
                ClassDefinition _ => "className",
                ItemDefinition _ => "displayName",
                InventoryLoadout _ => "loadoutName",
                _ => null,
            };
            if (prop == null) return;

            var so = new SerializedObject(asset);
            var property = so.FindProperty(prop);
            if (property == null) return;
            property.stringValue = name;
            so.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(asset);
            AssetDatabase.SaveAssets();
        }

        public static void EnsureFolder(string folder)
        {
            if (AssetDatabase.IsValidFolder(folder)) return;
            string parent = Path.GetDirectoryName(folder).Replace('\\', '/');
            string leaf = Path.GetFileName(folder);
            if (!AssetDatabase.IsValidFolder(parent)) EnsureFolder(parent);
            AssetDatabase.CreateFolder(parent, leaf);
        }

        public static List<T> LoadAll<T>() where T : UnityEngine.Object
        {
            var list = new List<T>();
            foreach (string guid in AssetDatabase.FindAssets($"t:{typeof(T).Name}"))
            {
                var asset = AssetDatabase.LoadAssetAtPath<T>(AssetDatabase.GUIDToAssetPath(guid));
                if (asset != null) list.Add(asset);
            }
            return list;
        }

        public static UnitDatabase FindUnitDatabase()
        {
            foreach (string guid in AssetDatabase.FindAssets("t:UnitDatabase"))
                return AssetDatabase.LoadAssetAtPath<UnitDatabase>(AssetDatabase.GUIDToAssetPath(guid));
            return null;
        }

        public static bool IsRegistered(UnitDatabase db, UnitDefinition unit)
        {
            if (db == null || unit == null) return false;
            foreach (UnitDefinition u in db.Units)
                if (u == unit) return true;
            return false;
        }

        // Appends a unit to the UnitDatabase list if it isn't already there. Returns true if it added it.
        public static bool RegisterUnit(UnitDefinition unit)
        {
            UnitDatabase db = FindUnitDatabase();
            if (db == null || unit == null || IsRegistered(db, unit)) return false;

            var so = new SerializedObject(db);
            var list = so.FindProperty("units");
            list.arraySize++;
            list.GetArrayElementAtIndex(list.arraySize - 1).objectReferenceValue = unit;
            so.ApplyModifiedProperties();
            EditorUtility.SetDirty(db);
            AssetDatabase.SaveAssets();
            return true;
        }

        // Rebuilds the UnitDatabase list from every UnitDefinition asset in the project. Returns the
        // count written, or -1 if there is no UnitDatabase asset.
        public static int RescanUnits()
        {
            UnitDatabase db = FindUnitDatabase();
            if (db == null) return -1;

            List<UnitDefinition> all = LoadAll<UnitDefinition>();
            all.Sort((a, b) => string.Compare(a.name, b.name, StringComparison.OrdinalIgnoreCase));

            var so = new SerializedObject(db);
            var list = so.FindProperty("units");
            list.arraySize = all.Count;
            for (int i = 0; i < all.Count; i++)
                list.GetArrayElementAtIndex(i).objectReferenceValue = all[i];
            so.ApplyModifiedProperties();
            EditorUtility.SetDirty(db);
            AssetDatabase.SaveAssets();
            return all.Count;
        }
    }
}
