namespace ProjectAstra.Core
{
    public struct CombatForecast
    {
        public int AttackerHit;
        public int AttackerDamage;
        public int AttackerCritRate;
        public bool AttackerCanDouble;
        public int AttackerAS;
        public int AttackerCurrentHP;
        public int AttackerMaxHP;

        public int DefenderHit;
        public int DefenderDamage;
        public int DefenderCritRate;
        public bool DefenderCanDouble;
        public bool DefenderCanCounter;
        public int DefenderAS;
        public int DefenderCurrentHP;
        public int DefenderMaxHP;

        public static CombatForecast Compute(
            CombatantData attacker, CombatantData defender,
            int defenderTerrainDef, int defenderTerrainAvo,
            int attackerTerrainDef, int attackerTerrainAvo)
        {
            int atkAS = StatUtils.AttackSpeed(attacker.spd, attacker.weapon.weight, attacker.con);
            int defAS = StatUtils.AttackSpeed(defender.spd, defender.weapon.weight, defender.con);

            int atkHit = CombatEngine.ComputeAttackerHit(attacker.skl, attacker.niyati, attacker.weapon.hit);
            int defAvo = CombatEngine.ComputeAvoid(defAS, defender.niyati, defenderTerrainAvo);

            int atkDmg = CombatEngine.ComputeDamage(attacker.weapon.damageType,
                attacker.str, attacker.mag, attacker.weapon.might, 0,
                defender.def, defender.res, defenderTerrainDef);

            int atkCrit = CombatEngine.ComputeCritRate(attacker.skl, attacker.weapon.crit, attacker.classCrit, defender.niyati);

            bool canCounter = !defender.weapon.IsEmpty &&
                              defender.weapon.CanReachRange(attacker.distance);

            int defHit = 0, defDmg = 0, defCrit = 0;
            if (canCounter)
            {
                int defHitRaw = CombatEngine.ComputeAttackerHit(defender.skl, defender.niyati, defender.weapon.hit);
                int atkAvo = CombatEngine.ComputeAvoid(atkAS, attacker.niyati, attackerTerrainAvo);
                defHit = CombatEngine.ComputeDisplayedHit(defHitRaw, atkAvo);
                defDmg = CombatEngine.ComputeDamage(defender.weapon.damageType,
                    defender.str, defender.mag, defender.weapon.might, 0,
                    attacker.def, attacker.res, attackerTerrainDef);
                defCrit = CombatEngine.ComputeCritRate(defender.skl, defender.weapon.crit, defender.classCrit, attacker.niyati);
            }

            return new CombatForecast
            {
                AttackerHit = CombatEngine.ComputeDisplayedHit(atkHit, defAvo),
                AttackerDamage = atkDmg,
                AttackerCritRate = atkCrit,
                AttackerCanDouble = CombatEngine.CanDoubleAttack(atkAS, defAS),
                AttackerAS = atkAS,
                AttackerCurrentHP = attacker.currentHP,
                AttackerMaxHP = attacker.maxHP,

                DefenderHit = defHit,
                DefenderDamage = defDmg,
                DefenderCritRate = defCrit,
                DefenderCanDouble = canCounter && CombatEngine.CanDoubleAttack(defAS, atkAS),
                DefenderCanCounter = canCounter,
                DefenderAS = defAS,
                DefenderCurrentHP = defender.currentHP,
                DefenderMaxHP = defender.maxHP,
            };
        }
    }

    public struct CombatantData
    {
        public int str, mag, skl, spd, def, res, con, niyati;
        public int currentHP, maxHP;
        public int distance;
        public WeaponData weapon;
        public int classCrit; // UC-08. Added to the crit formula.

        public static CombatantData FromStats(StatArray stats, int currentHP, int maxHP, WeaponData weapon, int distance, int classCrit = 0)
        {
            return new CombatantData
            {
                str = stats[StatIndex.Str],
                mag = stats[StatIndex.Mag],
                skl = stats[StatIndex.Skl],
                spd = stats[StatIndex.Spd],
                def = stats[StatIndex.Def],
                res = stats[StatIndex.Res],
                con = stats[StatIndex.Con],
                niyati = stats[StatIndex.Niyati],
                currentHP = currentHP,
                maxHP = maxHP,
                weapon = weapon,
                distance = distance,
                classCrit = classCrit,
            };
        }
    }
}
