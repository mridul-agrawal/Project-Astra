namespace ProjectAstra.Core
{
    public static class WeaponTriangle
    {
        const int HitBonus = 15;
        const int DmgBonus = 1;
        const int AvoBonus = 15;

        public static int ComputeAdvantage(WeaponData attacker, WeaponData defender)
        {
            if (defender.IsEmpty) return 0;

            if (attacker.damageType == DamageType.Magical && defender.damageType == DamageType.Magical)
                return ComputeMagicAdvantage(attacker.magicSchool, defender.magicSchool);

            if (attacker.damageType == DamageType.Physical && defender.damageType == DamageType.Physical)
                return ComputePhysicalAdvantage(attacker.weaponType, defender.weaponType);

            return 0;
        }

        public static int ComputePhysicalAdvantage(WeaponType attacker, WeaponType defender)
        {
            if (attacker == defender) return 0;

            // Sword > Axe > Lance > Sword
            if (attacker == WeaponType.Sword && defender == WeaponType.Axe) return 1;
            if (attacker == WeaponType.Axe && defender == WeaponType.Lance) return 1;
            if (attacker == WeaponType.Lance && defender == WeaponType.Sword) return 1;

            if (attacker == WeaponType.Axe && defender == WeaponType.Sword) return -1;
            if (attacker == WeaponType.Lance && defender == WeaponType.Axe) return -1;
            if (attacker == WeaponType.Sword && defender == WeaponType.Lance) return -1;

            return 0;
        }

        public static int ComputeMagicAdvantage(MagicSchool attacker, MagicSchool defender)
        {
            if (attacker == MagicSchool.None || defender == MagicSchool.None) return 0;
            if (attacker == defender) return 0;

            // Anima > Dark > Light > Anima
            if (attacker == MagicSchool.Anima && defender == MagicSchool.Dark) return 1;
            if (attacker == MagicSchool.Dark && defender == MagicSchool.Light) return 1;
            if (attacker == MagicSchool.Light && defender == MagicSchool.Anima) return 1;

            if (attacker == MagicSchool.Dark && defender == MagicSchool.Anima) return -1;
            if (attacker == MagicSchool.Light && defender == MagicSchool.Dark) return -1;
            if (attacker == MagicSchool.Anima && defender == MagicSchool.Light) return -1;

            return 0;
        }

        public static int GetHitBonus(int advantage) => advantage * HitBonus;
        public static int GetDamageBonus(int advantage) => advantage * DmgBonus;
        public static int GetAvoidBonus(int advantage) => advantage * AvoBonus;
    }
}
