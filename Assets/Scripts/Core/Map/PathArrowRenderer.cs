using System.Collections.Generic;
using UnityEngine;

namespace ProjectAstra.Core
{
    public class PathArrowRenderer : MonoBehaviour
    {
        private static readonly Color ArrowColor = new(0.5f, 0.95f, 1.0f, 0.85f);

        const int Size = 16;
        const int BodyMin = 5;
        const int BodyMax = 10;

        private readonly List<GameObject> _activeSegments = new();
        private readonly Queue<GameObject> _pool = new();
        private Dictionary<SegmentType, Sprite> _sprites;
        private Transform _container;

        private void Awake()
        {
            _container = new GameObject("PathArrows").transform;
            _sprites = GenerateAllSprites();
        }

        private void OnDestroy()
        {
            if (_container != null)
                Destroy(_container.gameObject);
        }

        public void ShowPath(List<Vector2Int> path)
        {
            Clear();
            if (path == null || path.Count < 2) return;

            for (int i = 1; i < path.Count; i++)
            {
                var type = ClassifySegment(path, i);
                PlaceSegment(path[i], type);
            }
        }

        public void Clear()
        {
            foreach (var obj in _activeSegments)
            {
                obj.SetActive(false);
                _pool.Enqueue(obj);
            }
            _activeSegments.Clear();
        }

        #region Segment classification

        private SegmentType ClassifySegment(List<Vector2Int> path, int i)
        {
            var inDir = path[i] - path[i - 1];

            if (i == path.Count - 1)
                return ClassifyArrowhead(inDir);

            var outDir = path[i + 1] - path[i];

            if (inDir == outDir)
                return inDir.x != 0 ? SegmentType.StraightH : SegmentType.StraightV;

            return ClassifyTurn(inDir, outDir);
        }

        private static SegmentType ClassifyArrowhead(Vector2Int dir)
        {
            if (dir == Vector2Int.right) return SegmentType.ArrowRight;
            if (dir == Vector2Int.left) return SegmentType.ArrowLeft;
            if (dir == Vector2Int.up) return SegmentType.ArrowUp;
            return SegmentType.ArrowDown;
        }

        private static SegmentType ClassifyTurn(Vector2Int inDir, Vector2Int outDir)
        {
            bool hasBottom = (inDir == Vector2Int.up) || (outDir == Vector2Int.down);
            bool hasTop = (inDir == Vector2Int.down) || (outDir == Vector2Int.up);
            bool hasLeft = (inDir == Vector2Int.right) || (outDir == Vector2Int.left);
            bool hasRight = (inDir == Vector2Int.left) || (outDir == Vector2Int.right);

            if (hasBottom && hasLeft) return SegmentType.TurnBL;
            if (hasBottom && hasRight) return SegmentType.TurnBR;
            if (hasTop && hasLeft) return SegmentType.TurnTL;
            return SegmentType.TurnTR;
        }

        #endregion

        #region Pooled placement

        private void PlaceSegment(Vector2Int tile, SegmentType type)
        {
            var obj = GetOrCreateSegment();
            obj.transform.position = new Vector3(tile.x + 0.5f, tile.y + 0.5f, 0f);
            obj.SetActive(true);

            var sr = obj.GetComponent<SpriteRenderer>();
            sr.sprite = _sprites[type];
            sr.color = ArrowColor;

            _activeSegments.Add(obj);
        }

        private GameObject GetOrCreateSegment()
        {
            if (_pool.Count > 0)
                return _pool.Dequeue();

            var obj = new GameObject("PathArrow");
            obj.transform.SetParent(_container);

            var sr = obj.AddComponent<SpriteRenderer>();
            sr.sortingLayerName = "UIOverlay";
            sr.sortingOrder = 0;

            return obj;
        }

        #endregion

        #region Procedural sprite generation

        private Dictionary<SegmentType, Sprite> GenerateAllSprites()
        {
            return new Dictionary<SegmentType, Sprite>
            {
                [SegmentType.StraightH] = BuildStraightH(),
                [SegmentType.StraightV] = BuildStraightV(),
                [SegmentType.TurnBL] = BuildTurn(bottom: true, left: true),
                [SegmentType.TurnBR] = BuildTurn(bottom: true, left: false),
                [SegmentType.TurnTL] = BuildTurn(bottom: false, left: true),
                [SegmentType.TurnTR] = BuildTurn(bottom: false, left: false),
                [SegmentType.ArrowUp] = BuildArrow(Vector2Int.up),
                [SegmentType.ArrowDown] = BuildArrow(Vector2Int.down),
                [SegmentType.ArrowLeft] = BuildArrow(Vector2Int.left),
                [SegmentType.ArrowRight] = BuildArrow(Vector2Int.right),
            };
        }

        private Sprite BuildStraightH()
        {
            var px = new Color32[Size * Size];
            FillRect(px, 0, BodyMin, Size - 1, BodyMax);
            return MakeSprite(px);
        }

        private Sprite BuildStraightV()
        {
            var px = new Color32[Size * Size];
            FillRect(px, BodyMin, 0, BodyMax, Size - 1);
            return MakeSprite(px);
        }

        private Sprite BuildTurn(bool bottom, bool left)
        {
            var px = new Color32[Size * Size];

            int vYMin = bottom ? 0 : BodyMin;
            int vYMax = bottom ? BodyMax : Size - 1;
            FillRect(px, BodyMin, vYMin, BodyMax, vYMax);

            int hXMin = left ? 0 : BodyMin;
            int hXMax = left ? BodyMax : Size - 1;
            FillRect(px, hXMin, BodyMin, hXMax, BodyMax);

            return MakeSprite(px);
        }

        private Sprite BuildArrow(Vector2Int dir)
        {
            var px = new Color32[Size * Size];

            if (dir == Vector2Int.right)
            {
                FillRect(px, 0, BodyMin, 7, BodyMax);
                FillTriangle(px, axis: 0, from: 7, to: Size - 1, forward: true);
            }
            else if (dir == Vector2Int.left)
            {
                FillRect(px, 8, BodyMin, Size - 1, BodyMax);
                FillTriangle(px, axis: 0, from: 8, to: 0, forward: false);
            }
            else if (dir == Vector2Int.up)
            {
                FillRect(px, BodyMin, 0, BodyMax, 7);
                FillTriangle(px, axis: 1, from: 7, to: Size - 1, forward: true);
            }
            else
            {
                FillRect(px, BodyMin, 8, BodyMax, Size - 1);
                FillTriangle(px, axis: 1, from: 8, to: 0, forward: false);
            }

            return MakeSprite(px);
        }

        private static void FillRect(Color32[] px, int x0, int y0, int x1, int y1)
        {
            var white = new Color32(255, 255, 255, 255);
            for (int y = y0; y <= y1; y++)
                for (int x = x0; x <= x1; x++)
                    px[y * Size + x] = white;
        }

        /// <summary>
        /// Draws a filled triangle (arrowhead) along the given axis.
        /// axis=0: horizontal (x varies, y tapers). axis=1: vertical (y varies, x tapers).
        /// forward=true: increasing coordinate. forward=false: decreasing.
        /// </summary>
        private static void FillTriangle(Color32[] px, int axis, int from, int to, bool forward)
        {
            var white = new Color32(255, 255, 255, 255);
            float baseHalf = 5f;
            float tipHalf = 0.5f;
            float center = 7.5f;

            int step = forward ? 1 : -1;
            int length = Mathf.Abs(to - from);

            for (int d = 0; d <= length; d++)
            {
                int coord = from + d * step;
                float t = (float)d / length;
                float halfSpan = Mathf.Lerp(baseHalf, tipHalf, t);
                int spanMin = Mathf.Max(0, Mathf.CeilToInt(center - halfSpan));
                int spanMax = Mathf.Min(Size - 1, Mathf.FloorToInt(center + halfSpan));

                for (int s = spanMin; s <= spanMax; s++)
                {
                    int x = axis == 0 ? coord : s;
                    int y = axis == 0 ? s : coord;
                    px[y * Size + x] = white;
                }
            }
        }

        private static Sprite MakeSprite(Color32[] pixels)
        {
            var tex = new Texture2D(Size, Size, TextureFormat.RGBA32, false);
            tex.filterMode = FilterMode.Point;
            tex.SetPixels32(pixels);
            tex.Apply();
            return Sprite.Create(tex, new Rect(0, 0, Size, Size), new Vector2(0.5f, 0.5f), Size);
        }

        #endregion

        private enum SegmentType
        {
            StraightH, StraightV,
            TurnBL, TurnBR, TurnTL, TurnTR,
            ArrowUp, ArrowDown, ArrowLeft, ArrowRight
        }
    }
}
