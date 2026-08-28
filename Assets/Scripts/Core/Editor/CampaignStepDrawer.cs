using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using ProjectAstra.Core.Flow;
using ProjectAstra.Core.Grid;
using ProjectAstra.Core.Gurukul;

namespace ProjectAstra.EditorTools
{
    // Draws a campaign step as: the Kind dropdown, then ONLY the id that matches it — a cutscene
    // id for a Cutscene step, a map-id popup for a Battle, a visit-id popup for a Gurukul visit.
    // A designer only ever sees the one field that applies, and picks from a list built out of the
    // real assets instead of typing a raw id.
    [CustomPropertyDrawer(typeof(CampaignStep))]
    public class CampaignStepDrawer : PropertyDrawer
    {
        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            var kind = property.FindPropertyRelative("kind");
            var row = new Rect(position.x, position.y, position.width, EditorGUIUtility.singleLineHeight);

            EditorGUI.PropertyField(row, kind);
            row.y += EditorGUIUtility.singleLineHeight + EditorGUIUtility.standardVerticalSpacing;

            DrawIdFieldFor((CampaignStepKind)kind.enumValueIndex, row, property);
        }

        public override float GetPropertyHeight(SerializedProperty property, GUIContent label) =>
            EditorGUIUtility.singleLineHeight * 2 + EditorGUIUtility.standardVerticalSpacing;

        private static void DrawIdFieldFor(CampaignStepKind kind, Rect row, SerializedProperty property)
        {
            switch (kind)
            {
                case CampaignStepKind.Battle:
                    DrawIdPopup(row, "Map", property.FindPropertyRelative("mapId"), MapIds());
                    break;
                case CampaignStepKind.HubVisit:
                    DrawIdPopup(row, "Visit", property.FindPropertyRelative("visitId"), VisitIds());
                    break;
                default:
                    EditorGUI.PropertyField(row, property.FindPropertyRelative("cutscene"));
                    break;
            }
        }

        private static void DrawIdPopup(Rect row, string label, SerializedProperty idProp, string[] ids)
        {
            if (ids.Length == 0)
            {
                EditorGUI.LabelField(row, label, $"(no {label.ToLowerInvariant()} assets found)");
                return;
            }

            int index = Array.IndexOf(ids, idProp.stringValue);
            EditorGUI.BeginChangeCheck();
            int picked = EditorGUI.Popup(row, label, Mathf.Max(index, 0), ids);
            if (EditorGUI.EndChangeCheck() && picked >= 0 && picked < ids.Length)
                idProp.stringValue = ids[picked];
        }

        private static string[] MapIds() => IdsOf<MapData>(map => map.MapId);
        private static string[] VisitIds() => IdsOf<GurukulVisit>(visit => visit.VisitId);

        private static string[] IdsOf<T>(Func<T, string> idOf) where T : ScriptableObject
        {
            var ids = new List<string>();
            foreach (string guid in AssetDatabase.FindAssets($"t:{typeof(T).Name}"))
            {
                var asset = AssetDatabase.LoadAssetAtPath<T>(AssetDatabase.GUIDToAssetPath(guid));
                string id = asset != null ? idOf(asset) : null;
                if (!string.IsNullOrEmpty(id)) ids.Add(id);
            }
            ids.Sort();
            return ids.ToArray();
        }
    }
}
