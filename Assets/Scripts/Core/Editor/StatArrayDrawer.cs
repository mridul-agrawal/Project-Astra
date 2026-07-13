using UnityEditor;
using UnityEngine;
using ProjectAstra.Core.Stats;

namespace ProjectAstra.Core.Editor
{
    // Draws a StatArray as a labeled 3x3 grid (HP Str Mag / Skl Spd Def / Res Con Niyati)
    // instead of the raw, unlabeled int[9] the default inspector shows. Applies everywhere the
    // struct appears: unit base stats, growths, and every class cap/growth/promotion array.
    [CustomPropertyDrawer(typeof(StatArray))]
    public class StatArrayDrawer : PropertyDrawer
    {
        private static readonly string[] StatLabels =
            { "HP", "Str", "Mag", "Skl", "Spd", "Def", "Res", "Con", "Niyati" };

        private const int Columns = 3;
        private const float CellLabelWidth = 44f;

        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            EditorGUI.BeginProperty(position, label, property);

            var values = property.FindPropertyRelative("values");
            EnsureNineSlots(values);

            float line = EditorGUIUtility.singleLineHeight;
            float pad = EditorGUIUtility.standardVerticalSpacing;

            EditorGUI.LabelField(new Rect(position.x, position.y, position.width, line), label, EditorStyles.boldLabel);

            float previousLabelWidth = EditorGUIUtility.labelWidth;
            EditorGUIUtility.labelWidth = CellLabelWidth;

            float cellWidth = position.width / Columns;
            for (int i = 0; i < StatArray.Length; i++)
            {
                var cell = CellRect(position, i, cellWidth, line, pad);
                EditorGUI.PropertyField(cell, values.GetArrayElementAtIndex(i), new GUIContent(StatLabels[i]));
            }

            EditorGUIUtility.labelWidth = previousLabelWidth;
            EditorGUI.EndProperty();
        }

        public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
        {
            int rows = (StatArray.Length + Columns - 1) / Columns;
            float line = EditorGUIUtility.singleLineHeight;
            float pad = EditorGUIUtility.standardVerticalSpacing;
            return line + pad + rows * (line + pad);
        }

        private static Rect CellRect(Rect position, int index, float cellWidth, float line, float pad)
        {
            int col = index % Columns;
            int row = index / Columns;
            float x = position.x + col * cellWidth;
            float y = position.y + line + pad + row * (line + pad);
            return new Rect(x, y, cellWidth - 4f, line);
        }

        // A default(StatArray) serializes with an empty backing array; size it to nine so every
        // slot draws (the runtime struct self-initializes to nine on first access anyway).
        private static void EnsureNineSlots(SerializedProperty values)
        {
            if (values.arraySize != StatArray.Length)
                values.arraySize = StatArray.Length;
        }
    }
}
