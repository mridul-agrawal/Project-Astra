using System;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;

namespace ProjectAstra.Core.Editor
{
    // Designer-facing tool to forge weapon/consumable item assets and browse the
    // catalog. "Forge" creates a normal ScriptableObject under ScriptableObjects/
    // Items, then edits it inline — these are the assets InventoryLoadouts point at.
    public class WeaponForgeWindow : EditorWindow
    {
        private const string ItemsRoot = "Assets/ScriptableObjects/Items";
        private const string WeaponsFolder = ItemsRoot + "/Weapons";
        private const string ConsumablesFolder = ItemsRoot + "/Consumables";

        private enum NewItemKind { Weapon, Consumable }

        private Vector2 listScroll;
        private Vector2 detailScroll;
        private ItemDefinition selected;
        private UnityEditor.Editor embeddedEditor;
        private NewItemKind newKind = NewItemKind.Weapon;
        private string newName = "New Weapon";
        private string filter = "";

        [MenuItem("Project Astra/Items/Weapon Forge")]
        public static void Open()
        {
            var window = GetWindow<WeaponForgeWindow>("Weapon Forge");
            window.minSize = new Vector2(740, 440);
        }

        private void OnGUI()
        {
            EditorGUILayout.BeginHorizontal();
            DrawCatalogPane();
            DrawDetailPane();
            EditorGUILayout.EndHorizontal();
        }

        private void DrawCatalogPane()
        {
            EditorGUILayout.BeginVertical(GUILayout.Width(260));
            EditorGUILayout.LabelField("Item Catalog", EditorStyles.boldLabel);
            filter = EditorGUILayout.TextField("Filter", filter);

            listScroll = EditorGUILayout.BeginScrollView(listScroll);
            DrawCatalogGroup<WeaponDefinition>("Weapons");
            DrawCatalogGroup<ConsumableDefinition>("Consumables");
            EditorGUILayout.EndScrollView();
            EditorGUILayout.EndVertical();
        }

        private void DrawCatalogGroup<T>(string header) where T : ItemDefinition
        {
            EditorGUILayout.Space(4);
            EditorGUILayout.LabelField(header, EditorStyles.miniBoldLabel);
            foreach (T item in LoadAll<T>())
            {
                if (!Matches(item)) continue;
                bool wasSelected = selected == item;
                if (GUILayout.Toggle(wasSelected, item.name, "Button") && !wasSelected)
                    Select(item);
            }
        }

        private bool Matches(ItemDefinition item) =>
            string.IsNullOrEmpty(filter) ||
            item.name.IndexOf(filter, StringComparison.OrdinalIgnoreCase) >= 0;

        private void DrawDetailPane()
        {
            EditorGUILayout.BeginVertical();
            DrawForgeSection();
            EditorGUILayout.Space(8);
            DrawSelectedEditor();
            EditorGUILayout.EndVertical();
        }

        private void DrawForgeSection()
        {
            EditorGUILayout.LabelField("Forge New Item", EditorStyles.boldLabel);
            newKind = (NewItemKind)EditorGUILayout.EnumPopup("Kind", newKind);
            newName = EditorGUILayout.TextField("Name", newName);
            if (GUILayout.Button("Forge", GUILayout.Width(120)))
                Select(ForgeNew());
        }

        private void DrawSelectedEditor()
        {
            if (selected == null)
            {
                EditorGUILayout.HelpBox("Select an item on the left, or forge a new one above.", MessageType.Info);
                return;
            }

            EditorGUILayout.LabelField($"Editing: {selected.name}", EditorStyles.boldLabel);
            if (GUILayout.Button("Reveal in Project", GUILayout.Width(140)))
                EditorGUIUtility.PingObject(selected);

            detailScroll = EditorGUILayout.BeginScrollView(detailScroll);
            if (embeddedEditor != null) embeddedEditor.OnInspectorGUI();
            EditorGUILayout.EndScrollView();
        }

        private ItemDefinition ForgeNew() => newKind == NewItemKind.Weapon
            ? CreateAsset<WeaponDefinition>(WeaponsFolder)
            : CreateAsset<ConsumableDefinition>(ConsumablesFolder);

        private T CreateAsset<T>(string folder) where T : ItemDefinition
        {
            EnsureFolder(folder);
            string safeName = string.IsNullOrWhiteSpace(newName) ? typeof(T).Name : newName.Trim();
            string path = AssetDatabase.GenerateUniqueAssetPath($"{folder}/{safeName}.asset");

            var asset = CreateInstance<T>();
            SetDisplayName(asset, safeName);
            AssetDatabase.CreateAsset(asset, path);
            AssetDatabase.SaveAssets();
            return asset;
        }

        // The runtime item reads its name from _displayName; seed it so a freshly
        // forged asset doesn't bake into an "empty" item.
        private static void SetDisplayName(ItemDefinition asset, string displayName)
        {
            var so = new SerializedObject(asset);
            so.FindProperty("displayName").stringValue = displayName;
            so.ApplyModifiedPropertiesWithoutUndo();
        }

        private void Select(ItemDefinition item)
        {
            selected = item;
            if (embeddedEditor != null) DestroyImmediate(embeddedEditor);
            embeddedEditor = item != null ? UnityEditor.Editor.CreateEditor(item) : null;
            Repaint();
        }

        private static void EnsureFolder(string folder)
        {
            if (AssetDatabase.IsValidFolder(folder)) return;
            string parent = Path.GetDirectoryName(folder).Replace('\\', '/');
            string leaf = Path.GetFileName(folder);
            if (!AssetDatabase.IsValidFolder(parent)) EnsureFolder(parent);
            AssetDatabase.CreateFolder(parent, leaf);
        }

        private static IEnumerable<T> LoadAll<T>() where T : ItemDefinition
        {
            foreach (string guid in AssetDatabase.FindAssets($"t:{typeof(T).Name}"))
            {
                var asset = AssetDatabase.LoadAssetAtPath<T>(AssetDatabase.GUIDToAssetPath(guid));
                if (asset != null) yield return asset;
            }
        }

        private void OnDisable()
        {
            if (embeddedEditor != null) DestroyImmediate(embeddedEditor);
        }
    }
}
