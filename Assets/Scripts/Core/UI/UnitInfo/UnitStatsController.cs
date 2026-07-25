using UnityEngine;
using ProjectAstra.Core.Combat;
using ProjectAstra.Core.Stats;
using ProjectAstra.Core.Units;

namespace ProjectAstra.Core.UI.UnitInfo
{
    // Builds the STATS tab model (equipped weapon + nine stat rows) and renders it.
    // Keeps the model so the composition root can pull the selected row's footer text.
    public sealed class UnitStatsController
    {
        private const int MoveDisplayCap = 10;

        private readonly UnitStatsView view;
        private readonly StatInfoTable statInfo;

        public UnitStatsModel Model { get; private set; }
        public int RowCount => Model != null && Model.Rows != null ? Model.Rows.Length : 0;

        public UnitStatsController(UnitStatsView view, StatInfoTable statInfo)
        {
            this.view = view;
            this.statInfo = statInfo;
        }

        public void Render(TestUnit unit)
        {
            Model = BuildModel(unit);
            view?.Render(Model);
        }

        public UnitInfoFooterModel FooterFor(int index)
        {
            if (Model == null || Model.Rows == null || index < 0 || index >= Model.Rows.Length) return null;
            var r = Model.Rows[index];
            return new UnitInfoFooterModel { Icon = r.Icon, Title = r.Label, Description = r.Description };
        }

        private UnitStatsModel BuildModel(TestUnit unit)
        {
            var inst = unit != null ? unit.UnitInstance : null;
            var rows = new StatRowVM[9];
            rows[0] = StatRow(StatKey.Strength, StatIndex.Str, StatGroup.Attack, inst);
            rows[1] = StatRow(StatKey.Magic,    StatIndex.Mag, StatGroup.Attack, inst);
            rows[2] = StatRow(StatKey.Skill,    StatIndex.Skl, StatGroup.Attack, inst);
            rows[3] = StatRow(StatKey.Speed,    StatIndex.Spd, StatGroup.Attack, inst);
            rows[4] = StatRow(StatKey.Defense,  StatIndex.Def, StatGroup.Guard,  inst);
            rows[5] = StatRow(StatKey.Resist,   StatIndex.Res, StatGroup.Guard,  inst);
            rows[6] = StatRow(StatKey.Con,      StatIndex.Con, StatGroup.Body,   inst);
            rows[7] = StatRow(StatKey.Luck,     StatIndex.Niyati, StatGroup.Body, inst);
            rows[8] = MoveRow(inst);
            return new UnitStatsModel { Weapon = BuildWeapon(unit), Rows = rows };
        }

        private StatRowVM StatRow(StatKey key, StatIndex idx, StatGroup group, UnitInstance inst)
        {
            var info = Info(key);
            int value = inst != null ? inst.Stats[idx] : 0;
            int cap = inst != null && inst.CurrentClass != null ? inst.CurrentClass.StatCaps[idx] : 0;
            return new StatRowVM
            {
                Label = info.label, Value = value, Cap = cap > 0 ? cap : Mathf.Max(value, 1),
                Icon = info.icon, Group = group, Description = info.description ?? "",
            };
        }

        private StatRowVM MoveRow(UnitInstance inst)
        {
            var info = Info(StatKey.Move);
            int value = inst != null ? inst.EffectiveMovement : 0;
            return new StatRowVM
            {
                Label = info.label, Value = value, Cap = MoveDisplayCap,
                Icon = info.icon, Group = StatGroup.Body, Description = info.description ?? "",
            };
        }

        private StatInfoTable.Entry Info(StatKey key)
        {
            var e = statInfo != null ? statInfo.Get(key) : new StatInfoTable.Entry { key = key };
            if (string.IsNullOrEmpty(e.label)) e.label = key.ToString().ToUpper();
            return e;
        }

        private static EquippedWeaponVM BuildWeapon(TestUnit unit)
        {
            var w = unit != null ? unit.equippedWeapon : WeaponData.None;
            if (w.IsEmpty) return new EquippedWeaponVM { HasWeapon = false };
            return new EquippedWeaponVM
            {
                HasWeapon = true, Name = w.name, WeaponType = w.weaponType,
                Mt = w.might, Hit = w.hit, Crt = w.crit, RngMin = w.minRange, RngMax = w.maxRange,
            };
        }
    }
}
