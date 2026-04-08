using System;
using System.Collections.Generic;

namespace ProjectAstra.Core
{
    public class UnitRegistry
    {
        private readonly List<UnitTurnState> _units = new();

        public event Action<TestUnit> OnUnitActed;

        public void Register(TestUnit unit, Faction faction)
        {
            if (Find(unit) != null) return;
            _units.Add(new UnitTurnState(unit, faction));
        }

        public void Unregister(TestUnit unit)
        {
            _units.RemoveAll(u => u.Unit == unit);
        }

        public void Clear()
        {
            _units.Clear();
        }

        public List<TestUnit> GetUnitsForFaction(Faction faction)
        {
            var result = new List<TestUnit>();
            foreach (var entry in _units)
                if (entry.Faction == faction)
                    result.Add(entry.Unit);
            return result;
        }

        public List<TestUnit> GetActableUnits(Faction faction)
        {
            var result = new List<TestUnit>();
            foreach (var entry in _units)
                if (entry.Faction == faction && entry.CanAct)
                    result.Add(entry.Unit);
            return result;
        }

        public bool CanAct(TestUnit unit)
        {
            var entry = Find(unit);
            return entry != null && entry.CanAct;
        }

        public Faction? GetFaction(TestUnit unit)
        {
            return Find(unit)?.Faction;
        }

        public void MarkMoved(TestUnit unit)
        {
            var entry = Find(unit);
            if (entry != null) entry.HasMoved = true;
        }

        public void MarkActed(TestUnit unit)
        {
            var entry = Find(unit);
            if (entry == null || !entry.CanAct) return;

            entry.CanAct = false;
            unit.MarkActed();
            OnUnitActed?.Invoke(unit);
        }

        public void CancelMove(TestUnit unit)
        {
            var entry = Find(unit);
            if (entry != null) entry.HasMoved = false;
        }

        public void ResetPhaseFlags(Faction faction)
        {
            foreach (var entry in _units)
            {
                if (entry.Faction != faction) continue;
                entry.CanAct = true;
                entry.HasMoved = false;
                entry.Unit.ResetActed();
            }
        }

        public bool AllDone(Faction faction)
        {
            foreach (var entry in _units)
                if (entry.Faction == faction && entry.CanAct)
                    return false;
            return true;
        }

        public TestUnit GetNextUnactedUnit(Faction faction, TestUnit current)
        {
            return CycleUnactedUnit(faction, current, 1);
        }

        public TestUnit GetPrevUnactedUnit(Faction faction, TestUnit current)
        {
            return CycleUnactedUnit(faction, current, -1);
        }

        public int UnitCount => _units.Count;

        public bool HasUnitsOfFaction(Faction faction)
        {
            foreach (var entry in _units)
                if (entry.Faction == faction)
                    return true;
            return false;
        }

        private TestUnit CycleUnactedUnit(Faction faction, TestUnit current, int direction)
        {
            var actable = GetActableUnits(faction);
            if (actable.Count == 0) return null;

            int currentIndex = actable.IndexOf(current);
            int startIndex = currentIndex >= 0 ? currentIndex + direction : 0;
            startIndex = ((startIndex % actable.Count) + actable.Count) % actable.Count;
            return actable[startIndex];
        }

        private UnitTurnState Find(TestUnit unit)
        {
            foreach (var entry in _units)
                if (entry.Unit == unit)
                    return entry;
            return null;
        }

        private class UnitTurnState
        {
            public TestUnit Unit { get; }
            public Faction Faction { get; }
            public bool CanAct { get; set; }
            public bool HasMoved { get; set; }

            public UnitTurnState(TestUnit unit, Faction faction)
            {
                Unit = unit;
                Faction = faction;
                CanAct = true;
                HasMoved = false;
            }
        }
    }
}
