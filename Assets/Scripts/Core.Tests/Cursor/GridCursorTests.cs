using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;
using ProjectAstra.Core;
using ProjectAstra.Core.Grid;
using ProjectAstra.Core.Cursor;
using CursorMode = ProjectAstra.Core.Cursor.CursorMode;

namespace ProjectAstra.Core.Tests.Cursor
{
    [TestFixture]
    public class GridCursorTests
    {
        private GameObject cursorGO;
        private GridCursor cursor;
        private MapRenderer mapRenderer;
        private MapData mapData;
        private SpriteRenderer spriteRenderer;

        [SetUp]
        public void SetUp()
        {
            // Create MapRenderer with a 5x5 map
            var mapRendererGO = new GameObject("MapRenderer");
            mapRenderer = mapRendererGO.AddComponent<MapRenderer>();
            mapData = ScriptableObject.CreateInstance<MapData>();
            var so = new UnityEditor.SerializedObject(mapData);
            so.FindProperty("width").intValue = 5;
            so.FindProperty("height").intValue = 5;
            so.ApplyModifiedPropertiesWithoutUndo();

            // Load map into renderer via reflection (set currentMap)
            var mapSo = new UnityEditor.SerializedObject(mapRenderer);
            // Can't easily load map without tilemaps, so use Initialize method
            // Instead, set the field directly
            var currentMapField = typeof(MapRenderer).GetField("currentMap",
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            currentMapField.SetValue(mapRenderer, mapData);

            // Create GridCursor
            cursorGO = new GameObject("GridCursor");
            cursor = cursorGO.AddComponent<GridCursor>();

            // Add a SpriteRenderer child
            var spriteGO = new GameObject("CursorSprite");
            spriteGO.transform.SetParent(cursorGO.transform);
            spriteRenderer = spriteGO.AddComponent<SpriteRenderer>();

            cursor.Initialize(mapRenderer, null);
            cursor.SetSpriteRenderer(spriteRenderer);
        }

        [TearDown]
        public void TearDown()
        {
            Object.DestroyImmediate(cursorGO);
            Object.DestroyImmediate(mapRenderer.gameObject);
            Object.DestroyImmediate(mapData);
        }

        // --- Position tests ---

        [Test]
        public void InitialPosition_IsZeroZero()
        {
            Assert.AreEqual(Vector2Int.zero, cursor.GridPosition);
        }

        [Test]
        public void SetPosition_UpdatesGridPosition()
        {
            cursor.SetPosition(new Vector2Int(3, 2));
            Assert.AreEqual(new Vector2Int(3, 2), cursor.GridPosition);
        }

        [Test]
        public void SetPosition_ClampsNegativeToZero()
        {
            cursor.SetPosition(new Vector2Int(-5, -3));
            Assert.AreEqual(Vector2Int.zero, cursor.GridPosition);
        }

        [Test]
        public void SetPosition_ClampsOverMaxToBounds()
        {
            cursor.SetPosition(new Vector2Int(99, 99));
            Assert.AreEqual(new Vector2Int(4, 4), cursor.GridPosition);
        }

        // --- Movement tests ---

        [Test]
        public void HandleCursorMove_InFreeMode_UpdatesPosition()
        {
            cursor.SetMode(CursorMode.Free);
            cursor.SetPosition(new Vector2Int(2, 2));

            cursor.HandleCursorMove(Vector2Int.right);

            Assert.AreEqual(new Vector2Int(3, 2), cursor.GridPosition);
        }

        [Test]
        public void HandleCursorMove_InLockedMode_DoesNotMove()
        {
            cursor.SetMode(CursorMode.Locked);
            cursor.SetPosition(new Vector2Int(2, 2));

            cursor.HandleCursorMove(Vector2Int.right);

            Assert.AreEqual(new Vector2Int(2, 2), cursor.GridPosition);
        }

        [Test]
        public void HandleCursorMove_ClampsAtMapEdge()
        {
            cursor.SetMode(CursorMode.Free);
            cursor.SetPosition(new Vector2Int(0, 0));

            cursor.HandleCursorMove(Vector2Int.left);

            Assert.AreEqual(new Vector2Int(0, 0), cursor.GridPosition);
        }

        [Test]
        public void HandleCursorMove_FiresOnCursorMovedEvent()
        {
            cursor.SetMode(CursorMode.Free);
            cursor.SetPosition(new Vector2Int(2, 2));

            Vector2Int? received = null;
            cursor.OnCursorMoved += pos => received = pos;

            cursor.HandleCursorMove(Vector2Int.up);

            Assert.IsTrue(received.HasValue);
            Assert.AreEqual(new Vector2Int(2, 3), received.Value);
        }

        [Test]
        public void HandleCursorMove_DoesNotFireEvent_WhenClampedToSamePosition()
        {
            cursor.SetMode(CursorMode.Free);
            cursor.SetPosition(new Vector2Int(0, 0));

            bool fired = false;
            cursor.OnCursorMoved += _ => fired = true;

            cursor.HandleCursorMove(Vector2Int.left);

            Assert.IsFalse(fired);
        }

        [Test]
        public void HandleCursorMove_WithConstraints_BlocksInvalidTile()
        {
            cursor.SetMode(CursorMode.Free);
            cursor.SetPosition(new Vector2Int(2, 2));

            // Simulate entering a constrained mode by injecting the
            // valid-tile set through the cursor's internal SetValidMoveTiles
            // seam (used in production by ActionMenuFlow to swap from move
            // to attack/heal targeting).
            cursor.SetValidMoveTiles(new HashSet<Vector2Int>
            {
                new Vector2Int(2, 2),
                new Vector2Int(3, 2),
                new Vector2Int(2, 3)
            });

            // Move right — (3,2) is valid
            cursor.HandleCursorMove(Vector2Int.right);
            Assert.AreEqual(new Vector2Int(3, 2), cursor.GridPosition);

            // Move right again — (4,2) is NOT in valid set
            cursor.HandleCursorMove(Vector2Int.right);
            Assert.AreEqual(new Vector2Int(3, 2), cursor.GridPosition); // Stayed
        }

        // --- Mode tests ---

        [Test]
        public void SetMode_ToLocked_DisablesSpriteRenderer()
        {
            cursor.SetMode(CursorMode.Locked);
            Assert.IsFalse(spriteRenderer.enabled);
        }

        [Test]
        public void SetMode_ToFree_EnablesSpriteRenderer()
        {
            cursor.SetMode(CursorMode.Locked);
            cursor.SetMode(CursorMode.Free);
            Assert.IsTrue(spriteRenderer.enabled);
        }

        [Test]
        public void SetMode_PreservesPosition()
        {
            cursor.SetPosition(new Vector2Int(3, 3));
            cursor.SetMode(CursorMode.Locked);
            cursor.SetMode(CursorMode.Free);
            Assert.AreEqual(new Vector2Int(3, 3), cursor.GridPosition);
        }

        // --- Memory tests ---

        [Test]
        public void SetPositionWithMemory_ThenReturn_RestoresOriginalPosition()
        {
            cursor.SetPosition(new Vector2Int(1, 1));
            cursor.SetPositionWithMemory(new Vector2Int(3, 3));

            Assert.AreEqual(new Vector2Int(3, 3), cursor.GridPosition);

            cursor.ReturnToMemorizedPosition();

            Assert.AreEqual(new Vector2Int(1, 1), cursor.GridPosition);
        }

        [Test]
        public void ReturnToMemorizedPosition_WithoutMemory_IsNoOp()
        {
            cursor.SetPosition(new Vector2Int(2, 2));
            cursor.ReturnToMemorizedPosition();
            Assert.AreEqual(new Vector2Int(2, 2), cursor.GridPosition);
        }
    }
}
