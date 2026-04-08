using UnityEditor;
using UnityEngine;
using ProjectAstra.Core;

namespace ProjectAstra.Core.Editor
{
    public static class UnitDefinitionGenerator
    {
        const string ClassFolder = "Assets/ScriptableObjects/Units/Classes";
        const string OutputFolder = "Assets/ScriptableObjects/Units/Characters";

        [MenuItem("Project Astra/Units/Generate Test Unit Definitions")]
        public static void Generate()
        {
            EnsureFolderExists();

            CreateUnit("Arjun",  "arjun",  "Lord",     1, StatArray.From(20, 7, 2, 6, 8, 5, 3, 7, 6), StatArray.From(70, 50, 20, 40, 45, 30, 25, 0, 35));
            CreateUnit("Karna",  "karna",  "Cavalier", 1, StatArray.From(22, 7, 0, 5, 6, 6, 1, 9, 4), StatArray.From(65, 45, 5, 35, 40, 35, 15, 0, 30));
            CreateUnit("Vidya",  "vidya",  "Mage",     1, StatArray.From(17, 1, 8, 6, 7, 2, 6, 4, 5), StatArray.From(50, 10, 60, 40, 35, 15, 50, 0, 40));
            CreateUnit("Priya",  "priya",  "Cleric",   1, StatArray.From(16, 1, 6, 5, 6, 1, 8, 3, 7), StatArray.From(45, 5, 50, 35, 30, 10, 55, 0, 50));

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log($"UnitDefinitionGenerator: Created test unit definitions in {OutputFolder}");
        }

        private static void CreateUnit(string unitName, string unitId, string className, int level,
            StatArray baseStats, StatArray growths)
        {
            string path = $"{OutputFolder}/{unitName}.asset";
            if (AssetDatabase.LoadAssetAtPath<UnitDefinition>(path) != null) return;

            var classDef = AssetDatabase.LoadAssetAtPath<ClassDefinition>($"{ClassFolder}/{className}.asset");
            if (classDef == null)
            {
                Debug.LogWarning($"UnitDefinitionGenerator: Class '{className}' not found. Run 'Generate Base Class Definitions' first.");
                return;
            }

            var asset = ScriptableObject.CreateInstance<UnitDefinition>();
            var so = new SerializedObject(asset);
            so.FindProperty("_unitName").stringValue = unitName;
            so.FindProperty("_unitId").stringValue = unitId;
            so.FindProperty("_defaultClass").objectReferenceValue = classDef;
            so.FindProperty("_baseLevel").intValue = level;
            SetStatArray(so, "_baseStats", baseStats);
            SetStatArray(so, "_personalGrowths", growths);
            so.ApplyModifiedPropertiesWithoutUndo();

            AssetDatabase.CreateAsset(asset, path);
        }

        private static void SetStatArray(SerializedObject so, string propName, StatArray values)
        {
            var prop = so.FindProperty(propName);
            var arr = prop.FindPropertyRelative("_values");
            arr.arraySize = StatArray.Length;
            for (int i = 0; i < StatArray.Length; i++)
                arr.GetArrayElementAtIndex(i).intValue = values[(StatIndex)i];
        }

        private static void EnsureFolderExists()
        {
            if (!AssetDatabase.IsValidFolder("Assets/ScriptableObjects/Units"))
                AssetDatabase.CreateFolder("Assets/ScriptableObjects", "Units");
            if (!AssetDatabase.IsValidFolder(OutputFolder))
                AssetDatabase.CreateFolder("Assets/ScriptableObjects/Units", "Characters");
        }
    }
}
