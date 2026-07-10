using UnityEditor;
using UnityEngine;
using ProjectAstra.Core.Flow;

namespace ProjectAstra.EditorTools
{
    // Draws a campaign step as: the Kind dropdown, then ONLY the id field that matches it
    // (cutscene id for a Cutscene step, map id for a Battle step). Keeps the irrelevant id
    // hidden so a designer only ever sees the one field that applies.
    [CustomPropertyDrawer(typeof(CampaignStep))]
    public class CampaignStepDrawer : PropertyDrawer
    {
        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            var kind = property.FindPropertyRelative("kind");
            var row = new Rect(position.x, position.y, position.width, EditorGUIUtility.singleLineHeight);

            EditorGUI.PropertyField(row, kind);
            row.y += EditorGUIUtility.singleLineHeight + EditorGUIUtility.standardVerticalSpacing;

            EditorGUI.PropertyField(row, property.FindPropertyRelative(IdFieldFor(kind)));
        }

        public override float GetPropertyHeight(SerializedProperty property, GUIContent label) =>
            EditorGUIUtility.singleLineHeight * 2 + EditorGUIUtility.standardVerticalSpacing;

        private static string IdFieldFor(SerializedProperty kind) =>
            (CampaignStepKind)kind.enumValueIndex == CampaignStepKind.Battle ? "map" : "cutscene";
    }
}
