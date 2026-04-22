using UnityEngine;
using ProjectAstra.Core.UI;

namespace ProjectAstra.Core.Progression.Evaluators
{
    /// <summary>
    /// On Start, walks the scene's TestUnits. If any enemy unit's UnitDefinition
    /// has isNamedCommander=true, raises the WarLedgerServices flag so the
    /// Ledger-trigger predicate fires even when the named commander survives.
    ///
    /// Drop on the BattleMap chapter root alongside CommitmentSet.
    /// </summary>
    public class NamedCommanderScanner : MonoBehaviour
    {
        private void Start()
        {
            var units = FindObjectsByType<TestUnit>(FindObjectsSortMode.None);
            foreach (var u in units)
            {
                if (u.faction != Faction.Enemy) continue;
                // Read the serialized UnitDefinition directly — Start execution order
                // isn't guaranteed relative to TestUnit.Start which binds UnitInstance.
                var def = u.UnitDefinition ?? u.UnitInstance?.Definition;
                if (def != null && def.IsNamedCommander)
                {
                    WarLedgerServices.EnemyForceHadNamedCommanderThisChapter = true;
                    return;
                }
            }
        }
    }
}
