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
            return new UnitInfoFooterModel
            {
                Icon = r.Icon,
                Title = r.Label,
                Description = FirstSentence(r.Description),
                Detail = AfterFirstSentence(r.Description),
            };
        }

        // §5 makes the weapon card a focus stop, so it needs footer copy of its own.
        public UnitInfoFooterModel WeaponFooter()
        {
            var w = Model != null ? Model.Weapon : null;
            if (w == null || !w.HasWeapon)
                return new UnitInfoFooterModel { Title = "NO WEAPON", Description = "Nothing is equipped." };

            return new UnitInfoFooterModel
            {
                Icon = w.Sigil,
                Title = w.Name.ToUpper(),
                Description = $"Equipped {w.WeaponType.ToString().ToLower()}, range {w.RangeText}.",
                Detail = $"Might {w.Mt} · Hit {w.Hit} · Crit {w.Crt}.",
            };
        }

        // The stat copy is already written as a short claim followed by the longer explanation,
        // which is exactly the two lines §7 asks for - so it is split rather than rewritten.
        private static string FirstSentence(string description)
        {
            if (string.IsNullOrEmpty(description)) return "";
            int stop = description.IndexOf('.');
            return stop < 0 ? description : description.Substring(0, stop + 1);
        }

        private static string AfterFirstSentence(string description)
        {
            if (string.IsNullOrEmpty(description)) return "";
            int stop = description.IndexOf('.');
            return stop < 0 || stop + 1 >= description.Length ? "" : description.Substring(stop + 1).Trim();
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
