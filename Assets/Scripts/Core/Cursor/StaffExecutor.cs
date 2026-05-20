using System;
using System.Collections.Generic;
using UnityEngine;
using ProjectAstra.Core.UI.Forecast;
using ProjectAstra.Core.UI.Overlays;
using ProjectAstra.Core.Units;

namespace ProjectAstra.Core.Cursor
{
    // Runs a single staff use — Heal on a target ally, or Fortify across
    // every unit in range. Mirrors CombatExecutor's shape: forecast hide,
    // delegate the actual heal arithmetic to UnitInventory.TryUse*, route
    // durability through ItemBreakToaster, fire onComplete when done.
    //
    // No grid-cursor or input dependency — composable from any system that
    // wants to fire a staff (today, GridCursor at HandleConfirm.Targeting
    // for Heal and OnActionSelected.Fortify for Fortify).
    public class StaffExecutor
    {
        private readonly CombatForecastUI _combatForecastUI;
        private readonly ToastNotificationUI _toastUI;

        public StaffExecutor(CombatForecastUI combatForecastUI, ToastNotificationUI toastUI)
        {
            _combatForecastUI = combatForecastUI;
            _toastUI = toastUI;
        }

        // Single-target heal. Early-outs (no target, broken staff) still fire
        // onComplete so the caller never has to branch.
        public void TryCommitHeal(TestUnit healer, TestUnit target, Action onComplete)
        {
            _combatForecastUI?.Hide();

            if (target == null) { onComplete?.Invoke(); return; }

            ItemBreakToaster.WithBreakAnnouncements(healer, _toastUI, () =>
            {
                if (healer.Inventory.TryUseStaff(target, out int healed, out string fail))
                    Debug.Log($"[Staff] {healer.name} healed {target.name} for {healed} HP.");
                else
                    Debug.LogWarning($"[Staff] Heal failed: {fail}");
            });

            onComplete?.Invoke();
        }

        // Area-of-effect heal. UnitInventory.TryUseFortify resolves which
        // units are reachable + still injured.
        public void TryCommitFortify(TestUnit healer, Action onComplete)
        {
            var allUnits = new List<TestUnit>(
                UnityEngine.Object.FindObjectsByType<TestUnit>(FindObjectsSortMode.None));

            ItemBreakToaster.WithBreakAnnouncements(healer, _toastUI, () =>
            {
                if (healer.Inventory.TryUseFortify(allUnits, out var healed, out string fail))
                {
                    foreach (var (unit, amount) in healed)
                        Debug.Log($"[Staff] {healer.name} healed {unit.name} for {amount} HP (Fortify).");
                }
                else
                {
                    Debug.LogWarning($"[Staff] Fortify failed: {fail}");
                }
            });

            onComplete?.Invoke();
        }
    }
}
