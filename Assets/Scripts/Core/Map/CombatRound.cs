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
    }

    public static class CombatRound
    {
        public static CombatResult Resolve(
            CombatantData attacker, CombatantData defender,
            int defTerrainDef, int defTerrainAvo,
            int atkTerrainDef, int atkTerrainAvo,
            IRng rng)
        {
            var hits = new List<HitResult>();
            int atkHP = attacker.currentHP;
            int defHP = defender.currentHP;
            bool unlikelyDeath = false;

            int atkAS = StatUtils.AttackSpeed(attacker.spd, attacker.weapon.weight, attacker.con);
            int defAS = StatUtils.AttackSpeed(defender.spd, defender.weapon.weight, defender.con);

            bool canCounter = !defender.weapon.IsEmpty &&
                              defender.weapon.CanReachRange(attacker.distance);
            bool atkDoubles = CombatEngine.CanDoubleAttack(atkAS, defAS);
            bool defDoubles = canCounter && CombatEngine.CanDoubleAttack(defAS, atkAS);

            // Precompute displayed values
            int atkHitRaw = CombatEngine.ComputeAttackerHit(attacker.skl, attacker.niyati, attacker.weapon.hit);
            int defAvo = CombatEngine.ComputeAvoid(defAS, defender.niyati, defTerrainAvo);
            int atkDisplayedHit = CombatEngine.ComputeDisplayedHit(atkHitRaw, defAvo);
            int atkDmg = CombatEngine.ComputeDamage(attacker.weapon.damageType,
                attacker.str, attacker.mag, attacker.weapon.might, 0,
                defender.def, defender.res, defTerrainDef);
            int atkCrit = CombatEngine.ComputeCritRate(attacker.skl, attacker.weapon.crit, 0, defender.niyati);

            int defDisplayedHit = 0, defDmg = 0, defCrit = 0;
            if (canCounter)
            {
                int defHitRaw = CombatEngine.ComputeAttackerHit(defender.skl, defender.niyati, defender.weapon.hit);
                int atkAvo = CombatEngine.ComputeAvoid(atkAS, attacker.niyati, atkTerrainAvo);
                defDisplayedHit = CombatEngine.ComputeDisplayedHit(defHitRaw, atkAvo);
                defDmg = CombatEngine.ComputeDamage(defender.weapon.damageType,
                    defender.str, defender.mag, defender.weapon.might, 0,
                    attacker.def, attacker.res, atkTerrainDef);
                defCrit = CombatEngine.ComputeCritRate(defender.skl, defender.weapon.crit, 0, attacker.niyati);
            }

            // Step 1: Attacker's first hit
            var hit1 = ResolveHit("Attacker", atkDisplayedHit, atkDmg, atkCrit, rng);
            hits.Add(hit1);
            if (hit1.Hit) defHP = Mathf.Max(0, defHP - hit1.Damage);
            if (defHP <= 0)
            {
                if (hit1.TrueHitRoll < 30) unlikelyDeath = true;
                return BuildResult(hits, atkHP, defHP, unlikelyDeath);
            }

            // Step 2: Defender's counterattack
            if (canCounter)
            {
                var hit2 = ResolveHit("Defender", defDisplayedHit, defDmg, defCrit, rng);
                hits.Add(hit2);
                if (hit2.Hit) atkHP = Mathf.Max(0, atkHP - hit2.Damage);
                if (atkHP <= 0)
                {
                    if (hit2.TrueHitRoll < 30) unlikelyDeath = true;
                    return BuildResult(hits, atkHP, defHP, unlikelyDeath);
                }
            }

            // Step 3: Attacker's double attack
            if (atkDoubles)
            {
                var hit3 = ResolveHit("Attacker", atkDisplayedHit, atkDmg, atkCrit, rng);
                hits.Add(hit3);
                if (hit3.Hit) defHP = Mathf.Max(0, defHP - hit3.Damage);
                if (defHP <= 0)
                {
                    if (hit3.TrueHitRoll < 30) unlikelyDeath = true;
                    return BuildResult(hits, atkHP, defHP, unlikelyDeath);
                }
            }

            // Step 4: Defender's double counterattack
            if (defDoubles)
            {
                var hit4 = ResolveHit("Defender", defDisplayedHit, defDmg, defCrit, rng);
                hits.Add(hit4);
                if (hit4.Hit) atkHP = Mathf.Max(0, atkHP - hit4.Damage);
                if (atkHP <= 0)
                {
                    if (hit4.TrueHitRoll < 30) unlikelyDeath = true;
                }
            }

            return BuildResult(hits, atkHP, defHP, unlikelyDeath);
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

        private static CombatResult BuildResult(List<HitResult> hits, int atkHP, int defHP, bool unlikely)
        {
            return new CombatResult
            {
                Hits = hits,
                AttackerDied = atkHP <= 0,
                DefenderDied = defHP <= 0,
                UnlikelyDeath = unlikely,
                AttackerHPAfter = atkHP,
                DefenderHPAfter = defHP,
            };
        }
    }
}
