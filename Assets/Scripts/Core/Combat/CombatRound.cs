using System.Collections.Generic;
using UnityEngine;
using ProjectAstra.Core.Stats;
using ProjectAstra.Core.Units;

namespace ProjectAstra.Core.Combat
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

    // FE GBA combat-round resolver. Plays the standard exchange:
    //   1a. Attacker's first hit
    //   1b. Attacker's brave second hit (if equipped with a brave weapon)
    //   2.  Defender's counter (if in range)
    //   3.  Attacker's double (if AS - defAS >= 4)
    //   4.  Defender's double counter
    // Combat stops as soon as either combatant hits 0 HP. UnlikelyDeath flags
    // the survivor for "death by sub-30% true hit" — used later for the
    // wartime epitaph wording.
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
            bool defenderFired = false;

            int atkAS = StatUtils.AttackSpeed(attacker.spd, attacker.weapon.weight, attacker.con);
            int defAS = StatUtils.AttackSpeed(defender.spd, defender.weapon.weight, defender.con);

            bool attackerCanFire = !attacker.weapon.IsEmpty && !attacker.weapon.IsBroken;
            bool canCounter = !defender.weapon.IsEmpty && !defender.weapon.IsBroken
                              && defender.weapon.CanReachRange(attacker.distance);
            bool atkDoubles = CombatEngine.CanDoubleAttack(atkAS, defAS);
            bool defDoubles = canCounter && CombatEngine.CanDoubleAttack(defAS, atkAS);

            int atkAdvantage = WeaponTriangle.ComputeAdvantage(attacker.weapon, defender.weapon);
            int defAdvantage = -atkAdvantage;

            bool atkEffective = attacker.weapon.IsEffectiveAgainst(defenderClassType);
            bool defEffective = canCounter && defender.weapon.IsEffectiveAgainst(attackerClassType);

            var atkProfile = BuildAttackProfile(attacker, defender, atkAdvantage,
                defTerrainAvo, defTerrainDef, atkEffective, defenderClassType, defAS);
            var defProfile = canCounter
                ? BuildAttackProfile(defender, attacker, defAdvantage,
                    atkTerrainAvo, atkTerrainDef, defEffective, attackerClassType, atkAS)
                : default;

            CombatResult Conclude() => Build(hits, atkHP, defHP, unlikelyDeath,
                atkAdvantage, atkEffective, attackerCanFire, defenderFired);

            // 1a. Attacker's first hit.
            if (attackerCanFire
                && !TryFireHit("Attacker", atkProfile, rng, hits, ref defHP, ref unlikelyDeath))
                return Conclude();

            // 1b. Brave second hit, before the counter.
            if (attackerCanFire && attacker.weapon.brave
                && !TryFireHit("Attacker", atkProfile, rng, hits, ref defHP, ref unlikelyDeath))
                return Conclude();

            // 2. Defender's counterattack.
            if (canCounter)
            {
                defenderFired = true;
                if (!TryFireHit("Defender", defProfile, rng, hits, ref atkHP, ref unlikelyDeath))
                    return Conclude();
            }

            // 3. Attacker's double.
            if (attackerCanFire && atkDoubles
                && !TryFireHit("Attacker", atkProfile, rng, hits, ref defHP, ref unlikelyDeath))
                return Conclude();

            // 4. Defender's double counter (last step; no early-return needed).
            if (defDoubles)
                TryFireHit("Defender", defProfile, rng, hits, ref atkHP, ref unlikelyDeath);

            return Conclude();
        }

        private struct AttackProfile
        {
            public int DisplayedHit;
            public int Damage;
            public int CritRate;
        }

        private static AttackProfile BuildAttackProfile(
            CombatantData self, CombatantData opponent, int triangleAdvantage,
            int opponentTerrainAvo, int opponentTerrainDef,
            bool effective, ClassType opponentClass, int opponentAS)
        {
            int might = effective
                ? CombatEngine.ComputeEffectiveMight(self.weapon.might, self.weapon, opponentClass)
                : self.weapon.might;

            int rawHit = CombatEngine.ComputeAttackerHit(self.skl, self.niyati,
                self.weapon.hit, WeaponTriangle.GetHitBonus(triangleAdvantage));
            int avo = CombatEngine.ComputeAvoid(opponentAS, opponent.niyati,
                opponentTerrainAvo + WeaponTriangle.GetAvoidBonus(-triangleAdvantage));

            return new AttackProfile
            {
                DisplayedHit = CombatEngine.ComputeDisplayedHit(rawHit, avo),
                Damage = CombatEngine.ComputeDamage(self.weapon.damageType,
                    self.str, self.mag, might,
                    WeaponTriangle.GetDamageBonus(triangleAdvantage),
                    opponent.def, opponent.res, opponentTerrainDef),
                CritRate = CombatEngine.ComputeCritRate(self.skl, self.weapon.crit,
                    self.classCrit, opponent.niyati),
            };
        }

        // Fires one hit and applies the result. Returns true if combat should
        // continue, false if the target hit 0 HP (caller should stop).
        private static bool TryFireHit(
            string who, AttackProfile profile, IRng rng,
            List<HitResult> hits, ref int targetHP, ref bool unlikelyDeath)
        {
            var hit = ResolveHit(who, profile.DisplayedHit, profile.Damage, profile.CritRate, rng);
            hits.Add(hit);

            if (hit.Hit) targetHP = Mathf.Max(0, targetHP - hit.Damage);

            if (targetHP <= 0)
            {
                // Sub-30% true-hit rolls mark the kill as "unlikely" so the
                // ledger epitaph can refer to it as a fluke.
                if (hit.TrueHitRoll < 30) unlikelyDeath = true;
                return false;
            }
            return true;
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
                TrueHitRoll = trueHitRoll,
            };
        }

        private static CombatResult Build(List<HitResult> hits, int atkHP, int defHP, bool unlikely,
            int triAdv, bool effective, bool attackerFired, bool defenderFired)
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
