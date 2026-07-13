using ProjectAstra.Core;
using ProjectAstra.Core.Combat;
using ProjectAstra.Core.Stats;
using ProjectAstra.Core.Units;

namespace ProjectAstra.Core.Tests
{
    // Test-only stock item catalog. These mirror the values that used to live as static factories
    // on WeaponData / ConsumableData before those were migrated to authored ScriptableObjects. Tests
    // build runtime structs directly from here so they stay hermetic (no asset loading) while the
    // shipping game reads its items from assets. Keep in sync with the corresponding .asset files.
    public static class TestItems
    {
        // --- Weapons ---

        public static WeaponData IronSword => new()
        {
            name = "Loha Khadga", weaponType = WeaponType.Sword, damageType = DamageType.Physical,
            tier = WeaponTier.Iron, minRank = WeaponRank.E,
            might = 5, hit = 90, crit = 0, weight = 5, minRange = 1, maxRange = 1,
            maxUses = 45, currentUses = 45,
        };

        public static WeaponData IronLance => new()
        {
            name = "Loha Shula", weaponType = WeaponType.Lance, damageType = DamageType.Physical,
            tier = WeaponTier.Iron, minRank = WeaponRank.E,
            might = 7, hit = 80, crit = 0, weight = 8, minRange = 1, maxRange = 1,
            maxUses = 45, currentUses = 45,
        };

        public static WeaponData IronAxe => new()
        {
            name = "Loha Parashu", weaponType = WeaponType.Axe, damageType = DamageType.Physical,
            tier = WeaponTier.Iron, minRank = WeaponRank.E,
            might = 8, hit = 75, crit = 0, weight = 10, minRange = 1, maxRange = 1,
            maxUses = 45, currentUses = 45,
        };

        public static WeaponData IronBow => new()
        {
            name = "Loha Dhanush", weaponType = WeaponType.Bow, damageType = DamageType.Physical,
            tier = WeaponTier.Iron, minRank = WeaponRank.E,
            might = 6, hit = 85, crit = 0, weight = 5, minRange = 2, maxRange = 2,
            maxUses = 45, currentUses = 45,
            effectivenessTargets = new[] { ClassType.Flying },
        };

        public static WeaponData SteelSword => new()
        {
            name = "Tamra Khadga", weaponType = WeaponType.Sword, damageType = DamageType.Physical,
            tier = WeaponTier.Steel, minRank = WeaponRank.D,
            might = 8, hit = 75, crit = 0, weight = 10, minRange = 1, maxRange = 1,
            maxUses = 30, currentUses = 30,
        };

        public static WeaponData SilverSword => new()
        {
            name = "Rajata Khadga", weaponType = WeaponType.Sword, damageType = DamageType.Physical,
            tier = WeaponTier.Silver, minRank = WeaponRank.B,
            might = 13, hit = 80, crit = 0, weight = 11, minRange = 1, maxRange = 1,
            maxUses = 20, currentUses = 20,
        };

        public static WeaponData KillerSword => new()
        {
            name = "Nishada Khadga", weaponType = WeaponType.Sword, damageType = DamageType.Physical,
            tier = WeaponTier.Killer, minRank = WeaponRank.B,
            might = 9, hit = 75, crit = 30, weight = 9, minRange = 1, maxRange = 1,
            maxUses = 20, currentUses = 20,
        };

        public static WeaponData BraveSword => new()
        {
            name = "Sarvasva Khadga", weaponType = WeaponType.Sword, damageType = DamageType.Physical,
            tier = WeaponTier.Silver, minRank = WeaponRank.B, brave = true,
            might = 9, hit = 75, crit = 0, weight = 12, minRange = 1, maxRange = 1,
            maxUses = 30, currentUses = 30,
        };

        public static WeaponData Fire => new()
        {
            name = "Agni", weaponType = WeaponType.AnimaTome, damageType = DamageType.Magical,
            magicSchool = MagicSchool.Anima, tier = WeaponTier.Iron, minRank = WeaponRank.E,
            might = 5, hit = 90, crit = 0, weight = 4, minRange = 1, maxRange = 2,
            maxUses = 40, currentUses = 40,
        };

        public static WeaponData Lightning => new()
        {
            name = "Sattva Jyoti", weaponType = WeaponType.LightTome, damageType = DamageType.Magical,
            magicSchool = MagicSchool.Light, tier = WeaponTier.Iron, minRank = WeaponRank.E,
            might = 5, hit = 95, crit = 5, weight = 6, minRange = 1, maxRange = 2,
            maxUses = 35, currentUses = 35,
            effectivenessTargets = new[] { ClassType.Monster },
        };

        public static WeaponData Flux => new()
        {
            name = "Tamasa Bindu", weaponType = WeaponType.DarkTome, damageType = DamageType.Magical,
            magicSchool = MagicSchool.Dark, tier = WeaponTier.Iron, minRank = WeaponRank.E,
            might = 7, hit = 80, crit = 0, weight = 8, minRange = 1, maxRange = 2,
            maxUses = 45, currentUses = 45,
        };

        public static WeaponData Heal => new()
        {
            name = "Chikitsa", weaponType = WeaponType.Staff, damageType = DamageType.Magical,
            staffEffect = StaffEffect.Heal, minRank = WeaponRank.E,
            might = 10, hit = 100, crit = 0, weight = 2, minRange = 1, maxRange = 1,
            maxUses = 30, currentUses = 30,
        };

        public static WeaponData Mend => new()
        {
            name = "Sukhada", weaponType = WeaponType.Staff, damageType = DamageType.Magical,
            staffEffect = StaffEffect.Heal, minRank = WeaponRank.C,
            might = 20, hit = 100, crit = 0, weight = 4, minRange = 1, maxRange = 1,
            maxUses = 20, currentUses = 20,
        };

        public static WeaponData Recover => new()
        {
            name = "Kayakalpa", weaponType = WeaponType.Staff, damageType = DamageType.Magical,
            staffEffect = StaffEffect.FullHeal, minRank = WeaponRank.B,
            might = 0, hit = 100, crit = 0, weight = 6, minRange = 1, maxRange = 1,
            maxUses = 15, currentUses = 15,
        };

        public static WeaponData Physic => new()
        {
            name = "Dooradarshi", weaponType = WeaponType.Staff, damageType = DamageType.Magical,
            staffEffect = StaffEffect.Ranged, minRank = WeaponRank.B,
            might = 10, hit = 100, crit = 0, weight = 4, minRange = 1, maxRange = 1,
            maxUses = 15, currentUses = 15,
        };

        public static WeaponData Fortify => new()
        {
            name = "Sarva Raksha", weaponType = WeaponType.Staff, damageType = DamageType.Magical,
            staffEffect = StaffEffect.AreaOfEffect, minRank = WeaponRank.A,
            might = 10, hit = 100, crit = 0, weight = 8, minRange = 0, maxRange = 0,
            maxUses = 8, currentUses = 8,
        };

        // --- Consumables ---

        public static ConsumableData Vulnerary => new()
        {
            name = "Sanjivani", type = ConsumableType.Vulnerary,
            magnitude = 10, maxUses = 3, currentUses = 3,
        };

        public static ConsumableData AmritaVastra => new()
        {
            name = "Amrita Vastra", type = ConsumableType.StatBooster,
            targetStat = StatIndex.HP, magnitude = 7, maxUses = 1, currentUses = 1,
        };

        public static ConsumableData ShaktiMudrika => new()
        {
            name = "Shakti Mudrika", type = ConsumableType.StatBooster,
            targetStat = StatIndex.Str, magnitude = 2, maxUses = 1, currentUses = 1,
        };

        public static ConsumableData VidyaDhuli => new()
        {
            name = "Vidya Dhuli", type = ConsumableType.StatBooster,
            targetStat = StatIndex.Mag, magnitude = 2, maxUses = 1, currentUses = 1,
        };

        public static ConsumableData SutraGrantha => new()
        {
            name = "Sutra Grantha", type = ConsumableType.StatBooster,
            targetStat = StatIndex.Skl, magnitude = 2, maxUses = 1, currentUses = 1,
        };

        public static ConsumableData VayuPankha => new()
        {
            name = "Vayu Pankha", type = ConsumableType.StatBooster,
            targetStat = StatIndex.Spd, magnitude = 2, maxUses = 1, currentUses = 1,
        };

        public static ConsumableData DeviPratima => new()
        {
            name = "Devi Pratima", type = ConsumableType.StatBooster,
            targetStat = StatIndex.Niyati, magnitude = 2, maxUses = 1, currentUses = 1,
        };

        public static ConsumableData NagaKavach => new()
        {
            name = "Naga Kavach", type = ConsumableType.StatBooster,
            targetStat = StatIndex.Def, magnitude = 2, maxUses = 1, currentUses = 1,
        };

        public static ConsumableData RakshaSutra => new()
        {
            name = "Raksha Sutra", type = ConsumableType.StatBooster,
            targetStat = StatIndex.Res, magnitude = 2, maxUses = 1, currentUses = 1,
        };

        public static ConsumableData DehaMudrika => new()
        {
            name = "Deha Mudrika", type = ConsumableType.StatBooster,
            targetStat = StatIndex.Con, magnitude = 2, maxUses = 1, currentUses = 1,
        };
    }
}
