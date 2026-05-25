using System;
using System.Collections.Generic;
using ProjectAstra.Core.Dialogue;
using UnityEditor;
using UnityEngine;

namespace ProjectAstra.Core.Editor
{
    // Draws a [SpeakerId] string as a dropdown: "(none)", the narrator, then every
    // DialogueSpeaker asset in the project. Designers pick a speaker instead of
    // typing an id. An id that no longer matches any asset is shown flagged rather
    // than silently dropped.
    [CustomPropertyDrawer(typeof(SpeakerIdAttribute))]
    public class SpeakerIdDrawer : PropertyDrawer
    {
        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            if (property.propertyType != SerializedPropertyType.String)
            {
                EditorGUI.PropertyField(position, property, label);
                return;
            }

            BuildOptions(property.stringValue, out var labels, out var values);
            int current = Mathf.Max(0, Array.IndexOf(values, property.stringValue));

            EditorGUI.BeginProperty(position, label, property);
            int picked = EditorGUI.Popup(position, label.text, current, labels);
            if (picked >= 0 && values[picked] != property.stringValue)
                property.stringValue = values[picked];
            EditorGUI.EndProperty();
        }

        private static void BuildOptions(string current, out string[] labels, out string[] values)
        {
            var labelList = new List<string> { "(none)", "Narrator" };
            var valueList = new List<string> { string.Empty, DialogueSpeakerRegistry.NarratorId };

            foreach (var guid in AssetDatabase.FindAssets("t:" + nameof(DialogueSpeaker)))
            {
                var speaker = AssetDatabase.LoadAssetAtPath<DialogueSpeaker>(AssetDatabase.GUIDToAssetPath(guid));
                if (speaker == null || string.IsNullOrEmpty(speaker.SpeakerId) || valueList.Contains(speaker.SpeakerId))
                    continue;
                labelList.Add(speaker.SpeakerId);
                valueList.Add(speaker.SpeakerId);
            }

            if (!string.IsNullOrEmpty(current) && !valueList.Contains(current))
            {
                labelList.Add($"{current}  (unregistered)");
                valueList.Add(current);
            }

            labels = labelList.ToArray();
            values = valueList.ToArray();
        }
    }
}
