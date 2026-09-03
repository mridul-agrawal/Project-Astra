using UnityEditor;
using UnityEngine;
using ProjectAstra.Core;

namespace ProjectAstra.Core.Editor
{
    // Turns any [HubPick] string into its dropdown.
    [CustomPropertyDrawer(typeof(HubPickAttribute))]
    public sealed class HubPickDrawer : PropertyDrawer
    {
        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            EditorGUI.BeginProperty(position, label, property);
            HubPickField.Draw(position, property, label.text, ((HubPickAttribute)attribute).Kind);
            EditorGUI.EndProperty();
        }
    }
}
