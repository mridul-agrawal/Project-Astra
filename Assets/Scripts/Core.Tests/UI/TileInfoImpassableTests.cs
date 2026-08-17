using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;
using ProjectAstra.Core.Grid;
using ProjectAstra.Core.UI.BattleMap.HUD;

namespace ProjectAstra.Core.Tests.UI
{
    // Guards the tile info panel's "Impassable" flag against the real authored terrain table.
    //
    // This exists because the flag originally asked whether all six movement costs were zero, which
    // no water or prop terrain satisfies - a flier crosses a river, so its flying cost is 1 - and so
    // the flag never appeared on the tiles that most needed it. The panel was signed off from a
    // screenshot of a hand-built model, which proved nothing about the derivation.
    [TestFixture]
    public class TileInfoImpassableTests
    {
        const string TablePath = "Assets/ScriptableObjects/Map/TerrainStatTable.asset";

        private TerrainStatTable table;
        private MapData map;

        [SetUp]
        public void SetUp()
        {
            table = UnityEditor.AssetDatabase.LoadAssetAtPath<TerrainStatTable>(TablePath);
            Assert.IsNotNull(table, "The authored terrain table is missing from " + TablePath);

            map = ScriptableObject.CreateInstance<MapData>();
            var so = new UnityEditor.SerializedObject(map);
            so.FindProperty("width").intValue = 1;
            so.FindProperty("height").intValue = 1;
            so.FindProperty("terrain").arraySize = 1;
            so.ApplyModifiedPropertiesWithoutUndo();
        }

        [TearDown]
        public void TearDown()
        {
            if (map != null) Object.DestroyImmediate(map);
        }

        // Every terrain a foot soldier cannot enter has to say so, whatever a flier could do.
        [Test]
        public void FootBlockedTerrain_ShowsImpassable(
            [Values(TerrainType.River, TerrainType.Water, TerrainType.Sea, TerrainType.Peak,
                    TerrainType.Rock, TerrainType.Log, TerrainType.Campfire, TerrainType.Obstacle,
                    TerrainType.Wall, TerrainType.DestructibleWall, TerrainType.Door, TerrainType.Void)]
            TerrainType terrain)
        {
            Assert.AreEqual(0, table.GetStats(terrain).moveCostFoot,
                $"{terrain} is expected to block foot movement in the authored table.");

            Assert.IsTrue(Flags(terrain).Contains("Impassable"),
                $"{terrain} blocks foot movement but the panel did not flag it Impassable.");
        }

        // Slow is not the same as blocked. Mountain costs a foot soldier 4, and must not be flagged.
        [Test]
        public void PassableTerrain_DoesNotShowImpassable(
            [Values(TerrainType.Plain, TerrainType.Forest, TerrainType.Mountain, TerrainType.Road,
                    TerrainType.Village, TerrainType.Fort, TerrainType.Gate, TerrainType.Throne,
                    TerrainType.Sand, TerrainType.Rubble)]
            TerrainType terrain)
        {
            Assert.AreNotEqual(0, table.GetStats(terrain).moveCostFoot,
                $"{terrain} is expected to be walkable in the authored table.");

            Assert.IsFalse(Flags(terrain).Contains("Impassable"),
                $"{terrain} is walkable but the panel flagged it Impassable.");
        }

        [Test]
        public void PlainTile_RendersNameOnlyWithNoStrip()
        {
            TileInfoModel model = Build(TerrainType.Plain);
            Assert.AreEqual("Plain", model.TerrainName);
            Assert.IsEmpty(model.Strips, "A plain tile should render the name plate alone.");
        }

        [Test]
        public void River_ProducesTheFlagOnlyStateFromTheSpecMatrix()
        {
            TileInfoModel model = Build(TerrainType.River);

            Assert.AreEqual("River", model.TerrainName);
            Assert.AreEqual(1, model.Strips.Count);
            Assert.AreEqual(1, model.Strips[0].Chips.Count);
            Assert.AreEqual("Impassable", model.Strips[0].Chips[0].Label);
            Assert.IsTrue(model.Strips[0].Chips[0].IsFlag, "A flag carries no value.");
            Assert.IsFalse(model.Strips[0].HasNegative, "A flag must not turn the chevron red.");
        }

        // ---- helpers -------------------------------------------------------------------------

        private List<string> Flags(TerrainType terrain)
        {
            var labels = new List<string>();
            foreach (TileEffectStrip strip in Build(terrain).Strips)
                foreach (TileEffectChip chip in strip.Chips)
                    if (chip.IsFlag) labels.Add(chip.Label);
            return labels;
        }

        // Runs the panel's own derivation, so the test fails if that logic drifts.
        private TileInfoModel Build(TerrainType terrain)
        {
            var so = new UnityEditor.SerializedObject(map);
            so.FindProperty("terrain").GetArrayElementAtIndex(0).intValue = (int)terrain;
            so.ApplyModifiedPropertiesWithoutUndo();

            MapService.Load(map, table);
            var controller = new TileInfoController(null);
            return controller.BuildModel(Vector2Int.zero, HudCorner.BottomLeft);
        }
    }
}
