using ProjectAstra.Core.UI.Overlays;
using ProjectAstra.Core.Units;
using ProjectAstra.Core.Cursor;

namespace ProjectAstra.Core.Combat.Playback
{
    // Per-hit + terminal application of combat consequences. Pulled out of
    // CombatExecutor so playback controllers can write HP at the strike
    // keyframe of each hit (FE-GBA convention — bars drain live).
    //
    // Contract:
    //   ApplyHitDamage  — call per hit at the strike keyframe (idempotent
    //                     wrt the final result: per-hit writes converge to
    //                     result.AttackerHPAfter / DefenderHPAfter).
    //   Finalize        — call once at the end (normal completion OR abort).
    //                     Snaps HP to the deterministic post-combat values
    //                     (safety net) and applies durability + EXP. Safe
    //                     to call after per-hit writes; HP snap is a no-op
    //                     when per-hit already converged.
    public static class CombatResultApplicator
    {
        // Per-hit HP write. Routes through UnitInstance.SetCurrentHP when the
        // unit has one (canonical path), falls back to the TestUnit field for
        // legacy test units without a UnitInstance.
        public static void ApplyHitDamage(TestUnit unit, int newHP)
        {
            if (unit == null) return;
            if (unit.UnitInstance != null)
                unit.UnitInstance.SetCurrentHP(newHP);
            else
                unit.currentHP = newHP;
        }

        // Terminal — call once when playback ends. Snaps HP to the result's
        // deterministic post-combat values, then consumes durability and
        // grants EXP. Safe to call regardless of whether per-hit writes
        // already converged.
        public static void Finalize(CombatPlaybackContext ctx)
        {
            if (ctx == null) return;
            ApplyHitDamage(ctx.Attacker, ctx.Result.AttackerHPAfter);
            ApplyHitDamage(ctx.Defender, ctx.Result.DefenderHPAfter);

            // Lord death clears the convoy reference (matches the original
            // CombatExecutor.HandleUnitDeathAfterCombat side-effect).
            if (ctx.Result.AttackerDied && IsLord(ctx.Attacker)) Convoy.Current = NullConvoy.Instance;
            if (ctx.Result.DefenderDied && IsLord(ctx.Defender)) Convoy.Current = NullConvoy.Instance;

            ApplyDurability(ctx.Attacker, ctx.Defender, ctx.Result, ctx.ToastUI);
            GrantCombatExp(ctx.Attacker, ctx.Defender, ctx.Result);
        }

        private static bool IsLord(TestUnit unit)
        {
            if (unit == null) return false;
            return unit.isLord || (unit.UnitInstance?.Definition?.IsLord ?? false);
        }

        private static void ApplyDurability(TestUnit attacker, TestUnit defender,
            CombatResult result, ToastNotificationUI toast)
        {
            if (result.AttackerFired && attacker != null && attacker.Inventory != null)
                ItemBreakToaster.WithBreakAnnouncements(attacker, toast,
                    () => attacker.Inventory.ConsumeEquippedWeaponUses(1));
            if (result.DefenderFired && defender != null && defender.Inventory != null)
                ItemBreakToaster.WithBreakAnnouncements(defender, toast,
                    () => defender.Inventory.ConsumeEquippedWeaponUses(1));
        }

        private static void GrantCombatExp(TestUnit attacker, TestUnit defender, CombatResult result)
        {
            if (ExpGranter.Instance == null) return;
            if (attacker == null || defender == null) return;
            if (attacker.UnitInstance == null || defender.UnitInstance == null) return;

            if (result.AttackerFired && !result.AttackerDied)
            {
                int exp = ExpMath.ComputeCombatExp(
                    attacker.UnitInstance, defender.UnitInstance, result.DefenderDied);
                ExpGranter.Instance.Grant(attacker, exp);
            }

            if (result.DefenderFired && !result.DefenderDied)
            {
                int exp = ExpMath.ComputeCombatExp(
                    defender.UnitInstance, attacker.UnitInstance, result.AttackerDied);
                ExpGranter.Instance.Grant(defender, exp);
            }
        }
    }
}
