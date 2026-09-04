using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using UnityEditor;
using UnityEngine;
using ProjectAstra.Core.Quests;

namespace ProjectAstra.Core.Editor
{
    // What a stage does when it starts or finishes is one of several kinds, and nothing used to ask
    // which: adding an entry left a blank row with no way to say what it should be.
    [CustomPropertyDrawer(typeof(QuestEvent), true)]
    public sealed class QuestEventDrawer : PropertyDrawer
    {
        private const float Gap = 2f;
        private const float ChangeWidth = 60f;

        public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
        {
            float line = EditorGUIUtility.singleLineHeight + Gap;
            if (NothingChosenYet(property) || !property.isExpanded) return line;

            float height = line;
            foreach (SerializedProperty child in WhatItNeeds(property))
                height += EditorGUI.GetPropertyHeight(child, true) + Gap;

            return height;
        }

        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            var row = new Rect(position.x, position.y, position.width, EditorGUIUtility.singleLineHeight);

            if (NothingChosenYet(property))
            {
                DrawChooser(row, property);
                return;
            }

            DrawHeader(row, property);
            if (property.isExpanded) DrawFields(position, row, property);
        }

        // An entry with no kind yet is the whole reason this drawer exists, so it says so and hands
        // over the list rather than leaving an empty row.
        private static void DrawChooser(Rect row, SerializedProperty property)
        {
            if (EditorGUI.DropdownButton(row, new GUIContent("Choose what this does"), FocusType.Keyboard))
                Offer(property, canClear: false);
        }

        private static void DrawHeader(Rect row, SerializedProperty property)
        {
            var title = new Rect(row.x, row.y, row.width - ChangeWidth - Gap, row.height);
            property.isExpanded =
                EditorGUI.Foldout(title, property.isExpanded, Readable(KindOf(property)), true);

            var change = new Rect(row.xMax - ChangeWidth, row.y, ChangeWidth, row.height);
            if (GUI.Button(change, "Change", EditorStyles.miniButton)) Offer(property, canClear: true);
        }

        private static void DrawFields(Rect position, Rect row, SerializedProperty property)
        {
            using (new EditorGUI.IndentLevelScope())
            {
                float y = row.yMax + Gap;

                foreach (SerializedProperty child in WhatItNeeds(property))
                {
                    float height = EditorGUI.GetPropertyHeight(child, true);
                    EditorGUI.PropertyField(new Rect(position.x, y, position.width, height), child, true);
                    y += height + Gap;
                }
            }
        }

        // Re-found from its path when the choice is made, because a menu answers after this drawer
        // has already gone.
        private static void Offer(SerializedProperty property, bool canClear)
        {
            SerializedObject owner = property.serializedObject;
            string path = property.propertyPath;
            var menu = new GenericMenu();

            foreach (Type kind in Kinds())
            {
                Type chosen = kind;
                menu.AddItem(new GUIContent(Readable(chosen.Name)), false, () => Become(owner, path, chosen));
            }

            if (canClear)
            {
                menu.AddSeparator("");
                menu.AddItem(new GUIContent("Nothing yet"), false, () => Become(owner, path, null));
            }

            menu.ShowAsContext();
        }

        private static void Become(SerializedObject owner, string path, Type kind)
        {
            owner.Update();

            SerializedProperty fresh = owner.FindProperty(path);
            if (fresh == null) return;

            fresh.managedReferenceValue = kind != null ? Activator.CreateInstance(kind) : null;
            fresh.isExpanded = true;
            owner.ApplyModifiedProperties();
        }

        // Found rather than listed, so a new kind of effect is offered the moment it is written.
        public static IEnumerable<Type> Kinds() =>
            TypeCache.GetTypesDerivedFrom<QuestEvent>()
                .Where(kind => !kind.IsAbstract && kind.GetConstructor(Type.EmptyTypes) != null)
                .OrderBy(kind => kind.Name);

        // PlayAuthoredEventEvent reads as "Play authored event", so the list is picked from by what
        // each one does rather than by what it is called in code.
        public static string Readable(string typeName)
        {
            const string suffix = "Event";

            string trimmed = typeName.EndsWith(suffix) && typeName.Length > suffix.Length
                ? typeName.Substring(0, typeName.Length - suffix.Length)
                : typeName;

            string spaced = Regex.Replace(trimmed, "(?<=[a-z])(?=[A-Z])", " ");
            return spaced.Length == 0
                ? typeName
                : char.ToUpperInvariant(spaced[0]) + spaced.Substring(1).ToLowerInvariant();
        }

        private static bool NothingChosenYet(SerializedProperty property) =>
            string.IsNullOrEmpty(property.managedReferenceFullTypename);

        private static string KindOf(SerializedProperty property)
        {
            string full = property.managedReferenceFullTypename;
            int dot = full.LastIndexOf('.');
            return dot >= 0 ? full.Substring(dot + 1) : full;
        }

        private static List<SerializedProperty> WhatItNeeds(SerializedProperty property)
        {
            var found = new List<SerializedProperty>();

            SerializedProperty child = property.Copy();
            SerializedProperty end = property.GetEndProperty();

            for (bool more = child.NextVisible(true);
                 more && !SerializedProperty.EqualContents(child, end);
                 more = child.NextVisible(false))
                found.Add(child.Copy());

            return found;
        }
    }
}
