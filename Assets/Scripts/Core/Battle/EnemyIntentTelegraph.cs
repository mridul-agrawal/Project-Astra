using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using ProjectAstra.Core.Grid;
using ProjectAstra.Core.Pathfinding;
using ProjectAstra.Core.UI.BattleMap;

namespace ProjectAstra.Core.Battle
{
    // Persistent on-map enemy-intent telegraph: a red path arrow from an enemy toward
    // its target, a pulsing "!" over the threatened unit, and an optional gold pulse on
    // the tile that answers the threat. Owns a private PathArrowRenderer instance so
    // the player's planning arrow is untouched.
    public class EnemyIntentTelegraph : MonoBehaviour
    {
        private static readonly Color ThreatRed = new(0.95f, 0.27f, 0.22f, 0.85f);
        private static readonly Color HintGold = new(1f, 0.78f, 0.25f, 0.45f);
        private const int UnderPlayerArrow = -1;

        private PathArrowRenderer arrow;
        private GameObject threatMarker;
        private SpriteRenderer hintOverlay;
        private Coroutine pulseRoutine;

        private void Awake()
        {
            arrow = gameObject.AddComponent<PathArrowRenderer>();
            arrow.SetStyle(ThreatRed, UnderPlayerArrow);
        }

        public void Show(IReadOnlyList<Vector2Int> path, Vector2Int threatenedTile, Vector2Int? hintTile)
        {
            Clear();

            arrow.ShowPath(new List<Vector2Int>(path));
            threatMarker = WorldMarker.Spawn(TileCenter(threatenedTile), "!", ThreatRed, 56f);
            if (hintTile.HasValue) hintOverlay = BuildHintOverlay(hintTile.Value);
            pulseRoutine = StartCoroutine(PulseLoop());
        }

        public void Clear()
        {
            if (pulseRoutine != null) StopCoroutine(pulseRoutine);
            pulseRoutine = null;

            arrow.Clear();
            if (threatMarker != null) Destroy(threatMarker);
            if (hintOverlay != null) Destroy(hintOverlay.gameObject);
            threatMarker = null;
            hintOverlay = null;
        }

        private void OnDisable() => Clear();

        // One loop drives both pulses so they breathe in sync.
        private IEnumerator PulseLoop()
        {
            float time = 0f;
            while (true)
            {
                time += Time.deltaTime;
                float wave = 0.5f + 0.5f * Mathf.Sin(time * 4f);

                if (threatMarker != null)
                    threatMarker.transform.localScale = Vector3.one * 0.018f * (1f + 0.15f * wave);
                if (hintOverlay != null)
                {
                    var c = HintGold;
                    c.a = Mathf.Lerp(0.25f, HintGold.a, wave);
                    hintOverlay.color = c;
                }
                yield return null;
            }
        }

        private SpriteRenderer BuildHintOverlay(Vector2Int tile)
        {
            var go = new GameObject("CorkHintOverlay");
            go.transform.SetParent(transform);
            go.transform.position = TileCenter(tile);

            var sr = go.AddComponent<SpriteRenderer>();
            sr.sprite = OverlaySpriteFactory.GetOverlaySprite();
            sr.color = HintGold;
            sr.sortingLayerName = "UIOverlay";
            sr.sortingOrder = UnderPlayerArrow;
            return sr;
        }

        private static Vector3 TileCenter(Vector2Int tile) => new(tile.x + 0.5f, tile.y + 0.5f, 0f);
    }
}
