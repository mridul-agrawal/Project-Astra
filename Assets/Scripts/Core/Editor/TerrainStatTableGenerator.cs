using UnityEditor;
using UnityEngine;
using ProjectAstra.Core;

namespace ProjectAstra.Core.Editor
{
    public static class TerrainStatTableGenerator
    {
        [MenuItem("Project Astra/Map/Generate Terrain Stat Table")]
        public static void Generate()
        {
            string folder = "Assets/ScriptableObjects/Map";
            if (!AssetDatabase.IsValidFolder(folder))
                AssetDatabase.CreateFolder("Assets/ScriptableObjects", "Map");

            string path = $"{folder}/TerrainStatTable.asset";
            var table = AssetDatabase.LoadAssetAtPath<TerrainStatTable>(path);
            if (table == null)
            {
                table = ScriptableObject.CreateInstance<TerrainStatTable>();
                AssetDatabase.CreateAsset(table, path);
            }

            var so = new SerializedObject(table);
            var stats = so.FindProperty("_stats");
            stats.arraySize = 18;

            // 0 = impassable for that movement type
            //                                    Foot  Mount Armr  Fly   Pir   DEF  AVO  Heal  Interact
            SetStats(stats, TerrainType.Plain,           1,    1,    1,    1,    1,    0,   0,   0, false);
            SetStats(stats, TerrainType.Forest,          2,    3,    3,    1,    2,    1,  20,   0, false);
            SetStats(stats, TerrainType.Mountain,        4,    0,    0,    1,    4,    1,  30,   0, false);
            SetStats(stats, TerrainType.Peak,            0,    0,    0,    1,    0,    2,  40,   0, false);
            SetStats(stats, TerrainType.Water,           0,    0,    0,    1,    2,    0,   0,   0, false);
            SetStats(stats, TerrainType.Sea,             0,    0,    0,    1,    1,    0,   0,   0, false);
            SetStats(stats, TerrainType.River,           0,    0,    0,    1,    2,    0,   0,   0, false);
            SetStats(stats, TerrainType.Road,            1,    1,    1,    1,    1,    0,   0,   0, false);
            SetStats(stats, TerrainType.Village,         1,    1,    1,    1,    1,    0,  10,   0, true);
            SetStats(stats, TerrainType.Fort,            1,    1,    1,    1,    1,    2,  20,  10, false);
            SetStats(stats, TerrainType.Gate,            1,    1,    1,    1,    1,    3,  30,  10, false);
            SetStats(stats, TerrainType.Chest,           1,    1,    1,    1,    1,    0,   0,   0, true);
            SetStats(stats, TerrainType.Door,            0,    0,    0,    0,    0,    0,   0,   0, true);
            SetStats(stats, TerrainType.Wall,            0,    0,    0,    0,    0,    0,   0,   0, false);
            SetStats(stats, TerrainType.DestructibleWall,0,    0,    0,    0,    0,    0,   0,   0, false);
            SetStats(stats, TerrainType.Rubble,          2,    3,    3,    1,    2,    0,  10,   0, false);
            SetStats(stats, TerrainType.Sand,            2,    3,    2,    1,    1,    0,   5,   0, false);
            SetStats(stats, TerrainType.Void,            0,    0,    0,    0,    0,    0,   0,   0, false);

            so.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(table);
            AssetDatabase.SaveAssets();
            Debug.Log("TerrainStatTable generated with all terrain type stats.");
        }

        private static void SetStats(SerializedProperty statsArray, TerrainType terrain,
            int foot, int mounted, int armoured, int flying, int pirate,
            int def, int avo, int heal, bool interactable)
        {
            var entry = statsArray.GetArrayElementAtIndex((int)terrain);
            entry.FindPropertyRelative("moveCostFoot").intValue = foot;
            entry.FindPropertyRelative("moveCostMounted").intValue = mounted;
            entry.FindPropertyRelative("moveCostArmoured").intValue = armoured;
            entry.FindPropertyRelative("moveCostFlying").intValue = flying;
            entry.FindPropertyRelative("moveCostPirate").intValue = pirate;
            entry.FindPropertyRelative("defenceBonus").intValue = def;
            entry.FindPropertyRelative("avoidBonus").intValue = avo;
            entry.FindPropertyRelative("healPerTurn").intValue = heal;
            entry.FindPropertyRelative("isInteractable").boolValue = interactable;
        }
    }
}
