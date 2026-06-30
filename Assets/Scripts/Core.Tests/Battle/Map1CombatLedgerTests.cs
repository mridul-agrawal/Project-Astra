using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using ProjectAstra.Core.Combat;
using ProjectAstra.Core.Grid;
using ProjectAstra.Core.Stats;
using ProjectAstra.Core.Units;

namespace ProjectAstra.Core.Tests.Battle
{
    // The enforcement artifact for Map 1's locked no-RNG principle. Loads the REAL
    // unit/weapon/map assets and replays the three scripted exchanges, asserting every
    // intermediate HP value plus the roll-forceability constraints the scripted RNG
    // depends on. Any data or formula change that would break a beat fails here with
    // the exact number that broke.
    //
    // The locked ledger:
    //   T2  Aranya (cork 7,4) vs raider (bridge 7,5): her hit lands for 7 (12 -> 5),
    //       his counter fires and MISSES (the dodge lesson). No doubling either way.
    //   T3  Same matchup: her hit kills (5 -> 0).
    //   T4  Aranya (7,8) vs boss (7,9): hit 7 (22 -> 15), counter hits for 18
    //       (20 -> exactly 2), her double CRITS for 21 (15 -> dead).
    [TestFixture]
    public class Map1CombatLedgerTests
    {
        private const string AranyaPath = "Assets/ScriptableObjects/Units/Characters/Aranya.asset";
        private const string RaiderPath = "Assets/ScriptableObjects/Units/Enemies/RakshasaRaider.asset";
        private const string BossPath = "Assets/ScriptableObjects/Units/Enemies/RakshasaMiniboss.asset";
        private const string BowPath = "Assets/ScriptableObjects/Items/Weapons/HuntingBow.asset";
        private const string HatchetPath = "Assets/ScriptableObjects/Items/Weapons/RaidersHatchet.asset";
        private const string AxePath = "Assets/ScriptableObjects/Items/Weapons/ChampionsAxe.asset";
        private const string MapPath = "Assets/ScriptableObjects/Map/Maps/Map1_BridgeAtSuvarnapur.asset";
        private const string TerrainTablePath = "Assets/ScriptableObjects/Map/TerrainStatTable.asset";

        // The locked beat geometry (15-wide map; north = high y).
        private static readonly Vector2Int CorkTile = new(7, 4);
        private static readonly Vector2Int BridgeTile = new(7, 5);
        private static readonly Vector2Int BossAttackTile = new(7, 8);
        private static readonly Vector2Int BossTile = new(7, 9);

        private UnitDefinition aranyaDef;
        private UnitDefinition raiderDef;
        private UnitDefinition bossDef;
        private WeaponData bow;
        private WeaponData hatchet;
        private WeaponData axe;
        private MapData map;
        private TerrainStatTable terrainTable;

        [OneTimeSetUp]
        public void LoadAssets()
        {
            aranyaDef = Load<UnitDefinition>(AranyaPath);
            raiderDef = Load<UnitDefinition>(RaiderPath);
            bossDef = Load<UnitDefinition>(BossPath);
            bow = Load<WeaponDefinition>(BowPath).ToRuntime();
            hatchet = Load<WeaponDefinition>(HatchetPath).ToRuntime();
            axe = Load<WeaponDefinition>(AxePath).ToRuntime();
            map = Load<MapData>(MapPath);
            terrainTable = Load<TerrainStatTable>(TerrainTablePath);
        }

        // --- The three scripted exchanges, with real terrain ---

        [Test]
        public void Turn2_HerHitLands_RaiderAt5_CounterFiresAndMisses()
        {
            var result = ResolveBridgeDuel(raiderHP: RaiderMaxHP(), SwingOutcome.Hit, SwingOutcome.Miss);

            Assert.AreEqual(2, result.Hits.Count, "T2 must be exactly attack + counter (no doubling)");
            Assert.IsTrue(result.Hits[0].Hit && !result.Hits[0].Crit, "her opener lands plainly");
            Assert.AreEqual(7, result.Hits[0].Damage, "her damage vs the raider");
            Assert.IsTrue(result.DefenderFired, "the counter must fire — it is the dodge lesson");
            Assert.IsFalse(result.Hits[1].Hit, "the counter must miss");
            Assert.AreEqual(5, result.DefenderHPAfter, "raider must survive T2 at exactly 5");
            Assert.AreEqual(AranyaMaxHP(), result.AttackerHPAfter, "Aranya untouched");
        }

        [Test]
        public void Turn3_HerHitKillsTheRaider()
        {
            var result = ResolveBridgeDuel(raiderHP: 5, SwingOutcome.Hit, SwingOutcome.Miss);

            Assert.IsTrue(result.DefenderDied, "T3 must kill the raider");
            Assert.AreEqual(AranyaMaxHP(), result.AttackerHPAfter, "Aranya untouched");
        }

        [Test]
        public void Turn4_CounterLeavesExactly2HP_DoubleCritKillsBoss()
        {
            var rng = Scripted(SwingOutcome.Hit, SwingOutcome.Hit, SwingOutcome.Crit);
            var result = ResolveBossFight(rng);

            Assert.AreEqual(3, result.Hits.Count, "T4 must be attack + counter + double");
            Assert.IsTrue(result.Hits[0].Hit && !result.Hits[0].Crit);
            Assert.AreEqual(7, result.Hits[0].Damage, "her damage vs the boss");
            Assert.IsTrue(result.Hits[1].Hit, "the boss counter must land");
            Assert.AreEqual(18, result.Hits[1].Damage, "the counter that defines the 2-HP moment");
            Assert.AreEqual(2, result.AttackerHPAfter, "Aranya at EXACTLY 2 HP");
            Assert.IsTrue(result.Hits[2].Crit, "the signature crit");
            Assert.AreEqual(21, result.Hits[2].Damage, "crit = 3x base");
            Assert.IsTrue(result.DefenderDied, "the crit must kill");
        }

        // --- Forceability constraints (what makes the scripted rolls possible) ---

        [Test]
        public void EnemyHitRates_AreForceableBothWays()
        {
            int raiderHit = DisplayedHit(Raider(RaiderMaxHP(), 1), Aranya(BridgeTile, CorkTile), CorkTile);
            int bossHit = DisplayedHit(Boss(1), Aranya(BossTile, BossAttackTile), BossAttackTile);

            Assert.That(raiderHit, Is.InRange(1, 99), "raider counter must be missable AND landable");
            Assert.That(bossHit, Is.InRange(1, 99), "boss counter must be missable AND landable");
        }

        [Test]
        public void AranyaRolls_AreForceable()
        {
            var vsRaider = Aranya(CorkTile, BridgeTile);
            var vsBoss = Aranya(BossAttackTile, BossTile);

            Assert.GreaterOrEqual(DisplayedHit(vsRaider, Raider(RaiderMaxHP(), 1), BridgeTile), 1, "her hits must be landable");
            Assert.GreaterOrEqual(DisplayedHit(vsBoss, Boss(1), BossTile), 1);
            Assert.GreaterOrEqual(CritRate(vsBoss, Boss(1)), 1, "the T4 crit must be forceable");
            Assert.LessOrEqual(CritRate(vsRaider, Raider(RaiderMaxHP(), 1)), 99, "crits vs the raider must be deniable");
        }

        [Test]
        public void DoublingMargins_MatchTheBeatStructure()
        {
            int aranyaAS = AttackSpeed(Aranya(CorkTile, BridgeTile));
            int raiderAS = AttackSpeed(Raider(RaiderMaxHP(), 1));
            int bossAS = AttackSpeed(Boss(1));

            Assert.IsFalse(CombatEngine.CanDoubleAttack(aranyaAS, raiderAS), "she must NOT double the raider (T2/T3 are single hits)");
            Assert.IsFalse(CombatEngine.CanDoubleAttack(raiderAS, aranyaAS), "the raider must never double her");
            Assert.IsTrue(CombatEngine.CanDoubleAttack(aranyaAS, bossAS), "she MUST double the boss (the crit is the double)");
            Assert.IsFalse(CombatEngine.CanDoubleAttack(bossAS, aranyaAS), "the boss must never double her");
        }

        // --- EXP bound: no level-up can shift her stats before the boss fight ---

        [Test]
        public void PreBossExp_CannotReachALevelUp()
        {
            var aranya = new UnitInstance(aranyaDef);
            var raider = new UnitInstance(raiderDef);

            int chipExp = ExpMath.ComputeCombatExp(aranya, raider, killed: false);   // T2 damage
            int killExp = ExpMath.ComputeCombatExp(aranya, raider, killed: true);    // T3 kill

            Assert.Less(chipExp + killExp, 100,
                "Aranya must enter the boss fight at base stats — a level-up would break the 2-HP equation");
        }

        // --- Combatant + resolution helpers (mirror CombatExecutor's wiring) ---

        private CombatResult ResolveBridgeDuel(int raiderHP, params SwingOutcome[] outcomes)
        {
            var aranya = Aranya(CorkTile, BridgeTile);
            var raider = Raider(raiderHP, 1);
            var (defTerrDef, defTerrAvo) = TerrainAt(BridgeTile, raiderDef);
            var (atkTerrDef, atkTerrAvo) = TerrainAt(CorkTile, aranyaDef);

            return CombatRound.Resolve(aranya, raider, defTerrDef, defTerrAvo, atkTerrDef, atkTerrAvo,
                Scripted(outcomes), ClassOf(aranyaDef), ClassOf(raiderDef));
        }

        private CombatResult ResolveBossFight(ScriptedCombatRng rng)
        {
            var aranya = Aranya(BossAttackTile, BossTile);
            var boss = Boss(1);
            var (defTerrDef, defTerrAvo) = TerrainAt(BossTile, bossDef);
            var (atkTerrDef, atkTerrAvo) = TerrainAt(BossAttackTile, aranyaDef);

            return CombatRound.Resolve(aranya, boss, defTerrDef, defTerrAvo, atkTerrDef, atkTerrAvo,
                rng, ClassOf(aranyaDef), ClassOf(bossDef));
        }

        private CombatantData Aranya(Vector2Int from, Vector2Int target) =>
            Combatant(aranyaDef, bow, AranyaMaxHP(), Distance(from, target));

        private CombatantData Raider(int hp, int distance) => Combatant(raiderDef, hatchet, hp, distance);

        private CombatantData Boss(int distance) => Combatant(bossDef, axe, BossMaxHP(), distance);

        private static CombatantData Combatant(UnitDefinition def, WeaponData weapon, int hp, int distance) =>
            CombatantData.FromStats(def.BaseStats, hp, def.BaseStats[StatIndex.HP], weapon,
                distance, def.DefaultClass.CritBonus);

        private int AranyaMaxHP() => aranyaDef.BaseStats[StatIndex.HP];
        private int RaiderMaxHP() => raiderDef.BaseStats[StatIndex.HP];
        private int BossMaxHP() => bossDef.BaseStats[StatIndex.HP];

        private static ScriptedCombatRng Scripted(params SwingOutcome[] outcomes)
        {
            var rng = new ScriptedCombatRng();
            rng.QueueOutcomes(outcomes);
            return rng;
        }

        private (int def, int avo) TerrainAt(Vector2Int tile, UnitDefinition unit)
        {
            int tileId = map.GetTileId(MapLayer.Ground, tile.x, tile.y);
            var terrain = map.Tilesets[0].GetTerrainType(tileId);
            return TerrainStatTable.GetTerrainBonuses(terrainTable.GetStats(terrain), unit.DefaultClass.MovementType);
        }

        // Mirrors CombatRound.BuildAttackProfile's hit math for constraint assertions.
        private int DisplayedHit(CombatantData striker, CombatantData target, Vector2Int targetTile)
        {
            int advantage = WeaponTriangle.ComputeAdvantage(striker.weapon, target.weapon);
            int rawHit = CombatEngine.ComputeAttackerHit(striker.skl, striker.niyati,
                striker.weapon.hit, WeaponTriangle.GetHitBonus(advantage));

            var targetDef = targetTile == BridgeTile ? raiderDef : targetTile == BossTile ? bossDef : aranyaDef;
            var (_, terrainAvo) = TerrainAt(targetTile, targetDef);
            int avoid = CombatEngine.ComputeAvoid(AttackSpeed(target), target.niyati,
                terrainAvo + WeaponTriangle.GetAvoidBonus(-advantage));

            return CombatEngine.ComputeDisplayedHit(rawHit, avoid);
        }

        private static int CritRate(CombatantData striker, CombatantData target) =>
            CombatEngine.ComputeCritRate(striker.skl, striker.weapon.crit, striker.classCrit, target.niyati);

        private static int AttackSpeed(CombatantData unit) =>
            StatUtils.AttackSpeed(unit.spd, unit.weapon.weight, unit.con);

        private static int Distance(Vector2Int a, Vector2Int b) =>
            Mathf.Abs(a.x - b.x) + Mathf.Abs(a.y - b.y);

        private static ClassType ClassOf(UnitDefinition def) => def.DefaultClass.ClassType;

        private static T Load<T>(string path) where T : Object
        {
            var asset = AssetDatabase.LoadAssetAtPath<T>(path);
            Assert.IsNotNull(asset, $"Missing required asset: {path}");
            return asset;
        }
    }
}
