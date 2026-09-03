using UnityEditor;
using UnityEngine;
using ProjectAstra.Core;
using ProjectAstra.Core.Hub;

namespace ProjectAstra.Core.Editor
{
    // Draws a door as the two decisions it really is: where it leads, and what it takes to open it.
    // The fields for a decision not yet made stay out of the way.
    [CustomPropertyDrawer(typeof(HubDoor))]
    public sealed class HubDoorDrawer : PropertyDrawer
    {
        private const float Gap = 2f;

        public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
        {
            float line = EditorGUIUtility.singleLineHeight + Gap;
            if (!property.isExpanded) return line;

            int rows = 5;                                          // id, position, verb, leads-to, house
            if (!GoesBack(property)) rows += 2;                    // spawn and facing in the room it leads to
            rows += 1;                                             // the gate
            if (IsGated(property)) rows += 1;                      // and what it says while shut

            return line * (rows + 1) + Gap;
        }

        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            var row = new Rect(position.x, position.y, position.width, EditorGUIUtility.singleLineHeight);

            property.isExpanded = EditorGUI.Foldout(row, property.isExpanded, Title(property, label), true);
            if (!property.isExpanded) return;

            using (new EditorGUI.IndentLevelScope())
            {
                Draw(ref row, property, "doorId", "Name");
                Draw(ref row, property, "position", "Stands at");
                Draw(ref row, property, "verb", "Reads as");

                DrawDestination(ref row, property);
                DrawGating(ref row, property);
            }
        }

        private static void DrawDestination(ref Rect row, SerializedProperty property)
        {
            Pick(ref row, property, "targetLocationId", "Leads to", HubIdKind.Location);

            if (GoesBack(property))
            {
                Next(ref row);
                EditorGUI.LabelField(row, " ", "Back the way she came in.", EditorStyles.miniLabel);
            }
            else
            {
                Draw(ref row, property, "targetSpawn", "Arrives at");
                Draw(ref row, property, "targetFacing", "Facing");
            }

            Draw(ref row, property, "houseIdentityId", "Which house");
        }

        private static void DrawGating(ref Rect row, SerializedProperty property)
        {
            Pick(ref row, property, "requiredGate", "Shut until", HubIdKind.Gate);

            // A door that can be shut owes her a reason, so the field only appears once it can be.
            if (IsGated(property))
                Pick(ref row, property, "deniedConversationId", "Says while shut", HubIdKind.Conversation);
        }

        private static GUIContent Title(SerializedProperty property, GUIContent label)
        {
            string id = Field(property, "doorId").stringValue;
            string to = Field(property, "targetLocationId").stringValue;
            string where = string.IsNullOrEmpty(to) ? "back" : to;

            return new GUIContent(string.IsNullOrEmpty(id) ? label.text : $"{id}  →  {where}");
        }

        private static bool GoesBack(SerializedProperty property) =>
            string.IsNullOrEmpty(Field(property, "targetLocationId").stringValue);

        private static bool IsGated(SerializedProperty property) =>
            !string.IsNullOrEmpty(Field(property, "requiredGate").stringValue);

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

        private static void Next(ref Rect row) => row.y += row.height + Gap;

        private static SerializedProperty Field(SerializedProperty property, string name) =>
            property.FindPropertyRelative(name);
    }
}
