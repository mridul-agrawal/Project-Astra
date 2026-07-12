using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using ProjectAstra.Core.Flow;
using ProjectAstra.Core.Grid;

namespace ProjectAstra.EditorTools
{
    // Draws a campaign step as: the Kind dropdown, then ONLY the id that matches it — a cutscene
    // id for a Cutscene step, or a map-id popup (built from the MapData assets in the project) for
    // a Battle step. A designer only ever sees the one field that applies, and picks a map from a
    // list instead of typing a raw id.
    [CustomPropertyDrawer(typeof(CampaignStep))]
    public class CampaignStepDrawer : PropertyDrawer
    {
        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            var kind = property.FindPropertyRelative("kind");
            var row = new Rect(position.x, position.y, position.width, EditorGUIUtility.singleLineHeight);

            EditorGUI.PropertyField(row, kind);
            row.y += EditorGUIUtility.singleLineHeight + EditorGUIUtility.standardVerticalSpacing;

            if ((CampaignStepKind)kind.enumValueIndex == CampaignStepKind.Battle)
                DrawMapIdPopup(row, property.FindPropertyRelative("mapId"));
            else
                EditorGUI.PropertyField(row, property.FindPropertyRelative("cutscene"));
        }

        public override float GetPropertyHeight(SerializedProperty property, GUIContent label) =>
            EditorGUIUtility.singleLineHeight * 2 + EditorGUIUtility.standardVerticalSpacing;

        private static void DrawMapIdPopup(Rect row, SerializedProperty mapIdProp)
        {
            string[] ids = AllMapIds();
            if (ids.Length == 0)
            {
                EditorGUI.LabelField(row, "Map", "(no MapData assets found)");
                return;
            }

            int index = System.Array.IndexOf(ids, mapIdProp.stringValue);
            EditorGUI.BeginChangeCheck();
            int picked = EditorGUI.Popup(row, "Map", Mathf.Max(index, 0), ids);
            if (EditorGUI.EndChangeCheck() && picked >= 0 && picked < ids.Length)
                mapIdProp.stringValue = ids[picked];
        }

        private static string[] AllMapIds()
        {
            var ids = new List<string>();
            foreach (string guid in AssetDatabase.FindAssets("t:MapData"))
            {
                var map = AssetDatabase.LoadAssetAtPath<MapData>(AssetDatabase.GUIDToAssetPath(guid));
                if (map != null && !string.IsNullOrEmpty(map.MapId)) ids.Add(map.MapId);
            }
            ids.Sort();
            return ids.ToArray();
        }
    }
}
