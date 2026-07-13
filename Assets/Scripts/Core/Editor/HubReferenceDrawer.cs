using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace ProjectAstra.Core.Editor
{
    // Draws a [HubRef] ScriptableObject field as: object picker + [+] create-new + [↗] open-in-hub.
    // "+" makes a fresh asset of the field's type (a menu of concrete kinds when the field type is
    // abstract, e.g. ItemDefinition), wires it into the property, and jumps to it in the Data Hub.
    [CustomPropertyDrawer(typeof(HubRefAttribute))]
    public class HubReferenceDrawer : PropertyDrawer
    {
        private const float ButtonWidth = 24f;
        private const float Gap = 2f;

        public override float GetPropertyHeight(SerializedProperty property, GUIContent label) =>
            EditorGUIUtility.singleLineHeight;

        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            if (property.propertyType != SerializedPropertyType.ObjectReference)
            {
                EditorGUI.PropertyField(position, property, label);
                return;
            }

            EditorGUI.BeginProperty(position, label, property);

            float buttonsWidth = ButtonWidth * 2 + Gap;
            var fieldRect = new Rect(position.x, position.y, position.width - buttonsWidth - Gap, position.height);
            var newRect = new Rect(fieldRect.xMax + Gap, position.y, ButtonWidth, position.height);
            var openRect = new Rect(newRect.xMax + Gap, position.y, ButtonWidth, position.height);

            Type elementType = ElementType();
            EditorGUI.ObjectField(fieldRect, property, elementType, label);

            if (GUI.Button(newRect, new GUIContent("+", $"Create a new {elementType.Name}")))
                OnCreateClicked(property, elementType);

            using (new EditorGUI.DisabledScope(property.objectReferenceValue == null))
                if (GUI.Button(openRect, new GUIContent("↗", "Open in Data Hub")))
                    DataHubAssets.RequestNavigate(property.objectReferenceValue);

            EditorGUI.EndProperty();
        }

        private Type ElementType()
        {
            Type t = fieldInfo.FieldType;
            if (t.IsArray) return t.GetElementType();
            if (t.IsGenericType && t.GetGenericTypeDefinition() == typeof(List<>)) return t.GetGenericArguments()[0];
            return t;
        }

        private void OnCreateClicked(SerializedProperty property, Type elementType)
        {
            if (!elementType.IsAbstract)
            {
                CreateAndAssign(property, elementType);
                return;
            }

            var menu = new GenericMenu();
            foreach (Type concrete in TypeCache.GetTypesDerivedFrom(elementType))
            {
                if (concrete.IsAbstract) continue;
                Type captured = concrete;
                menu.AddItem(new GUIContent($"New {concrete.Name}"), false, () => CreateAndAssign(property, captured));
            }
            menu.ShowAsContext();
        }

        // Re-resolve the property from its object + path: creation can run deferred (menu callback),
        // by which time the original SerializedProperty may be stale.
        private static void CreateAndAssign(SerializedProperty property, Type concrete)
        {
            SerializedObject so = property.serializedObject;
            string path = property.propertyPath;

            ScriptableObject asset = DataHubAssets.Create(concrete, $"New {concrete.Name}");

            so.Update();
            SerializedProperty fresh = so.FindProperty(path);
            fresh.objectReferenceValue = asset;
            so.ApplyModifiedProperties();

            DataHubAssets.RequestNavigate(asset);
        }
    }
}
