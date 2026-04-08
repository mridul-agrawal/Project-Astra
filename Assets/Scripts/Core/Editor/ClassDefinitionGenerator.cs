using UnityEditor;
using UnityEngine;
using ProjectAstra.Core;

namespace ProjectAstra.Core.Editor
{
    public static class ClassDefinitionGenerator
    {
        const string OutputFolder = "Assets/ScriptableObjects/Units/Classes";

        [MenuItem("Project Astra/Units/Generate Base Class Definitions")]
        public static void Generate()
        {
            EnsureFolderExists();

            // Base classes
            CreateClass("Lord",          ClassType.Infantry, 5, MovementType.Foot,     new[] { WeaponType.Sword },                   true,  false, StatArray.From(60, 25, 20, 25, 25, 25, 22, 15, 30), StatArray.From(0, 5, 0, 5, 0, 0, 0, 0, 5),  2, StatArray.From(20, 7, 2, 6, 8, 5, 3, 7, 6));
            CreateClass("Cavalier",      ClassType.Cavalry,  7, MovementType.Mounted,  new[] { WeaponType.Sword, WeaponType.Lance },  true,  false, StatArray.From(52, 24, 2, 22, 22, 22, 20, 14, 30), StatArray.From(5, 5, 0, 0, 0, 5, 0, 0, 0),  2, StatArray.From(22, 7, 0, 5, 6, 6, 1, 9, 4));
            CreateClass("Knight",        ClassType.Armoured, 4, MovementType.Armoured, new[] { WeaponType.Lance, WeaponType.Sword },  true,  false, StatArray.From(60, 26, 2, 20, 18, 28, 20, 16, 30), StatArray.From(10, 5, 0, 0, -5, 10, 0, 0, 0), 2, StatArray.From(22, 9, 0, 4, 3, 10, 1, 12, 3));
            CreateClass("Myrmidon",      ClassType.Infantry, 5, MovementType.Foot,     new[] { WeaponType.Sword },                   true,  false, StatArray.From(48, 22, 2, 28, 28, 18, 20, 12, 30), StatArray.From(-5, 0, 0, 10, 10, -5, 0, 0, 0), 2, StatArray.From(18, 5, 0, 9, 10, 3, 2, 5, 5));
            CreateClass("Fighter",       ClassType.Infantry, 5, MovementType.Foot,     new[] { WeaponType.Axe },                     true,  false, StatArray.From(56, 28, 2, 20, 20, 20, 16, 16, 30), StatArray.From(10, 10, 0, 0, 0, 0, -5, 0, 0), 2, StatArray.From(24, 8, 0, 4, 5, 4, 1, 10, 3));
            CreateClass("Archer",        ClassType.Infantry, 5, MovementType.Foot,     new[] { WeaponType.Bow },                     true,  false, StatArray.From(50, 24, 2, 26, 24, 20, 18, 14, 30), StatArray.From(0, 5, 0, 5, 5, 0, 0, 0, 0),  2, StatArray.From(20, 6, 0, 7, 7, 4, 2, 7, 4));
            CreateClass("Mage",          ClassType.Mage,     5, MovementType.Foot,     new[] { WeaponType.AnimaTome },               true,  false, StatArray.From(44, 2, 26, 22, 22, 16, 26, 10, 30), StatArray.From(-5, 0, 10, 0, 0, -5, 10, 0, 0), 2, StatArray.From(17, 1, 8, 6, 7, 2, 6, 4, 5));
            CreateClass("Cleric",        ClassType.Healer,   5, MovementType.Foot,     new[] { WeaponType.Staff, WeaponType.LightTome }, true, false, StatArray.From(42, 2, 24, 22, 22, 14, 28, 10, 30), StatArray.From(-5, 0, 5, 5, 0, -5, 10, 0, 5), 2, StatArray.From(16, 1, 6, 5, 6, 1, 8, 3, 7));
            CreateClass("Thief",         ClassType.Thief,    6, MovementType.Thief,    new[] { WeaponType.Sword },                   true,  false, StatArray.From(44, 20, 2, 26, 28, 16, 18, 10, 30), StatArray.From(-5, 0, 0, 10, 10, -5, 0, 0, 0), 2, StatArray.From(18, 4, 0, 8, 11, 3, 1, 5, 6));
            CreateClass("PegasusKnight", ClassType.Flying,   7, MovementType.Flying,   new[] { WeaponType.Lance },                   true,  false, StatArray.From(46, 20, 18, 24, 26, 18, 26, 10, 30), StatArray.From(-5, 0, 5, 5, 5, -5, 10, 0, 0), 2, StatArray.From(18, 5, 4, 7, 9, 4, 7, 4, 5));
            CreateClass("WyvernRider",   ClassType.Flying,   7, MovementType.Flying,   new[] { WeaponType.Lance, WeaponType.Axe },   true,  false, StatArray.From(52, 26, 2, 20, 20, 26, 16, 16, 30), StatArray.From(10, 10, 0, 0, 0, 10, -5, 0, 0), 2, StatArray.From(22, 9, 0, 5, 6, 8, 1, 11, 3));
            CreateClass("Shaman",        ClassType.Mage,     5, MovementType.Foot,     new[] { WeaponType.DarkTome },                true,  false, StatArray.From(46, 2, 26, 22, 20, 18, 24, 12, 30), StatArray.From(0, 0, 10, 0, -5, 0, 5, 0, -5), 2, StatArray.From(18, 1, 8, 5, 5, 3, 6, 5, 3));
            CreateClass("Soldier",       ClassType.Infantry, 5, MovementType.Foot,     new[] { WeaponType.Lance },                   true,  false, StatArray.From(52, 24, 2, 22, 22, 22, 18, 14, 30), StatArray.From(5, 5, 0, 0, 0, 5, 0, 0, 0),  2, StatArray.From(20, 6, 0, 5, 5, 5, 1, 8, 4));

            // Promoted classes
            CreateClass("GreatLord",     ClassType.Infantry, 6, MovementType.Foot,     new[] { WeaponType.Sword, WeaponType.Lance },  false, true, StatArray.From(80, 30, 24, 30, 30, 28, 26, 18, 30), StatArray.From(5, 5, 5, 5, 5, 5, 5, 0, 5),  2, StatArray.From(3, 2, 1, 2, 2, 2, 1, 1, 0));
            CreateClass("Paladin",       ClassType.Cavalry,  8, MovementType.Mounted,  new[] { WeaponType.Sword, WeaponType.Lance, WeaponType.Axe }, false, true, StatArray.From(72, 28, 6, 26, 26, 28, 24, 16, 30), StatArray.From(5, 5, 0, 5, 0, 5, 5, 0, 0), 2, StatArray.From(4, 2, 1, 1, 1, 2, 2, 1, 0));
            CreateClass("General",       ClassType.Armoured, 5, MovementType.Armoured, new[] { WeaponType.Lance, WeaponType.Sword, WeaponType.Axe }, false, true, StatArray.From(80, 30, 4, 24, 22, 30, 24, 20, 30), StatArray.From(10, 10, 0, 5, 0, 10, 5, 0, 0), 2, StatArray.From(4, 2, 0, 1, 1, 2, 1, 2, 0));
            CreateClass("Swordmaster",   ClassType.Infantry, 6, MovementType.Foot,     new[] { WeaponType.Sword },                   false, true, StatArray.From(60, 26, 6, 30, 30, 22, 24, 14, 30), StatArray.From(0, 5, 0, 10, 10, 0, 0, 0, 0), 2, StatArray.From(3, 1, 0, 2, 2, 1, 1, 1, 0));
            CreateClass("Warrior",       ClassType.Infantry, 6, MovementType.Foot,     new[] { WeaponType.Axe, WeaponType.Bow },     false, true, StatArray.From(72, 30, 4, 24, 24, 24, 20, 18, 30), StatArray.From(10, 10, 0, 5, 0, 5, 0, 0, 0), 2, StatArray.From(4, 2, 0, 1, 1, 1, 1, 1, 0));
            CreateClass("Sniper",        ClassType.Infantry, 6, MovementType.Foot,     new[] { WeaponType.Bow },                     false, true, StatArray.From(64, 28, 4, 30, 28, 24, 22, 16, 30), StatArray.From(0, 5, 0, 10, 5, 5, 0, 0, 0), 2, StatArray.From(3, 2, 0, 2, 2, 1, 1, 1, 0));
            CreateClass("Sage",          ClassType.Mage,     6, MovementType.Foot,     new[] { WeaponType.AnimaTome, WeaponType.Staff }, false, true, StatArray.From(60, 6, 30, 26, 26, 20, 30, 12, 30), StatArray.From(0, 0, 10, 5, 5, 0, 10, 0, 0), 2, StatArray.From(3, 0, 2, 2, 1, 1, 2, 0, 0));
            CreateClass("Bishop",        ClassType.Healer,   6, MovementType.Foot,     new[] { WeaponType.Staff, WeaponType.LightTome }, false, true, StatArray.From(56, 6, 28, 26, 26, 18, 30, 12, 30), StatArray.From(0, 0, 5, 5, 5, 0, 10, 0, 5), 2, StatArray.From(3, 0, 2, 1, 1, 1, 2, 0, 0));
            CreateClass("Assassin",      ClassType.Thief,    7, MovementType.Thief,    new[] { WeaponType.Sword },                   false, true, StatArray.From(56, 24, 6, 30, 30, 20, 22, 12, 30), StatArray.From(0, 5, 0, 10, 10, 0, 0, 0, 0), 2, StatArray.From(2, 1, 0, 2, 2, 1, 1, 0, 0));
            CreateClass("FalconKnight",  ClassType.Flying,   8, MovementType.Flying,   new[] { WeaponType.Lance, WeaponType.Sword },  false, true, StatArray.From(60, 24, 22, 28, 30, 24, 30, 12, 30), StatArray.From(0, 0, 5, 5, 5, 0, 10, 0, 0), 2, StatArray.From(3, 1, 1, 2, 2, 1, 2, 0, 0));
            CreateClass("WyvernLord",    ClassType.Flying,   8, MovementType.Flying,   new[] { WeaponType.Lance, WeaponType.Axe, WeaponType.Sword }, false, true, StatArray.From(72, 30, 4, 24, 24, 30, 20, 18, 30), StatArray.From(10, 10, 0, 5, 0, 10, 0, 0, 0), 2, StatArray.From(4, 2, 0, 1, 1, 2, 0, 1, 0));
            CreateClass("Druid",         ClassType.Mage,     6, MovementType.Foot,     new[] { WeaponType.DarkTome, WeaponType.Staff }, false, true, StatArray.From(60, 4, 30, 26, 24, 22, 28, 14, 30), StatArray.From(0, 0, 10, 5, 0, 5, 5, 0, 0), 2, StatArray.From(3, 0, 2, 1, 1, 1, 2, 0, 0));
            CreateClass("Halberdier",    ClassType.Infantry, 6, MovementType.Foot,     new[] { WeaponType.Lance },                   false, true, StatArray.From(68, 28, 4, 26, 26, 26, 22, 16, 30), StatArray.From(5, 5, 0, 5, 5, 5, 0, 0, 0), 2, StatArray.From(3, 2, 0, 1, 1, 2, 1, 1, 0));

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log($"ClassDefinitionGenerator: Created class definitions in {OutputFolder}");
        }

        private static void CreateClass(string className, ClassType classType, int moveRange, MovementType moveType,
            WeaponType[] weapons, bool canPromote, bool isPromoted,
            StatArray caps, StatArray growthMods, int hpGain, StatArray promoBonuses)
        {
            string path = $"{OutputFolder}/{className}.asset";
            var existing = AssetDatabase.LoadAssetAtPath<ClassDefinition>(path);
            if (existing != null) return;

            var asset = ScriptableObject.CreateInstance<ClassDefinition>();
            var so = new SerializedObject(asset);
            so.FindProperty("_className").stringValue = className;
            so.FindProperty("_classType").enumValueIndex = (int)classType;
            so.FindProperty("_movementRange").intValue = moveRange;
            so.FindProperty("_movementType").enumValueIndex = (int)moveType;
            so.FindProperty("_canPromote").boolValue = canPromote;
            so.FindProperty("_isPromoted").boolValue = isPromoted;
            so.FindProperty("_hpGainOnLevelUp").intValue = hpGain;

            SetWeaponWhitelist(so, weapons);
            SetStatArray(so, "_statCaps", caps);
            SetStatArray(so, "_statGrowthModifiers", growthMods);
            SetStatArray(so, "_promotionBonuses", promoBonuses);

            so.ApplyModifiedPropertiesWithoutUndo();
            AssetDatabase.CreateAsset(asset, path);
        }

        private static void SetWeaponWhitelist(SerializedObject so, WeaponType[] weapons)
        {
            var prop = so.FindProperty("_weaponWhitelist");
            prop.arraySize = weapons.Length;
            for (int i = 0; i < weapons.Length; i++)
                prop.GetArrayElementAtIndex(i).enumValueIndex = (int)weapons[i];
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
                AssetDatabase.CreateFolder("Assets/ScriptableObjects/Units", "Classes");
        }
    }
}
