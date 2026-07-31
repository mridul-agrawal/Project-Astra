using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;
using ProjectAstra.Core.Units;
using ProjectAstra.Core.Animation;
using CursorMode = ProjectAstra.Core.Cursor.CursorMode;

namespace ProjectAstra.Core.Tests.Animation
{
    [TestFixture]
    public class HoverSelectionModelTests
    {
        private TestUnit selectable;
        private TestUnit notSelectable;
        private Dictionary<Vector2Int, TestUnit> board;

        [SetUp]
        public void SetUp()
        {
            selectable = new GameObject("Selectable").AddComponent<TestUnit>();
            notSelectable = new GameObject("NotSelectable").AddComponent<TestUnit>();
            board = new Dictionary<Vector2Int, TestUnit>
            {
                { new Vector2Int(2, 2), selectable },
                { new Vector2Int(5, 5), notSelectable }
            };
        }

        [TearDown]
        public void TearDown()
        {
            Object.DestroyImmediate(selectable.gameObject);
            Object.DestroyImmediate(notSelectable.gameObject);
        }

        private TestUnit UnitAt(Vector2Int p) => board.TryGetValue(p, out var u) ? u : null;
        private bool IsSelectable(TestUnit u) => u == selectable;

        [Test]
        public void Free_OverSelectableUnit_SelectsIt()
        {
            var result = HoverSelectionModel.ResolveSelected(
                CursorMode.Free, new Vector2Int(2, 2), null, UnitAt, IsSelectable);
            Assert.AreEqual(selectable, result);
        }

        [Test]
        public void Free_OverNonSelectableUnit_SelectsNothing()
        {
            var result = HoverSelectionModel.ResolveSelected(
                CursorMode.Free, new Vector2Int(5, 5), null, UnitAt, IsSelectable);
            Assert.IsNull(result);
        }

        [Test]
        public void Free_OverEmptyTile_SelectsNothing()
        {
            var result = HoverSelectionModel.ResolveSelected(
                CursorMode.Free, new Vector2Int(9, 9), null, UnitAt, IsSelectable);
            Assert.IsNull(result);
        }

        [Test]
        public void UnitSelected_KeepsPickedUpUnit_RegardlessOfCursor()
        {
            var result = HoverSelectionModel.ResolveSelected(
                CursorMode.UnitSelected, new Vector2Int(9, 9), selectable, UnitAt, IsSelectable);
            Assert.AreEqual(selectable, result);
        }

        [Test]
        public void Targeting_KeepsPickedUpUnit()
        {
            var result = HoverSelectionModel.ResolveSelected(
                CursorMode.Targeting, new Vector2Int(0, 0), selectable, UnitAt, IsSelectable);
            Assert.AreEqual(selectable, result);
        }

        [Test]
        public void Locked_SelectsNothing()
        {
            var result = HoverSelectionModel.ResolveSelected(
                CursorMode.Locked, new Vector2Int(2, 2), null, UnitAt, IsSelectable);
            Assert.IsNull(result);
        }
    }
}
