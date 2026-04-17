using System.Collections.Generic;
using UnityEngine;

namespace ProjectAstra.Core
{
    public struct HitResult
    {
        public string Attacker;
        public bool Hit;
        public bool Crit;
        public int Damage;
        public int TrueHitRoll;
    }

    public struct CombatResult
    {
        public List<HitResult> Hits;
        public bool AttackerDied;
        public bool DefenderDied;
        public bool UnlikelyDeath;
        public int AttackerHPAfter;
        public int DefenderHPAfter;
        public int TriangleAdvantage;
        public bool AttackerEffective;
        public bool AttackerFired;
        public bool DefenderFired;
    }

    public static class CombatRound
    {
        public static CombatResult Resolve(
            CombatantData attacker, CombatantData defender,
            int defTerrainDef, int defTerrainAvo,
            int atkTerrainDef, int atkTerrainAvo,
            IRng rng,
            ClassType attackerClassType = ClassType.Infantry,
            ClassType defenderClassType = ClassType.Infantry)
        {
            var hits = new List<HitResult>();
            int atkHP = attacker.currentHP;
            int defHP = defender.currentHP;
            bool unlikelyDeath = false;

            int atkAS = StatUtils.AttackSpeed(attacker.spd, attacker.weapon.weight, attacker.con);
            int defAS = StatUtils.AttackSpeed(defender.spd, defender.weapon.weight, defender.con);

            bool attackerCanFire = !attacker.weapon.IsEmpty && !attacker.weapon.IsBroken;
            bool canCounter = !defender.weapon.IsEmpty && !defender.weapon.IsBroken &&
                              defender.weapon.CanReachRange(attacker.distance);
            bool atkDoubles = CombatEngine.CanDoubleAttack(atkAS, defAS);
            bool defDoubles = canCounter && CombatEngine.CanDoubleAttack(defAS, atkAS);

            // Weapon triangle
            int atkAdvantage = WeaponTriangle.ComputeAdvantage(attacker.weapon, defender.weapon);
            int defAdvantage = -atkAdvantage;

            // Effectiveness
            bool atkEffective = attacker.weapon.IsEffectiveAgainst(defenderClassType);
            bool defEffective = canCounter && defender.weapon.IsEffectiveAgainst(attackerClassType);

            // Precompute attacker stats
            int atkMight = atkEffective
                ? CombatEngine.ComputeEffectiveMight(attacker.weapon.might, attacker.weapon, defenderClassType)
                : attacker.weapon.might;
            int atkHitRaw = CombatEngine.ComputeAttackerHit(attacker.skl, attacker.niyati,
                attacker.weapon.hit, WeaponTriangle.GetHitBonus(atkAdvantage));
            int defAvo = CombatEngine.ComputeAvoid(defAS, defender.niyati,
                defTerrainAvo + WeaponTriangle.GetAvoidBonus(defAdvantage));
            int atkDisplayedHit = CombatEngine.ComputeDisplayedHit(atkHitRaw, defAvo);
            int atkDmg = CombatEngine.ComputeDamage(attacker.weapon.damageType,
                attacker.str, attacker.mag, atkMight,
                WeaponTriangle.GetDamageBonus(atkAdvantage),
                defender.def, defender.res, defTerrainDef);
            int atkCrit = CombatEngine.ComputeCritRate(attacker.skl, attacker.weapon.crit, attacker.classCrit, defender.niyati);

            // Precompute defender stats
            int defDisplayedHit = 0, defDmg = 0, defCrit = 0;
            if (canCounter)
            {
                int defMight = defEffective
                    ? CombatEngine.ComputeEffectiveMight(defender.weapon.might, defender.weapon, attackerClassType)
                    : defender.weapon.might;
                int defHitRaw = CombatEngine.ComputeAttackerHit(defender.skl, defender.niyati,
                    defender.weapon.hit, WeaponTriangle.GetHitBonus(defAdvantage));
                int atkAvo = CombatEngine.ComputeAvoid(atkAS, attacker.niyati,
                    atkTerrainAvo + WeaponTriangle.GetAvoidBonus(atkAdvantage));
                defDisplayedHit = CombatEngine.ComputeDisplayedHit(defHitRaw, atkAvo);
                defDmg = CombatEngine.ComputeDamage(defender.weapon.damageType,
                    defender.str, defender.mag, defMight,
                    WeaponTriangle.GetDamageBonus(defAdvantage),
                    attacker.def, attacker.res, atkTerrainDef);
                defCrit = CombatEngine.ComputeCritRate(defender.skl, defender.weapon.crit, defender.classCrit, attacker.niyati);
            }

            bool defenderFired = false;

            // Step 1a: Attacker's first hit
            if (attackerCanFire)
            {
                var hit1 = ResolveHit("Attacker", atkDisplayedHit, atkDmg, atkCrit, rng);
                hits.Add(hit1);
                if (hit1.Hit) defHP = Mathf.Max(0, defHP - hit1.Damage);
                if (defHP <= 0) { if (hit1.TrueHitRoll < 30) unlikelyDeath = true; return Build(hits, atkHP, defHP, unlikelyDeath, atkAdvantage, atkEffective, attackerCanFire, defenderFired); }
            }

            // Step 1b: Brave second hit (before counter)
            if (attackerCanFire && attacker.weapon.brave)
            {
                var hitBrave = ResolveHit("Attacker", atkDisplayedHit, atkDmg, atkCrit, rng);
                hits.Add(hitBrave);
                if (hitBrave.Hit) defHP = Mathf.Max(0, defHP - hitBrave.Damage);
                if (defHP <= 0) { if (hitBrave.TrueHitRoll < 30) unlikelyDeath = true; return Build(hits, atkHP, defHP, unlikelyDeath, atkAdvantage, atkEffective, attackerCanFire, defenderFired); }
            }

            // Step 2: Defender's counterattack
            if (canCounter)
            {
                var hit2 = ResolveHit("Defender", defDisplayedHit, defDmg, defCrit, rng);
                hits.Add(hit2);
                defenderFired = true;
                if (hit2.Hit) atkHP = Mathf.Max(0, atkHP - hit2.Damage);
                if (atkHP <= 0) { if (hit2.TrueHitRoll < 30) unlikelyDeath = true; return Build(hits, atkHP, defHP, unlikelyDeath, atkAdvantage, atkEffective, attackerCanFire, defenderFired); }
            }

            // Step 3: Attacker's double attack
            if (attackerCanFire && atkDoubles)
            {
                var hit3 = ResolveHit("Attacker", atkDisplayedHit, atkDmg, atkCrit, rng);
                hits.Add(hit3);
                if (hit3.Hit) defHP = Mathf.Max(0, defHP - hit3.Damage);
                if (defHP <= 0) { if (hit3.TrueHitRoll < 30) unlikelyDeath = true; return Build(hits, atkHP, defHP, unlikelyDeath, atkAdvantage, atkEffective, attackerCanFire, defenderFired); }
            }

            // Step 4: Defender's double counterattack
            if (defDoubles)
            {
                var hit4 = ResolveHit("Defender", defDisplayedHit, defDmg, defCrit, rng);
                hits.Add(hit4);
                if (hit4.Hit) atkHP = Mathf.Max(0, atkHP - hit4.Damage);
                if (atkHP <= 0 && hit4.TrueHitRoll < 30) unlikelyDeath = true;
            }

            return Build(hits, atkHP, defHP, unlikelyDeath, atkAdvantage, atkEffective, attackerCanFire, defenderFired);
        }

        private static HitResult ResolveHit(string who, int displayedHit, int baseDamage, int critRate, IRng rng)
        {
            int rand1 = rng.Range(0, 100);
            int rand2 = rng.Range(0, 100);
            int trueHitRoll = CombatEngine.RollTrueHit(rand1, rand2);

            bool hit = CombatEngine.IsHit(displayedHit, trueHitRoll);
            bool crit = false;
            int damage = 0;

            if (hit)
            {
                int critRoll = rng.Range(0, 100);
                crit = CombatEngine.IsCrit(critRate, critRoll);
                damage = crit ? CombatEngine.ApplyCritMultiplier(baseDamage) : baseDamage;
            }

            return new HitResult
            {
                Attacker = who,
                Hit = hit,
                Crit = crit,
                Damage = damage,
                TrueHitRoll = trueHitRoll
            };
        }

        private static CombatResult Build(List<HitResult> hits, int atkHP, int defHP, bool unlikely, int triAdv, bool effective, bool attackerFired, bool defenderFired)
        {
            return new CombatResult
            {
                Hits = hits,
                AttackerDied = atkHP <= 0,
                DefenderDied = defHP <= 0,
                UnlikelyDeath = unlikely,
                AttackerHPAfter = atkHP,
                DefenderHPAfter = defHP,
                TriangleAdvantage = triAdv,
                AttackerEffective = effective,
                AttackerFired = attackerFired,
                DefenderFired = defenderFired,
            };
        }
    }
}
