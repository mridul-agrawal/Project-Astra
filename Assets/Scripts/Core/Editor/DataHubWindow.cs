using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using ProjectAstra.Core.Units;

namespace ProjectAstra.Core.Editor
{
    // One window to author every unit-data ScriptableObject. The left rail picks a type, the middle
    // pane lists + creates assets of that type, the right pane edits the selected one with its
    // normal inspector. Inline [HubRef] "+ New" buttons jump here via DataHubAssets.NavigateRequested.
    public class DataHubWindow : EditorWindow
    {
        private enum Tab { Units, Classes, Weapons, Consumables, Loadouts, Problems }

        private Tab tab = Tab.Units;
        private UnityEngine.Object selected;
        private UnityEditor.Editor embedded;
        private string filter = "";
        private string newName = "";
        private Vector2 listScroll, detailScroll, problemsScroll;
        private List<HubProblem> problems;
        private UnityEngine.Object pendingNavigate;

        [MenuItem("Project Astra/Data Hub")]
        public static void Open()
        {
            var window = GetWindow<DataHubWindow>("Data Hub");
            window.minSize = new Vector2(880, 560);
        }

        private void OnEnable() => DataHubAssets.NavigateRequested += OnNavigate;

        private void OnDisable()
        {
            DataHubAssets.NavigateRequested -= OnNavigate;
            if (embedded != null) DestroyImmediate(embedded);
        }

        private void OnNavigate(UnityEngine.Object target)
        {
            if (target == null) return;
            if (TabForType(target.GetType(), out Tab matched)) tab = matched;
            Select(target);
            Repaint();
        }

        private void OnGUI()
        {
            EditorGUILayout.BeginHorizontal();
            DrawRail();
            if (tab == Tab.Problems) DrawProblems();
            else { DrawList(); DrawDetail(); }
            EditorGUILayout.EndHorizontal();

            // Deferred so tab/selection don't change mid-layout (avoids IMGUI layout mismatches).
            if (pendingNavigate != null)
            {
                OnNavigate(pendingNavigate);
                pendingNavigate = null;
            }
        }

        private void DrawRail()
        {
            EditorGUILayout.BeginVertical(GUILayout.Width(120));
            EditorGUILayout.LabelField("Data Hub", EditorStyles.boldLabel);
            EditorGUILayout.Space(4);
            foreach (Tab t in Enum.GetValues(typeof(Tab)))
            {
                bool on = tab == t;
                if (GUILayout.Toggle(on, t.ToString(), "Button") && !on)
                {
                    tab = t;
                    Select(null);
                    if (t == Tab.Problems) problems = null;   // recompute on entry
                }
            }
            EditorGUILayout.EndVertical();
        }

        private void DrawList()
        {
            Type type = TypeForTab(tab);
            EditorGUILayout.BeginVertical(GUILayout.Width(230));

            EditorGUILayout.LabelField(tab.ToString(), EditorStyles.boldLabel);
            EditorGUILayout.BeginHorizontal();
            newName = EditorGUILayout.TextField(newName);
            if (GUILayout.Button("+ New", GUILayout.Width(58))) CreateNew(type);
            EditorGUILayout.EndHorizontal();
            filter = EditorGUILayout.TextField("Filter", filter);
            EditorGUILayout.Space(2);

            listScroll = EditorGUILayout.BeginScrollView(listScroll);
            foreach (UnityEngine.Object asset in LoadAll(type))
            {
                if (!Matches(asset)) continue;
                bool on = selected == asset;
                if (GUILayout.Toggle(on, DisplayLabel(asset), "Button") && !on) Select(asset);
            }
            EditorGUILayout.EndScrollView();
            EditorGUILayout.EndVertical();
        }

        private void DrawDetail()
        {
            EditorGUILayout.BeginVertical();
            if (selected == null)
            {
                EditorGUILayout.HelpBox("Select an asset on the left, or create one with + New.", MessageType.Info);
                EditorGUILayout.EndVertical();
                return;
            }

            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField($"Editing: {DisplayLabel(selected)}", EditorStyles.boldLabel);
            using (new EditorGUI.DisabledScope(!FileNameDiffersFromLabel(selected)))
                if (GUILayout.Button("Rename file", GUILayout.Width(90))) RenameFileToMatch(selected);
            if (GUILayout.Button("Reveal in Project", GUILayout.Width(130))) EditorGUIUtility.PingObject(selected);
            EditorGUILayout.EndHorizontal();

            detailScroll = EditorGUILayout.BeginScrollView(detailScroll);
            if (embedded != null) embedded.OnInspectorGUI();
            EditorGUILayout.EndScrollView();
            EditorGUILayout.EndVertical();
        }

        private void DrawProblems()
        {
            EditorGUILayout.BeginVertical();

            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField("Problems", EditorStyles.boldLabel);
            if (GUILayout.Button("Refresh", GUILayout.Width(80))) problems = DataHubProblems.Collect();
            if (GUILayout.Button("Rescan UnitDatabase", GUILayout.Width(170)))
            {
                int count = DataHubAssets.RescanUnits();
                problems = DataHubProblems.Collect();
                ShowNotification(new GUIContent(count >= 0 ? $"UnitDatabase rebuilt: {count} units" : "No UnitDatabase asset found"));
            }
            EditorGUILayout.EndHorizontal();

            problems ??= DataHubProblems.Collect();

            problemsScroll = EditorGUILayout.BeginScrollView(problemsScroll);
            if (problems.Count == 0)
                EditorGUILayout.HelpBox("No problems found — every reference resolves.", MessageType.Info);
            foreach (HubProblem problem in problems)
            {
                EditorGUILayout.BeginHorizontal();
                EditorGUILayout.HelpBox(problem.Message, MessageType.Warning);
                using (new EditorGUI.DisabledScope(problem.Asset == null))
                    if (GUILayout.Button("Fix", GUILayout.Width(50), GUILayout.Height(38)))
                    {
                        Selection.activeObject = problem.Asset;
                        EditorGUIUtility.PingObject(problem.Asset);
                        pendingNavigate = problem.Asset;
                    }
                EditorGUILayout.EndHorizontal();
            }
            EditorGUILayout.EndScrollView();

            EditorGUILayout.EndVertical();
        }

        private void CreateNew(Type type)
        {
            string name = string.IsNullOrWhiteSpace(newName) ? $"New {type.Name}" : newName;
            ScriptableObject asset = DataHubAssets.Create(type, name);
            newName = "";
            GUI.FocusControl(null);
            Select(asset);
        }

        private void Select(UnityEngine.Object asset)
        {
            selected = asset;
            if (embedded != null) DestroyImmediate(embedded);
            embedded = asset != null ? UnityEditor.Editor.CreateEditor(asset) : null;
        }

        private bool Matches(UnityEngine.Object asset)
        {
            if (string.IsNullOrEmpty(filter)) return true;
            return DisplayLabel(asset).IndexOf(filter, StringComparison.OrdinalIgnoreCase) >= 0
                || asset.name.IndexOf(filter, StringComparison.OrdinalIgnoreCase) >= 0;
        }

        // What the catalog shows: the asset's own name field (className / unitName / displayName),
        // not the .asset file name. New assets are created with a generic file name and the designer
        // sets the real name in the inspector, so keying on the file name would list them all alike.
        private static string DisplayLabel(UnityEngine.Object asset)
        {
            string internalName = asset switch
            {
                UnitDefinition u => u.UnitName,
                ClassDefinition c => c.ClassName,
                ItemDefinition i => i.DisplayName,
                _ => null,
            };
            return string.IsNullOrWhiteSpace(internalName) ? asset.name : internalName;
        }

        private static bool FileNameDiffersFromLabel(UnityEngine.Object asset)
        {
            string label = DisplayLabel(asset);
            return !string.IsNullOrWhiteSpace(label) && asset.name != label;
        }

        // Renames the .asset file to match its display name, so the Project view agrees with the Hub.
        private void RenameFileToMatch(UnityEngine.Object asset)
        {
            if (!FileNameDiffersFromLabel(asset)) return;
            AssetDatabase.RenameAsset(AssetDatabase.GetAssetPath(asset), DisplayLabel(asset));
            AssetDatabase.SaveAssets();
        }

        private static List<UnityEngine.Object> LoadAll(Type type)
        {
            var list = new List<UnityEngine.Object>();
            foreach (string guid in AssetDatabase.FindAssets($"t:{type.Name}"))
            {
                var asset = AssetDatabase.LoadAssetAtPath(AssetDatabase.GUIDToAssetPath(guid), type);
                if (asset != null) list.Add(asset);
            }
            list.Sort((a, b) => string.Compare(a.name, b.name, StringComparison.OrdinalIgnoreCase));
            return list;
        }

        private static Type TypeForTab(Tab tab) => tab switch
        {
            Tab.Units => typeof(UnitDefinition),
            Tab.Classes => typeof(ClassDefinition),
            Tab.Weapons => typeof(WeaponDefinition),
            Tab.Consumables => typeof(ConsumableDefinition),
            Tab.Loadouts => typeof(InventoryLoadout),
            _ => typeof(UnitDefinition),
        };

        private static bool TabForType(Type type, out Tab tab)
        {
            if (type == typeof(UnitDefinition)) { tab = Tab.Units; return true; }
            if (type == typeof(ClassDefinition)) { tab = Tab.Classes; return true; }
            if (typeof(WeaponDefinition).IsAssignableFrom(type)) { tab = Tab.Weapons; return true; }
            if (typeof(ConsumableDefinition).IsAssignableFrom(type)) { tab = Tab.Consumables; return true; }
            if (type == typeof(InventoryLoadout)) { tab = Tab.Loadouts; return true; }
            tab = Tab.Units; return false;
        }
    }
}
