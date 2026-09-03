using UnityEditor;
using UnityEngine;
using ProjectAstra.Core;
using ProjectAstra.Core.Hub.Events;

namespace ProjectAstra.Core.Editor
{
    // Draws an event action as what it is: the kind, then only the fields that kind uses.
    [CustomPropertyDrawer(typeof(HubEventAction))]
    public sealed class HubEventActionDrawer : PropertyDrawer
    {
        private const float Gap = 2f;

        public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
        {
            float line = EditorGUIUtility.singleLineHeight + Gap;
            if (!property.isExpanded) return line;

            HubEventActionShape shape = ShapeOf(property);
            float height = line * 2 + SummaryHeight(shape) + Gap;

            if (shape.UsesTarget) height += line;
            if (shape.UsesValue) height += line;
            if (shape.UsesPosition) height += line;
            if (shape.UsesFacing) height += line;
            if (shape.UsesSeconds) height += line;
            if (shape.UsesFlag) height += line;
            if (shape.UsesState) height += line;
            if (shape.UsesRoute) height += EditorGUI.GetPropertyHeight(Field(property, "route")) + Gap;

            return height;
        }

        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            var row = new Rect(position.x, position.y, position.width, EditorGUIUtility.singleLineHeight);

            SerializedProperty kind = Field(property, "kind");
            property.isExpanded = EditorGUI.Foldout(row, property.isExpanded, Title(property, label), true);
            if (!property.isExpanded) return;

            using (new EditorGUI.IndentLevelScope())
            {
                Next(ref row);
                EditorGUI.PropertyField(row, kind, new GUIContent("Do"));

                HubEventActionShape shape = ShapeOf(property);
                DrawSummary(ref row, shape);
                DrawFields(ref row, property, shape);
            }
        }

        // The kind read back as a sentence, so the list reads as a sequence of beats rather than a
        // column of enum names.
        private static GUIContent Title(SerializedProperty property, GUIContent label)
        {
            var kind = (HubEventActionKind)Field(property, "kind").enumValueIndex;
            HubEventActionShape shape = HubEventActionShape.Of(kind);

            string subject = shape.UsesTarget ? Field(property, "targetId").stringValue
                : shape.UsesValue ? Field(property, "valueId").stringValue
                : null;

            string name = ObjectNames.NicifyVariableName(kind.ToString());
            return new GUIContent(string.IsNullOrEmpty(subject) ? name : $"{name}  ·  {subject}");
        }

        private static void DrawSummary(ref Rect row, HubEventActionShape shape)
        {
            if (string.IsNullOrEmpty(shape.Summary)) return;

            row.y += row.height + Gap;
            row.height = SummaryHeight(shape);
            EditorGUI.LabelField(row, shape.Summary, EditorStyles.wordWrappedMiniLabel);
            row.height = EditorGUIUtility.singleLineHeight;
        }

        private static void DrawFields(ref Rect row, SerializedProperty property, HubEventActionShape shape)
        {
            if (shape.UsesTarget) Pick(ref row, property, "targetId", shape.TargetLabel, shape.TargetKind);
            if (shape.UsesValue) Pick(ref row, property, "valueId", shape.ValueLabel, shape.ValueKind);

            if (shape.UsesPosition) Draw(ref row, property, "position", "Where");
            if (shape.UsesFacing) Draw(ref row, property, "facing", "Facing");
            if (shape.UsesState) Draw(ref row, property, "state", "State");
            if (shape.UsesFlag) Draw(ref row, property, "flag", shape.FlagLabel);
            if (shape.UsesSeconds) Draw(ref row, property, "seconds", shape.SecondsLabel);

            if (shape.UsesRoute) DrawRoute(ref row, property);
        }

        // The action stores plain strings, so the dropdown is applied here rather than by an
        // attribute — one field means a different kind of name depending on the action.
        private static void Pick(ref Rect row, SerializedProperty property, string name, string label, HubIdKind kind)
        {
            Next(ref row);
            HubPickField.Draw(row, Field(property, name), label, kind);
        }

        private static void Draw(ref Rect row, SerializedProperty property, string name, string label)
        {
            Next(ref row);
            EditorGUI.PropertyField(row, Field(property, name), new GUIContent(label));
        }

        private static void DrawRoute(ref Rect row, SerializedProperty property)
        {
            SerializedProperty route = Field(property, "route");
            row.y += row.height + Gap;
            row.height = EditorGUI.GetPropertyHeight(route);
            EditorGUI.PropertyField(row, route, new GUIContent("Route"), true);
        }

        private static float SummaryHeight(HubEventActionShape shape) =>
            string.IsNullOrEmpty(shape.Summary)
                ? 0f
                : EditorStyles.wordWrappedMiniLabel.CalcHeight(new GUIContent(shape.Summary),
                    EditorGUIUtility.currentViewWidth - 60f) + Gap;

        private static void Next(ref Rect row)
        {
            row.y += row.height + Gap;
            row.height = EditorGUIUtility.singleLineHeight;
        }

        private static HubEventActionShape ShapeOf(SerializedProperty property) =>
            HubEventActionShape.Of((HubEventActionKind)Field(property, "kind").enumValueIndex);

        private static SerializedProperty Field(SerializedProperty property, string name) =>
            property.FindPropertyRelative(name);
    }
}
