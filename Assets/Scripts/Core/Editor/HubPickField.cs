using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using ProjectAstra.Core;

namespace ProjectAstra.Core.Editor
{
    // One id field, drawn as a dropdown of the names that exist. A name that has stopped existing is
    // flagged rather than quietly kept, because a rename should be visible.
    public static class HubPickField
    {
        private const string None = "(none)";
        private const string Missing = "  ← no longer exists";
        private const string NameOne = "New…";

        public static void Draw(Rect position, SerializedProperty property, string label, HubIdKind kind)
        {
            if (property == null) return;

            if (property.propertyType != SerializedPropertyType.String)
            {
                EditorGUI.PropertyField(position, property, new GUIContent(label));
                return;
            }

            Choices choices = Choices.For(kind, property.stringValue);

            int picked = EditorGUI.Popup(position, label, choices.IndexOfCurrent, choices.Labels);
            if (picked == choices.IndexOfCurrent) return;

            if (choices.IsAdd(picked)) NameIt.Open(kind, chosen => Introduce(property, kind, chosen));
            else Assign(property, choices.Values[picked]);
        }

        // A kind that lives in an asset gets one made; a gate or a signal only needs its name.
        private static void Introduce(SerializedProperty property, HubIdKind kind, string id)
        {
            if (HubAssets.CanCreate(kind) && HubAssets.Create(kind, id) == null) return;
            Assign(property, id);
        }

        // Re-resolved from its path, because naming a new one happens after this drawer has gone.
        private static void Assign(SerializedProperty property, string id)
        {
            SerializedObject owner = property.serializedObject;
            string path = property.propertyPath;

            owner.Update();
            SerializedProperty fresh = owner.FindProperty(path);
            if (fresh == null) return;

            fresh.stringValue = id;
            owner.ApplyModifiedProperties();
        }

        // What the dropdown offers, in the order it offers it.
        private readonly struct Choices
        {
            public readonly string[] Labels;
            public readonly string[] Values;
            public readonly int IndexOfCurrent;

            private Choices(string[] labels, string[] values, int current)
            {
                Labels = labels;
                Values = values;
                IndexOfCurrent = current;
            }

            public bool IsAdd(int index) => Values[index] == null;

            public static Choices For(HubIdKind kind, string current)
            {
                var labels = new List<string> { None };
                var values = new List<string> { string.Empty };

                foreach (string id in HubIds.Of(kind))
                {
                    labels.Add(id);
                    values.Add(id);
                }

                if (!string.IsNullOrEmpty(current) && !values.Contains(current))
                {
                    labels.Add(current + Missing);
                    values.Add(current);
                }

                if (HubIds.IsNamedInPlace(kind) || HubAssets.CanCreate(kind))
                {
                    labels.Add(HubAssets.CanCreate(kind) ? $"New {kind}…" : NameOne);
                    values.Add(null);
                }

                return new Choices(labels.ToArray(), values.ToArray(),
                    Mathf.Max(0, values.IndexOf(current ?? string.Empty)));
            }
        }

        // A one-field prompt for the kinds nothing declares, so naming a new gate is a deliberate
        // step rather than a typo waiting to happen.
        private sealed class NameIt : EditorWindow
        {
            private System.Action<string> chosen;
            private string typed = "";
            private bool focused;

            public static void Open(HubIdKind kind, System.Action<string> onChosen)
            {
                var window = CreateInstance<NameIt>();
                window.chosen = onChosen;
                window.titleContent = new GUIContent($"New {kind}");
                window.position = new Rect(Where(), new Vector2(260, 58));
                window.ShowPopup();
            }

            private static Vector2 Where() =>
                Event.current != null
                    ? GUIUtility.GUIToScreenPoint(Event.current.mousePosition)
                    : new Vector2(400f, 400f);

            private void OnGUI()
            {
                if (IsEscape()) { Close(); return; }

                GUI.SetNextControlName("name");
                typed = EditorGUILayout.TextField(typed);
                TakeFocusOnce();

                using (new EditorGUILayout.HorizontalScope())
                {
                    if (GUILayout.Button("Cancel")) Close();

                    using (new EditorGUI.DisabledScope(string.IsNullOrWhiteSpace(typed)))
                        if (GUILayout.Button("Use it") || IsReturn()) Accept();
                }
            }

            private void TakeFocusOnce()
            {
                if (focused) return;
                EditorGUI.FocusTextInControl("name");
                focused = true;
            }

            private static bool IsEscape() =>
                Event.current.type == EventType.KeyDown && Event.current.keyCode == KeyCode.Escape;

            private static bool IsReturn() =>
                Event.current.type == EventType.KeyDown &&
                Event.current.keyCode is KeyCode.Return or KeyCode.KeypadEnter;

            private void Accept()
            {
                if (string.IsNullOrWhiteSpace(typed)) return;

                chosen?.Invoke(typed.Trim());
                HubIds.Forget();
                Close();
            }

            private void OnLostFocus() => Close();
        }
    }
}
