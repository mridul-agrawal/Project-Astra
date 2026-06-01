using System.Collections.Generic;
using ProjectAstra.Core.Units;

namespace ProjectAstra.Core.Combat.Playback
{
    // Walks CombatResult.Hits once with a running HP tally for each side and
    // produces a flat playback plan. Two responsibilities:
    //   1. Fuse the first two attacker-side hits into a BraveHitStep iff the
    //      attacker's equipped weapon is brave AND CombatRound.Resolve didn't
    //      truncate hit 2 (i.e. hits.Count >= 2 and both are attacker-side).
    //      Everything after the brave pair plays as normal single steps.
    //   2. Tag each crit hit with its CritContext (Righteous vs Tragic), and
    //      mark the fatal hit on each side (the one that drops HP to 0). The
    //      controller uses these to choose flash color, fall speed, and the
    //      pre-death hold extension.
    public static class BraveFlowComposer
    {
        public static CombatPlan Compose(CombatResult result, TestUnit attacker, TestUnit defender)
        {
            var plan = new CombatPlan { Steps = new List<PlaybackStep>() };
            if (result.Hits == null || result.Hits.Count == 0) return plan;

            int attackerHp = HpOf(attacker);
            int defenderHp = HpOf(defender);

            int i = 0;
            if (CanFuseBrave(result.Hits, attacker))
            {
                var h1 = result.Hits[0];
                var h2 = result.Hits[1];
                // Both hits target the defender (attacker-side).
                int hp1 = ApplyHpAndCheckFatal(ref defenderHp, h1, out bool h1Fatal);
                int hp2 = ApplyHpAndCheckFatal(ref defenderHp, h2, out bool h2Fatal);
                plan.Steps.Add(new BraveHitStep
                {
                    AttackerIsStriker = true,
                    Hit1 = h1,
                    Hit2 = h2,
                    Hit1Fatal = h1Fatal,
                    Hit2Fatal = h2Fatal,
                    Hit1CritContext = CritContextClassifier.Classify(defender, h1Fatal),
                    Hit2CritContext = CritContextClassifier.Classify(defender, h2Fatal),
                });
                i = 2;
            }

            for (; i < result.Hits.Count; i++)
            {
                var h = result.Hits[i];
                bool attackerSide = h.Attacker == "Attacker";
                var receiver = attackerSide ? defender : attacker;
                int curHp = attackerSide ? defenderHp : attackerHp;
                int newHp = ApplyHpAndCheckFatal(ref curHp, h, out bool fatal);
                if (attackerSide) defenderHp = curHp; else attackerHp = curHp;
                plan.Steps.Add(new SingleHitStep
                {
                    AttackerIsStriker = attackerSide,
                    Hit = h,
                    FatalHit = fatal,
                    CritContext = CritContextClassifier.Classify(receiver, fatal),
                });
                if (fatal) break;  // CombatRound truncates after a death, but be defensive.
            }

            return plan;
        }

        private static bool CanFuseBrave(List<HitResult> hits, TestUnit attacker)
        {
            if (hits.Count < 2) return false;
            if (hits[0].Attacker != "Attacker" || hits[1].Attacker != "Attacker") return false;
            if (attacker?.Inventory == null) return false;
            var weapon = attacker.Inventory.GetEquippedWeapon();
            return !weapon.IsEmpty && weapon.brave;
        }

        // Applies the hit's damage to the running HP and reports whether it was
        // the fatal blow. Returns the new HP.
        private static int ApplyHpAndCheckFatal(ref int hp, HitResult hit, out bool fatal)
        {
            int damage = hit.Hit ? hit.Damage : 0;
            hp = UnityEngine.Mathf.Max(0, hp - damage);
            fatal = hit.Hit && hp <= 0;
            return hp;
        }

        private static int HpOf(TestUnit u) =>
            u?.UnitInstance != null ? u.UnitInstance.CurrentHP : (u != null ? u.currentHP : 0);
    }
}
