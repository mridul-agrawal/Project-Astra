using System.Collections.Generic;
using ProjectAstra.Core.Units;
using ProjectAstra.Core.Stats;

namespace ProjectAstra.Core.Editor
{
    public struct HubProblem
    {
        public readonly string Message;
        public readonly UnityEngine.Object Asset;
        public HubProblem(UnityEngine.Object asset, string message) { Asset = asset; Message = message; }
    }

    // One pass over every authored asset, surfacing the cross-reference and completeness issues the
    // raw inspector can't: unresolved ids, unregistered units, empty stats, dangling loadout slots.
    // Rendered in the Data Hub's Problems tab.
    public static class DataHubProblems
    {
        public static List<HubProblem> Collect()
        {
            var problems = new List<HubProblem>();
            var db = DataHubAssets.FindUnitDatabase();
            var idOwners = new Dictionary<string, UnitDefinition>();

            foreach (UnitDefinition u in DataHubAssets.LoadAll<UnitDefinition>())
            {
                if (string.IsNullOrWhiteSpace(u.UnitId))
                    problems.Add(new HubProblem(u, $"{u.name}: empty unitId"));
                else if (idOwners.TryGetValue(u.UnitId, out UnitDefinition other))
                    problems.Add(new HubProblem(u, $"{u.name}: duplicate unitId '{u.UnitId}' (shared with {other.name})"));
                else
                    idOwners[u.UnitId] = u;

                if (db != null && !DataHubAssets.IsRegistered(db, u))
                    problems.Add(new HubProblem(u, $"{u.name}: not registered in UnitDatabase"));
                if (u.DefaultClass == null)
                    problems.Add(new HubProblem(u, $"{u.name}: no defaultClass assigned"));
                if (IsAllZero(u.BaseStats))
                    problems.Add(new HubProblem(u, $"{u.name}: base stats are all zero"));
            }

            foreach (ClassDefinition c in DataHubAssets.LoadAll<ClassDefinition>())
                if (IsAllZero(c.StatCaps))
                    problems.Add(new HubProblem(c, $"{c.name}: stat caps are all zero"));

            foreach (WeaponDefinition w in DataHubAssets.LoadAll<WeaponDefinition>())
            {
                var rt = w.ToRuntime();
                if (rt.minRange > rt.maxRange)
                    problems.Add(new HubProblem(w, $"{w.name}: minRange ({rt.minRange}) > maxRange ({rt.maxRange})"));
                if (rt.characterLocked && !string.IsNullOrEmpty(rt.ownerUnitId) && !idOwners.ContainsKey(rt.ownerUnitId))
                    problems.Add(new HubProblem(w, $"{w.name}: character-locked to unknown unitId '{rt.ownerUnitId}'"));
            }

            foreach (InventoryLoadout l in DataHubAssets.LoadAll<InventoryLoadout>())
            {
                var items = l.Items;
                if (items == null) continue;
                for (int i = 0; i < items.Length; i++)
                    if (items[i] == null)
                        problems.Add(new HubProblem(l, $"{l.name}: item slot {i} is empty/missing"));
            }

            return problems;
        }

        private static bool IsAllZero(StatArray stats)
        {
            for (int i = 0; i < StatArray.Length; i++)
                if (stats[(StatIndex)i] != 0) return false;
            return true;
        }
    }
}
