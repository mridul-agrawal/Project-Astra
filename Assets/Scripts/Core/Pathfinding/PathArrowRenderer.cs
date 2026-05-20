using System.Collections.Generic;
using UnityEngine;

namespace ProjectAstra.Core.Pathfinding
{
    // Renders the tile-to-tile path arrow shown while the player picks a
    // destination. Builds all segment sprites procedurally on Awake, then
    // stitches them together (straights, turns, arrowhead) and pools the
    // GameObjects for reuse.
    public class PathArrowRenderer : MonoBehaviour
    {
        private static readonly Color ArrowColor = new(0.5f, 0.95f, 1.0f, 0.85f);
        private static readonly Color32 SpritePixelColor = new(255, 255, 255, 255);

        const int SpriteSize = 16;
        const int BodyMin = 5;
        const int BodyMax = 10;
        // 16-px sprite splits at the midline. Forward arrows have body in
        // [0..7] and head in [7..15]; reverse arrows are mirrored at 8.
        const int ForwardBodyEnd = 7;
        const int ReverseBodyStart = 8;

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

        // --- Segment classification ---

        private static SegmentType ClassifySegment(List<Vector2Int> path, int i)
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

        // --- Pooled placement ---

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

        // --- Procedural sprite generation ---

        private static Dictionary<SegmentType, Sprite> GenerateAllSprites()
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

        private static Sprite BuildStraightH()
        {
            var px = new Color32[SpriteSize * SpriteSize];
            FillRect(px, 0, BodyMin, SpriteSize - 1, BodyMax);
            return MakeSprite(px);
        }

        private static Sprite BuildStraightV()
        {
            var px = new Color32[SpriteSize * SpriteSize];
            FillRect(px, BodyMin, 0, BodyMax, SpriteSize - 1);
            return MakeSprite(px);
        }

        private static Sprite BuildTurn(bool bottom, bool left)
        {
            var px = new Color32[SpriteSize * SpriteSize];

            int vYMin = bottom ? 0 : BodyMin;
            int vYMax = bottom ? BodyMax : SpriteSize - 1;
            FillRect(px, BodyMin, vYMin, BodyMax, vYMax);

            int hXMin = left ? 0 : BodyMin;
            int hXMax = left ? BodyMax : SpriteSize - 1;
            FillRect(px, hXMin, BodyMin, hXMax, BodyMax);

            return MakeSprite(px);
        }

        private static Sprite BuildArrow(Vector2Int dir)
        {
            var px = new Color32[SpriteSize * SpriteSize];

            if (dir == Vector2Int.right)
            {
                FillRect(px, 0, BodyMin, ForwardBodyEnd, BodyMax);
                FillTriangle(px, axis: 0, from: ForwardBodyEnd, to: SpriteSize - 1, forward: true);
            }
            else if (dir == Vector2Int.left)
            {
                FillRect(px, ReverseBodyStart, BodyMin, SpriteSize - 1, BodyMax);
                FillTriangle(px, axis: 0, from: ReverseBodyStart, to: 0, forward: false);
            }
            else if (dir == Vector2Int.up)
            {
                FillRect(px, BodyMin, 0, BodyMax, ForwardBodyEnd);
                FillTriangle(px, axis: 1, from: ForwardBodyEnd, to: SpriteSize - 1, forward: true);
            }
            else
            {
                FillRect(px, BodyMin, ReverseBodyStart, BodyMax, SpriteSize - 1);
                FillTriangle(px, axis: 1, from: ReverseBodyStart, to: 0, forward: false);
            }

            return MakeSprite(px);
        }

        private static void FillRect(Color32[] px, int x0, int y0, int x1, int y1)
        {
            for (int y = y0; y <= y1; y++)
                for (int x = x0; x <= x1; x++)
                    px[y * SpriteSize + x] = SpritePixelColor;
        }

        // Draws a filled triangle (arrowhead) along the given axis. axis=0
        // means horizontal (x varies, y tapers); axis=1 means vertical (y
        // varies, x tapers). forward steps in increasing coordinate; reverse
        // steps backward.
        private static void FillTriangle(Color32[] px, int axis, int from, int to, bool forward)
        {
            const float BaseHalfWidth = 5f;
            const float TipHalfWidth = 0.5f;
            const float SpriteCenter = 7.5f;

            int step = forward ? 1 : -1;
            int length = Mathf.Abs(to - from);

            for (int d = 0; d <= length; d++)
            {
                int coord = from + d * step;
                float t = (float)d / length;
                float halfSpan = Mathf.Lerp(BaseHalfWidth, TipHalfWidth, t);
                int spanMin = Mathf.Max(0, Mathf.CeilToInt(SpriteCenter - halfSpan));
                int spanMax = Mathf.Min(SpriteSize - 1, Mathf.FloorToInt(SpriteCenter + halfSpan));

                for (int s = spanMin; s <= spanMax; s++)
                {
                    int x = axis == 0 ? coord : s;
                    int y = axis == 0 ? s : coord;
                    px[y * SpriteSize + x] = SpritePixelColor;
                }
            }
        }

        private static Sprite MakeSprite(Color32[] pixels)
        {
            var tex = new Texture2D(SpriteSize, SpriteSize, TextureFormat.RGBA32, false);
            tex.filterMode = FilterMode.Point;
            tex.SetPixels32(pixels);
            tex.Apply();
            return Sprite.Create(tex, new Rect(0, 0, SpriteSize, SpriteSize), new Vector2(0.5f, 0.5f), SpriteSize);
        }

        private enum SegmentType
        {
            StraightH, StraightV,
            TurnBL, TurnBR, TurnTL, TurnTR,
            ArrowUp, ArrowDown, ArrowLeft, ArrowRight
        }
    }
}
