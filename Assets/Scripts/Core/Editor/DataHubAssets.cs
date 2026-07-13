using System;
using System.IO;
using UnityEditor;
using UnityEngine;
using ProjectAstra.Core.Units;
using ProjectAstra.Core.Dialogue;

namespace ProjectAstra.Core.Editor
{
    // Shared asset plumbing for the Data Hub: where each authored type lives on disk, how a new
    // asset is created, and a navigate signal the window listens to so an inline "+ New" jumps
    // straight to the freshly created asset.
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
            return asset;
        }

        public static void EnsureFolder(string folder)
        {
            if (AssetDatabase.IsValidFolder(folder)) return;
            string parent = Path.GetDirectoryName(folder).Replace('\\', '/');
            string leaf = Path.GetFileName(folder);
            if (!AssetDatabase.IsValidFolder(parent)) EnsureFolder(parent);
            AssetDatabase.CreateFolder(parent, leaf);
        }
    }
}
