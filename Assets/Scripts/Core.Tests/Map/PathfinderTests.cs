using System;
using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;
using ProjectAstra.Core;

namespace ProjectAstra.Core.Tests
{
    [TestFixture]
    public class PathfinderTests
    {
        // --- Test helpers ---

        private static Func<int, int, TerrainType> UniformGrid(TerrainType type, int w, int h)
            => (x, y) => (x >= 0 && x < w && y >= 0 && y < h) ? type : TerrainType.Void;

        private static Func<TerrainType, TerrainStats> DefaultStats()
            => t => t == TerrainType.Void
                ? new TerrainStats()
                : TerrainStats.Default;

        private static Func<TerrainType, TerrainStats> CustomStats(
            int plainCost = 1, int forestCost = 2, int wallCost = 0)
        {
            return t =>
            {
                if (t == TerrainType.Plain || t == TerrainType.Road)
                    return StatsWithFootCost(plainCost);
                if (t == TerrainType.Forest)
                    return StatsWithFootCost(forestCost);
                if (t == TerrainType.Wall || t == TerrainType.Void)
                    return StatsWithFootCost(wallCost);
                return TerrainStats.Default;
            };
        }

        private static TerrainStats StatsWithFootCost(int cost)
        {
            var s = TerrainStats.Default;
            s.moveCostFoot = cost;
            s.moveCostMounted = cost;
            s.moveCostArmoured = cost;
            s.moveCostFlying = cost == 0 ? 0 : 1; // flying always 1 unless truly impassable
            s.moveCostPirate = cost;
            s.moveCostThief = cost;
            return s;
        }

        private static Func<Vector2Int, Pathfinder.OccupantType> NoOccupants()
            => _ => Pathfinder.OccupantType.None;

        private static Func<Vector2Int, Pathfinder.OccupantType> OccupantsAt(
            Dictionary<Vector2Int, Pathfinder.OccupantType> map)
            => pos => map.TryGetValue(pos, out var t) ? t : Pathfinder.OccupantType.None;

        // Grid with specific terrain at specific positions
        private static Func<int, int, TerrainType> MixedGrid(
            int w, int h, TerrainType defaultType,
            Dictionary<Vector2Int, TerrainType> overrides)
        {
            return (x, y) =>
            {
                if (x < 0 || x >= w || y < 0 || y >= h) return TerrainType.Void;
                var pos = new Vector2Int(x, y);
                return overrides.TryGetValue(pos, out var t) ? t : defaultType;
            };
        }

        // --- GetMovementCost tests ---

        [Test]
        public void GetMovementCost_Foot_ReturnsFootField()
        {
            var stats = new TerrainStats { moveCostFoot = 3 };
            Assert.AreEqual(3, Pathfinder.GetMovementCost(stats, MovementType.Foot));
        }

        [Test]
        public void GetMovementCost_AllTypes_ReturnCorrectField()
        {
            var stats = new TerrainStats
            {
                moveCostFoot = 1, moveCostMounted = 2, moveCostArmoured = 3,
                moveCostFlying = 4, moveCostPirate = 5, moveCostThief = 6
            };
            Assert.AreEqual(1, Pathfinder.GetMovementCost(stats, MovementType.Foot));
            Assert.AreEqual(2, Pathfinder.GetMovementCost(stats, MovementType.Mounted));
            Assert.AreEqual(3, Pathfinder.GetMovementCost(stats, MovementType.Armoured));
            Assert.AreEqual(4, Pathfinder.GetMovementCost(stats, MovementType.Flying));
            Assert.AreEqual(5, Pathfinder.GetMovementCost(stats, MovementType.Pirate));
            Assert.AreEqual(6, Pathfinder.GetMovementCost(stats, MovementType.Thief));
        }

        // --- Reachability tests ---

        [Test]
        public void Reachability_OpenPlain_Mov2_ReturnsDiamondOf13()
        {
            // 5x5 plain grid, origin at center (2,2), Mov=2
            var result = Pathfinder.ComputeReachability(
                new Vector2Int(2, 2), 2, MovementType.Foot, 5, 5,
                UniformGrid(TerrainType.Plain, 5, 5), DefaultStats(), NoOccupants());

            // Diamond with radius 2: 1 + 4 + 8... actually: tiles at dist 0,1,2
            // dist 0: 1, dist 1: 4, dist 2: 8 = 13
            Assert.AreEqual(13, result.Destinations.Count);
            Assert.IsTrue(result.Destinations.Contains(new Vector2Int(2, 2))); // origin
            Assert.IsTrue(result.Destinations.Contains(new Vector2Int(4, 2))); // 2 east
            Assert.IsTrue(result.Destinations.Contains(new Vector2Int(0, 2))); // 2 west
            Assert.IsFalse(result.Destinations.Contains(new Vector2Int(4, 4))); // too far
        }

        [Test]
        public void Reachability_ImpassableWall_BlocksExpansion()
        {
            // 5x5 grid with a wall at (3,2) blocking eastward expansion
            var overrides = new Dictionary<Vector2Int, TerrainType>
            {
                { new Vector2Int(3, 2), TerrainType.Wall }
            };

            var result = Pathfinder.ComputeReachability(
                new Vector2Int(2, 2), 3, MovementType.Foot, 5, 5,
                MixedGrid(5, 5, TerrainType.Plain, overrides),
                CustomStats(plainCost: 1, wallCost: 0), NoOccupants());

            Assert.IsFalse(result.Destinations.Contains(new Vector2Int(3, 2))); // wall itself
            // (4,2) should still be reachable by going around if Mov is enough
        }

        [Test]
        public void Reachability_ForestCosts2_ReducesReach()
        {
            // All forest grid, Mov=2, Foot costs 2 per forest tile
            var result = Pathfinder.ComputeReachability(
                new Vector2Int(2, 2), 2, MovementType.Foot, 5, 5,
                UniformGrid(TerrainType.Forest, 5, 5),
                CustomStats(forestCost: 2), NoOccupants());

            // Can only reach adjacent tiles (cost 2 each), not two tiles away (cost 4)
            Assert.AreEqual(5, result.Destinations.Count); // origin + 4 adjacent
            Assert.IsTrue(result.Destinations.Contains(new Vector2Int(3, 2)));
            Assert.IsFalse(result.Destinations.Contains(new Vector2Int(4, 2)));
        }

        [Test]
        public void Reachability_FlyingIgnoresTerrainCost()
        {
            // All forest grid, Mov=2, Flying always costs 1
            var result = Pathfinder.ComputeReachability(
                new Vector2Int(2, 2), 2, MovementType.Flying, 5, 5,
                UniformGrid(TerrainType.Forest, 5, 5),
                CustomStats(forestCost: 2), NoOccupants());

            // Flying treats everything as cost 1, so full diamond of 13
            Assert.AreEqual(13, result.Destinations.Count);
        }

        [Test]
        public void Reachability_AllyOccupied_InPassThroughNotDestinations()
        {
            var occupants = new Dictionary<Vector2Int, Pathfinder.OccupantType>
            {
                { new Vector2Int(3, 2), Pathfinder.OccupantType.Ally }
            };

            var result = Pathfinder.ComputeReachability(
                new Vector2Int(2, 2), 2, MovementType.Foot, 5, 5,
                UniformGrid(TerrainType.Plain, 5, 5), DefaultStats(),
                OccupantsAt(occupants));

            Assert.IsFalse(result.Destinations.Contains(new Vector2Int(3, 2)));
            Assert.IsTrue(result.PassThrough.Contains(new Vector2Int(3, 2)));
        }

        [Test]
        public void Reachability_EnemyOccupied_FullyBlocked()
        {
            var occupants = new Dictionary<Vector2Int, Pathfinder.OccupantType>
            {
                { new Vector2Int(3, 2), Pathfinder.OccupantType.Enemy }
            };

            var result = Pathfinder.ComputeReachability(
                new Vector2Int(2, 2), 3, MovementType.Foot, 5, 5,
                UniformGrid(TerrainType.Plain, 5, 5), DefaultStats(),
                OccupantsAt(occupants));

            Assert.IsFalse(result.Destinations.Contains(new Vector2Int(3, 2)));
            Assert.IsFalse(result.PassThrough.Contains(new Vector2Int(3, 2)));
        }

        [Test]
        public void Reachability_OriginAlwaysInDestinations_EvenMov0()
        {
            var result = Pathfinder.ComputeReachability(
                new Vector2Int(2, 2), 0, MovementType.Foot, 5, 5,
                UniformGrid(TerrainType.Plain, 5, 5), DefaultStats(), NoOccupants());

            Assert.AreEqual(1, result.Destinations.Count);
            Assert.IsTrue(result.Destinations.Contains(new Vector2Int(2, 2)));
        }

        [Test]
        public void Reachability_CanPassThroughAllyToReachBeyond()
        {
            var occupants = new Dictionary<Vector2Int, Pathfinder.OccupantType>
            {
                { new Vector2Int(3, 2), Pathfinder.OccupantType.Ally }
            };

            var result = Pathfinder.ComputeReachability(
                new Vector2Int(2, 2), 2, MovementType.Foot, 5, 5,
                UniformGrid(TerrainType.Plain, 5, 5), DefaultStats(),
                OccupantsAt(occupants));

            // Can pass through ally at (3,2) to reach (4,2)
            Assert.IsTrue(result.Destinations.Contains(new Vector2Int(4, 2)));
            Assert.IsTrue(result.PassThrough.Contains(new Vector2Int(3, 2)));
        }

        [Test]
        public void Reachability_1x1Map_OnlyOrigin()
        {
            var result = Pathfinder.ComputeReachability(
                new Vector2Int(0, 0), 5, MovementType.Foot, 1, 1,
                UniformGrid(TerrainType.Plain, 1, 1), DefaultStats(), NoOccupants());

            Assert.AreEqual(1, result.Destinations.Count);
            Assert.IsTrue(result.Destinations.Contains(new Vector2Int(0, 0)));
        }

        [Test]
        public void Reachability_32x32Map_CompletesQuickly()
        {
            var result = Pathfinder.ComputeReachability(
                new Vector2Int(16, 16), 10, MovementType.Foot, 32, 32,
                UniformGrid(TerrainType.Plain, 32, 32), DefaultStats(), NoOccupants());

            Assert.IsTrue(result.Destinations.Count > 0);
        }

        // --- Path reconstruction tests ---

        [Test]
        public void ReconstructPath_StraightLine_ReturnsStraightPath()
        {
            var result = Pathfinder.ComputeReachability(
                new Vector2Int(0, 0), 4, MovementType.Foot, 5, 1,
                UniformGrid(TerrainType.Plain, 5, 1), DefaultStats(), NoOccupants());

            var path = Pathfinder.ReconstructPath(
                new Vector2Int(0, 0), new Vector2Int(4, 0), result);

            Assert.IsNotNull(path);
            Assert.AreEqual(5, path.Count);
            Assert.AreEqual(new Vector2Int(0, 0), path[0]);
            Assert.AreEqual(new Vector2Int(4, 0), path[4]);

            // Each step should be +1 in x
            for (int i = 0; i < path.Count; i++)
                Assert.AreEqual(new Vector2Int(i, 0), path[i]);
        }

        [Test]
        public void ReconstructPath_UnreachableDestination_ReturnsNull()
        {
            var result = Pathfinder.ComputeReachability(
                new Vector2Int(0, 0), 1, MovementType.Foot, 5, 5,
                UniformGrid(TerrainType.Plain, 5, 5), DefaultStats(), NoOccupants());

            var path = Pathfinder.ReconstructPath(
                new Vector2Int(0, 0), new Vector2Int(4, 4), result);

            Assert.IsNull(path);
        }

        [Test]
        public void ReconstructPath_OriginEqualsDestination_ReturnsSingleElement()
        {
            var result = Pathfinder.ComputeReachability(
                new Vector2Int(2, 2), 3, MovementType.Foot, 5, 5,
                UniformGrid(TerrainType.Plain, 5, 5), DefaultStats(), NoOccupants());

            var path = Pathfinder.ReconstructPath(
                new Vector2Int(2, 2), new Vector2Int(2, 2), result);

            Assert.IsNotNull(path);
            Assert.AreEqual(1, path.Count);
            Assert.AreEqual(new Vector2Int(2, 2), path[0]);
        }

        // --- Attack range tests ---

        [Test]
        public void AttackRange_SingleTile_Range1_Returns4Cardinals()
        {
            var destinations = new HashSet<Vector2Int> { new Vector2Int(2, 2) };
            var range = Pathfinder.ComputeAttackRange(destinations, 1, 1, 5, 5);

            Assert.AreEqual(4, range.Count);
            Assert.IsTrue(range.Contains(new Vector2Int(3, 2)));
            Assert.IsTrue(range.Contains(new Vector2Int(1, 2)));
            Assert.IsTrue(range.Contains(new Vector2Int(2, 3)));
            Assert.IsTrue(range.Contains(new Vector2Int(2, 1)));
            Assert.IsFalse(range.Contains(new Vector2Int(2, 2))); // destination excluded
        }

        [Test]
        public void AttackRange_SingleTile_Range1to2_ReturnsDiamond()
        {
            var destinations = new HashSet<Vector2Int> { new Vector2Int(2, 2) };
            var range = Pathfinder.ComputeAttackRange(destinations, 1, 2, 5, 5);

            // dist 1: 4 tiles, dist 2: 8 tiles = 12 total
            Assert.AreEqual(12, range.Count);
        }

        [Test]
        public void AttackRange_MultipleTiles_UnionCorrect()
        {
            var destinations = new HashSet<Vector2Int>
            {
                new Vector2Int(1, 1),
                new Vector2Int(3, 1)
            };
            var range = Pathfinder.ComputeAttackRange(destinations, 1, 1, 5, 5);

            // Both tiles' cardinals, minus the destinations themselves
            Assert.IsTrue(range.Contains(new Vector2Int(0, 1))); // from (1,1)
            Assert.IsTrue(range.Contains(new Vector2Int(4, 1))); // from (3,1)
            Assert.IsTrue(range.Contains(new Vector2Int(2, 1))); // shared neighbor
            Assert.IsFalse(range.Contains(new Vector2Int(1, 1))); // destination excluded
            Assert.IsFalse(range.Contains(new Vector2Int(3, 1))); // destination excluded
        }

        [Test]
        public void AttackRange_ClampsToMapBounds()
        {
            var destinations = new HashSet<Vector2Int> { new Vector2Int(0, 0) };
            var range = Pathfinder.ComputeAttackRange(destinations, 1, 1, 5, 5);

            // Corner: only 2 of 4 cardinals are in bounds
            Assert.AreEqual(2, range.Count);
            Assert.IsTrue(range.Contains(new Vector2Int(1, 0)));
            Assert.IsTrue(range.Contains(new Vector2Int(0, 1)));
        }

        // --- Thief movement type tests ---

        [Test]
        public void GetMovementCost_Thief_ReturnsThiefField()
        {
            var stats = new TerrainStats { moveCostThief = 7 };
            Assert.AreEqual(7, Pathfinder.GetMovementCost(stats, MovementType.Thief));
        }

        [Test]
        public void Reachability_ThiefOnForest_Cost1()
        {
            // Thief treats forest as cost 1 (vs Foot's cost 2)
            Func<TerrainType, TerrainStats> thiefForestStats = t =>
            {
                var s = TerrainStats.Default;
                if (t == TerrainType.Forest)
                {
                    s.moveCostFoot = 2;
                    s.moveCostThief = 1;
                }
                return s;
            };

            var result = Pathfinder.ComputeReachability(
                new Vector2Int(2, 2), 2, MovementType.Thief, 5, 5,
                UniformGrid(TerrainType.Forest, 5, 5), thiefForestStats, NoOccupants());

            // Cost 1 per tile means full diamond of 13 (same as plain)
            Assert.AreEqual(13, result.Destinations.Count);
        }
    }
}
