using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;
using ProjectAstra.Core;

namespace ProjectAstra.Core.Tests
{
    [TestFixture]
    public class GridCursorTests
    {
        private GameObject _cursorGO;
        private GridCursor _cursor;
        private MapRenderer _mapRenderer;
        private MapData _mapData;
        private SpriteRenderer _spriteRenderer;

        [SetUp]
        public void SetUp()
        {
            // Create MapRenderer with a 5x5 map
            var mapRendererGO = new GameObject("MapRenderer");
            _mapRenderer = mapRendererGO.AddComponent<MapRenderer>();
            _mapData = ScriptableObject.CreateInstance<MapData>();
            var so = new UnityEditor.SerializedObject(_mapData);
            so.FindProperty("_width").intValue = 5;
            so.FindProperty("_height").intValue = 5;
            so.ApplyModifiedPropertiesWithoutUndo();

            // Load map into renderer via reflection (set _currentMap)
            var mapSo = new UnityEditor.SerializedObject(_mapRenderer);
            // Can't easily load map without tilemaps, so use Initialize method
            // Instead, set the field directly
            var currentMapField = typeof(MapRenderer).GetField("_currentMap",
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            currentMapField.SetValue(_mapRenderer, _mapData);

            // Create GridCursor
            _cursorGO = new GameObject("GridCursor");
            _cursor = _cursorGO.AddComponent<GridCursor>();

            // Add a SpriteRenderer child
            var spriteGO = new GameObject("CursorSprite");
            spriteGO.transform.SetParent(_cursorGO.transform);
            _spriteRenderer = spriteGO.AddComponent<SpriteRenderer>();

            _cursor.Initialize(_mapRenderer, null);
            _cursor.SetSpriteRenderer(_spriteRenderer);
        }

        [TearDown]
        public void TearDown()
        {
            Object.DestroyImmediate(_cursorGO);
            Object.DestroyImmediate(_mapRenderer.gameObject);
            Object.DestroyImmediate(_mapData);
        }

        // --- Position tests ---

        [Test]
        public void InitialPosition_IsZeroZero()
        {
            Assert.AreEqual(Vector2Int.zero, _cursor.GridPosition);
        }

        [Test]
        public void SetPosition_UpdatesGridPosition()
        {
            _cursor.SetPosition(new Vector2Int(3, 2));
            Assert.AreEqual(new Vector2Int(3, 2), _cursor.GridPosition);
        }

        [Test]
        public void SetPosition_ClampsNegativeToZero()
        {
            _cursor.SetPosition(new Vector2Int(-5, -3));
            Assert.AreEqual(Vector2Int.zero, _cursor.GridPosition);
        }

        [Test]
        public void SetPosition_ClampsOverMaxToBounds()
        {
            _cursor.SetPosition(new Vector2Int(99, 99));
            Assert.AreEqual(new Vector2Int(4, 4), _cursor.GridPosition);
        }

        // --- Movement tests ---

        [Test]
        public void HandleCursorMove_InFreeMode_UpdatesPosition()
        {
            _cursor.SetMode(CursorMode.Free);
            _cursor.SetPosition(new Vector2Int(2, 2));

            _cursor.HandleCursorMove(Vector2Int.right);

            Assert.AreEqual(new Vector2Int(3, 2), _cursor.GridPosition);
        }

        [Test]
        public void HandleCursorMove_InLockedMode_DoesNotMove()
        {
            _cursor.SetMode(CursorMode.Locked);
            _cursor.SetPosition(new Vector2Int(2, 2));

            _cursor.HandleCursorMove(Vector2Int.right);

            Assert.AreEqual(new Vector2Int(2, 2), _cursor.GridPosition);
        }

        [Test]
        public void HandleCursorMove_ClampsAtMapEdge()
        {
            _cursor.SetMode(CursorMode.Free);
            _cursor.SetPosition(new Vector2Int(0, 0));

            _cursor.HandleCursorMove(Vector2Int.left);

            Assert.AreEqual(new Vector2Int(0, 0), _cursor.GridPosition);
        }

        [Test]
        public void HandleCursorMove_FiresOnCursorMovedEvent()
        {
            _cursor.SetMode(CursorMode.Free);
            _cursor.SetPosition(new Vector2Int(2, 2));

            Vector2Int? received = null;
            _cursor.OnCursorMoved += pos => received = pos;

            _cursor.HandleCursorMove(Vector2Int.up);

            Assert.IsTrue(received.HasValue);
            Assert.AreEqual(new Vector2Int(2, 3), received.Value);
        }

        [Test]
        public void HandleCursorMove_DoesNotFireEvent_WhenClampedToSamePosition()
        {
            _cursor.SetMode(CursorMode.Free);
            _cursor.SetPosition(new Vector2Int(0, 0));

            bool fired = false;
            _cursor.OnCursorMoved += _ => fired = true;

            _cursor.HandleCursorMove(Vector2Int.left);

            Assert.IsFalse(fired);
        }

        [Test]
        public void HandleCursorMove_WithConstraints_BlocksInvalidTile()
        {
            _cursor.SetMode(CursorMode.Free);
            _cursor.SetPosition(new Vector2Int(2, 2));

            // Simulate entering a constrained mode by going through UnitSelected
            // Use HandleCursorMove with valid tile set via reflection
            var field = typeof(GridCursor).GetField("_validMoveTiles",
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            field.SetValue(_cursor, new HashSet<Vector2Int>
            {
                new Vector2Int(2, 2),
                new Vector2Int(3, 2),
                new Vector2Int(2, 3)
            });

            // Move right — (3,2) is valid
            _cursor.HandleCursorMove(Vector2Int.right);
            Assert.AreEqual(new Vector2Int(3, 2), _cursor.GridPosition);

            // Move right again — (4,2) is NOT in valid set
            _cursor.HandleCursorMove(Vector2Int.right);
            Assert.AreEqual(new Vector2Int(3, 2), _cursor.GridPosition); // Stayed
        }

        // --- Mode tests ---

        [Test]
        public void SetMode_ToLocked_DisablesSpriteRenderer()
        {
            _cursor.SetMode(CursorMode.Locked);
            Assert.IsFalse(_spriteRenderer.enabled);
        }

        [Test]
        public void SetMode_ToFree_EnablesSpriteRenderer()
        {
            _cursor.SetMode(CursorMode.Locked);
            _cursor.SetMode(CursorMode.Free);
            Assert.IsTrue(_spriteRenderer.enabled);
        }

        [Test]
        public void SetMode_PreservesPosition()
        {
            _cursor.SetPosition(new Vector2Int(3, 3));
            _cursor.SetMode(CursorMode.Locked);
            _cursor.SetMode(CursorMode.Free);
            Assert.AreEqual(new Vector2Int(3, 3), _cursor.GridPosition);
        }

        // --- Memory tests ---

        [Test]
        public void SetPositionWithMemory_ThenReturn_RestoresOriginalPosition()
        {
            _cursor.SetPosition(new Vector2Int(1, 1));
            _cursor.SetPositionWithMemory(new Vector2Int(3, 3));

            Assert.AreEqual(new Vector2Int(3, 3), _cursor.GridPosition);

            _cursor.ReturnToMemorizedPosition();

            Assert.AreEqual(new Vector2Int(1, 1), _cursor.GridPosition);
        }

        [Test]
        public void ReturnToMemorizedPosition_WithoutMemory_IsNoOp()
        {
            _cursor.SetPosition(new Vector2Int(2, 2));
            _cursor.ReturnToMemorizedPosition();
            Assert.AreEqual(new Vector2Int(2, 2), _cursor.GridPosition);
        }
    }
}
