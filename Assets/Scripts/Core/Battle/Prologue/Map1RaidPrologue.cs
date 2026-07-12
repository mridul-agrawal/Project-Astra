using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using ProjectAstra.Core.Audio;
using ProjectAstra.Core.Cursor;
using ProjectAstra.Core.Grid;
using ProjectAstra.Core.Input;
using ProjectAstra.Core.Turn;
using ProjectAstra.Core.Units;
using ProjectAstra.Core.UI.BattleMap;
using ProjectAstra.Core.Dialogue;
using CursorMode = ProjectAstra.Core.Cursor.CursorMode;
using CameraController = ProjectAstra.Core.Camera.CameraController;

namespace ProjectAstra.Core.Battle.Prologue
{
    // Beat 0 for Map 1 ("The Bridge at Suvarnapur"): an on-map scripted raid that plays before
    // the player's first turn. It's a top-to-bottom chase — the Rakshasa own the north bank, cut
    // down a villager up top, and the survivors flee south across the single footbridge to safety;
    // then Aranya arrives on the south bank to defend them. The raider never crosses; it holds the
    // line at its Turn-1 tile. Teaches the stakes by showing, not telling. Hardcoded to this map.
    //
    // Map sprites are static, so motion is "token theater": pieces slide, the kill is a lunge +
    // flash + fade. The three character actions funnel through WalkUnit / LungeAt / PlayDeath so
    // real animation can replace them later without touching the choreography. (Camera/audio later.)
    public class Map1RaidPrologue : MonoBehaviour, IBattlePrologue
    {
        [Tooltip("Aranya's arrival vow, played through the shared dialogue view when she reaches the south bank.")]
        [SerializeField] private DialogueScript _aranyaArrivalScript;

        // North bank (high y); the only crossing is the footbridge at (7,5).
        private static readonly Vector2Int RaiderHome = new(7, 7);   // raider's Turn-1 tile (holds the line)
        private static readonly Vector2Int BossTile = new(7, 9);
        private static readonly Vector2Int VictimTile = new(7, 6);   // north foot of the bridge — caught before crossing
        private static readonly Vector2Int AranyaHome = new(10, 2);
        private static readonly Vector2Int AranyaEntrance = new(10, 0);

        private static readonly Vector2Int[] AranyaWalkOn =
            { new(10, 0), new(9, 0), new(9, 1), new(9, 2), new(10, 2) }; // skirts the villager at (10,1)

        // Each surviving villager is posed up north, then flees back to its own spawn (its Turn-1
        // tile) across the bridge. Keyed by spawn so each returns exactly where it belongs.
        private static readonly Dictionary<Vector2Int, FleePlan> FleePlans = new()
        {
            [new(7, 2)] = new FleePlan(new(6, 6),
                new[] { new Vector2Int(6, 6), new(7, 6), new(7, 5), new(7, 4), new(7, 3), new(7, 2) }),
            [new(4, 1)] = new FleePlan(new(8, 7),
                new[] { new Vector2Int(8, 7), new(8, 6), new(7, 6), new(7, 5), new(7, 4), new(7, 3),
                        new(6, 3), new(5, 3), new(5, 2), new(5, 1), new(4, 1) }),
            [new(10, 1)] = new FleePlan(new(8, 8),
                new[] { new Vector2Int(8, 8), new(8, 7), new(8, 6), new(7, 6), new(7, 5), new(7, 4),
                        new(7, 3), new(8, 3), new(9, 3), new(9, 2), new(9, 1), new(10, 1) }),
        };

        private readonly struct FleePlan
        {
            public readonly Vector2Int Start;
            public readonly Vector2Int[] Path;
            public FleePlan(Vector2Int start, Vector2Int[] path) { Start = start; Path = path; }
        }

        // Cinematic camera: open zoomed-in tight on the kill up north, then zoom back out while
        // panning down to the south bank as the chase widens — the reveal IS the chase opening up.
        private static readonly Vector2Int NorthFrame = new(7, 7);
        private static readonly Vector2Int SouthFrame = new(7, 4);
        private const float NorthZoomOrtho = 2.6f;        // tight on the raider + victim
        private const float GameplayOrtho = 4.21875f;     // the map's pixel-perfect size (full south view)

        private const float WalkTilesPerSecond = 4f;
        private const float FleeStaggerSeconds = 0.45f;
        private const float PanDownSeconds = 3f;

        private const float LetterboxBarHeight = 110f;     // per 1080-tall reference
        private const float LetterboxFadeSeconds = 0.4f;
        private const float SkipHintDelaySeconds = 1f;
        private const float SkipHintFadeSeconds = 0.4f;

        private static readonly Color DangerRed = new(0.95f, 0.27f, 0.22f, 1f);

        private TestUnit aranya;
        private TestUnit raider;
        private readonly List<(TestUnit unit, Vector2Int home)> posedVillagers = new();
        private readonly List<GameObject> cinematicProps = new();
        private GridCursor cursor;
        private CameraController cinematicCamera;
        private bool skipRequested;
        private bool choreographyDone;

        // Runs the choreography as a child coroutine so the skip key can cut it short at any
        // point; both endings funnel through FinalizeToGameplayState — the single source of
        // the terminal state the player's Turn 1 starts from.
        public IEnumerator Play()
        {
            if (!IsMap1Loaded()) yield break;

            ResolveActors(out aranya, out raider, out var villagers, out var villagerSprite);
            cursor = FindFirstObjectByType<GridCursor>();
            cinematicCamera = FindFirstObjectByType<CameraController>();
            cursor?.SetMode(CursorMode.Locked);

            skipRequested = false;
            choreographyDone = false;
            if (InputManager.Instance != null) InputManager.Instance.OnSkipDialogue += RequestSkip;

            StartCoroutine(ShowSkipHintAfterDelay());
            StartCoroutine(RunChoreography(villagers, villagerSprite));
            while (!choreographyDone && !skipRequested) yield return null;

            if (InputManager.Instance != null) InputManager.Instance.OnSkipDialogue -= RequestSkip;
            FinalizeToGameplayState();
        }

        private void RequestSkip() => skipRequested = true;

        private void OnDestroy()
        {
            if (InputManager.Instance != null) InputManager.Instance.OnSkipDialogue -= RequestSkip;
        }

        private IEnumerator RunChoreography(List<TestUnit> villagers, SpriteRenderer villagerSprite)
        {
            if (cinematicCamera != null)
            {
                cinematicCamera.BeginCinematic();
                cinematicCamera.SnapCinematicFrame(NorthFrame, NorthZoomOrtho);  // open tight on the raid up north
            }

            // TODO(audio): raid tension music here once a music clip is wired (none exists yet).

            GameObject letterbox = BuildLetterbox(out var topBar, out var bottomBar);
            cinematicProps.Add(letterbox);
            StartCoroutine(AnimateBars(topBar, bottomBar, 0f, LetterboxBarHeight, LetterboxFadeSeconds));

            // Pose: Aranya waits off the south edge; the villagers are caught up north; one of them
            // (a dedicated extra) is at the bridge's north foot, about to be cut down.
            var aranyaSprite = SpriteOf(aranya);
            if (aranyaSprite != null) aranyaSprite.enabled = false;
            PlaceAt(aranya, AranyaEntrance);

            var flees = PoseVillagersForFlight(villagers);
            var victim = SpawnVictimToken(VictimTile, villagerSprite);
            if (victim != null) cinematicProps.Add(victim);

            // Telegraph the target — a pulsing "!" over the doomed villager — under the raid's roar.
            GameObject victimAlert = victim != null ? WorldMarker.Spawn(ToWorld(VictimTile), "!", DangerRed, 56f) : null;
            if (victimAlert != null) cinematicProps.Add(victimAlert);
            Coroutine alertPulse = victimAlert != null ? StartCoroutine(WorldMarker.Pulse(victimAlert.transform)) : null;
            AudioManager.Instance?.Play(SoundId.RakshasaRoar);

            yield return Hold(0.9f);

            // The raid: the raider strikes down the trapped villager at the bridge's north foot.
            if (raider != null) yield return LungeAt(raider, VictimTile);
            if (alertPulse != null) StopCoroutine(alertPulse);
            if (victimAlert != null) Destroy(victimAlert);
            if (cinematicCamera != null) StartCoroutine(cinematicCamera.Shake(0.18f, 0.28f));
            AudioManager.Instance?.Play(SoundId.HitCrit);         // the killing blow
            AudioManager.Instance?.Play(SoundId.VillagerScream);  // a life cut short
            yield return PlayDeath(victim);
            yield return Hold(0.6f);

            // The chase: survivors flee south across the bridge, past the raider, scattering home —
            // and the camera zooms back out and pans down with them, the village opening into view.
            if (cinematicCamera != null) StartCoroutine(cinematicCamera.CinematicFrameTo(SouthFrame, GameplayOrtho, PanDownSeconds));
            yield return FleeVillagers(flees);
            yield return Hold(0.5f);

            // Their defender arrives on the south bank; the raider holds the north line.
            if (aranyaSprite != null) aranyaSprite.enabled = true;
            if (aranya != null) yield return WalkUnit(aranya, AranyaWalkOn, WalkTilesPerSecond);
            yield return Hold(0.4f);

            // Her vow, before control passes — Confirm advances (the cursor is Locked, so
            // nothing else consumes it).
            if (_aranyaArrivalScript != null && DialogueService.Instance != null)
                yield return DialogueService.Instance.PlayRoutine(_aranyaArrivalScript, DialogueTriggeringContext.Cutscene);

            yield return AnimateBars(topBar, bottomBar, LetterboxBarHeight, 0f, LetterboxFadeSeconds);
            choreographyDone = true;
        }

        // The one terminal state both endings (natural finish and skip) land on: props gone,
        // everyone on their Turn-1 tile, gameplay camera restored, cursor freed.
        private void FinalizeToGameplayState()
        {
            StopAllCoroutines();
            DialogueService.Instance?.Skip();

            foreach (var prop in cinematicProps)
                if (prop != null) Destroy(prop);
            cinematicProps.Clear();

            var aranyaSprite = SpriteOf(aranya);
            if (aranyaSprite != null) aranyaSprite.enabled = true;
            SnapTo(aranya, AranyaHome);
            SnapTo(raider, RaiderHome);
            foreach (var (unit, home) in posedVillagers) SnapTo(unit, home);
            posedVillagers.Clear();

            cinematicCamera?.EndCinematic(AranyaHome);
            cursor?.SetMode(CursorMode.Free);
        }

        // --- actors ---

        private static void ResolveActors(out TestUnit aranya, out TestUnit raider,
            out List<TestUnit> villagers, out SpriteRenderer villagerSprite)
        {
            aranya = null;
            raider = null;
            villagers = new List<TestUnit>();
            villagerSprite = null;

            foreach (var unit in FindObjectsByType<TestUnit>(FindObjectsSortMode.None))
            {
                if (unit.faction == Faction.Player) aranya = unit;
                else if (unit.faction == Faction.Enemy && unit.gridPosition == RaiderHome) raider = unit;
                else if (unit.faction == Faction.Allied)
                {
                    villagers.Add(unit);
                    if (villagerSprite == null) villagerSprite = SpriteOf(unit);
                }
            }
        }

        // Move each villager to its authored north start; return the (unit, flee-path) pairs to run.
        // Remembers each one's Turn-1 tile so a skip can snap it straight home.
        private List<(TestUnit unit, Vector2Int[] path)> PoseVillagersForFlight(List<TestUnit> villagers)
        {
            var flees = new List<(TestUnit, Vector2Int[])>();
            foreach (var villager in villagers)
            {
                if (!FleePlans.TryGetValue(villager.gridPosition, out var plan)) continue;
                posedVillagers.Add((villager, villager.gridPosition));
                PlaceAt(villager, plan.Start);
                flees.Add((villager, plan.Path));
            }
            return flees;
        }

        // A pure visual stand-in for the slain villager — not a TestUnit, so no death events reach
        // the registry/victory watcher. Clones a living villager's sprite so it matches on-map size.
        private static GameObject SpawnVictimToken(Vector2Int tile, SpriteRenderer template)
        {
            if (template == null) return null;

            var clone = Instantiate(template);
            clone.name = "DoomedVillager";
            clone.transform.SetParent(null, false);
            clone.transform.localScale = template.transform.lossyScale;
            clone.transform.position = ToWorld(tile);
            return clone.gameObject;
        }

        // --- character actions (swap these for real animation when art exists) ---

        // Runs the flee paths with a slight stagger so the survivors cross the one-tile bridge
        // single-file (the chokepoint) rather than piling onto the same tile.
        private IEnumerator FleeVillagers(List<(TestUnit unit, Vector2Int[] path)> flees)
        {
            int running = flees.Count;
            foreach (var (unit, path) in flees)
            {
                StartCoroutine(RunThenSignal(WalkUnit(unit, path, WalkTilesPerSecond), () => running--));
                yield return new WaitForSeconds(FleeStaggerSeconds);
            }
            while (running > 0) yield return null;
        }

        // One continuous glide along the whole path with a single ease-in/out — easing each tile
        // separately would decelerate the unit to a near-stop at every corner.
        private static IEnumerator WalkUnit(TestUnit unit, Vector2Int[] path, float tilesPerSecond)
        {
            int segments = path.Length - 1;
            if (segments < 1) yield break;

            float totalDuration = segments / tilesPerSecond;
            float elapsed = 0f;
            while (elapsed < totalDuration)
            {
                elapsed += Time.deltaTime;
                float progress = Mathf.SmoothStep(0f, 1f, Mathf.Clamp01(elapsed / totalDuration));
                float along = progress * segments;                        // tiles travelled along the path
                int seg = Mathf.Min((int)along, segments - 1);
                unit.transform.position = Vector3.Lerp(ToWorld(path[seg]), ToWorld(path[seg + 1]), along - seg);
                unit.gridPosition = path[Mathf.Clamp(Mathf.RoundToInt(along), 0, segments)];
                yield return null;
            }
            unit.gridPosition = path[segments];
            unit.SnapToGridPosition();
        }

        private static IEnumerator LungeAt(TestUnit attacker, Vector2Int targetTile)
        {
            Vector3 home = attacker.transform.position;
            Vector3 strike = Vector3.Lerp(home, ToWorld(targetTile), 0.5f);
            yield return MoveOver(attacker.transform, home, strike, 0.09f);
            yield return MoveOver(attacker.transform, strike, home, 0.13f);
        }

        private IEnumerator PlayDeath(GameObject victim)
        {
            if (victim == null) yield break;
            var sprite = victim.GetComponent<SpriteRenderer>();

            GameObject mark = WorldMarker.Spawn(victim.transform.position, "X", DangerRed, 56f);
            if (mark != null)
            {
                cinematicProps.Add(mark);
                StartCoroutine(WorldMarker.RiseAndFade(mark, 0.7f, 0.5f));
            }

            yield return HitFlashEffect.Flash(sprite, 0.1f, DangerRed);   // a red death flash
            StartCoroutine(SlideDown(victim.transform, 0.3f, 0.45f));
            yield return SpriteFader.FadeOut(sprite, 0.45f);
            Destroy(victim);
        }

        // --- small motion helpers ---

        private IEnumerator RunThenSignal(IEnumerator routine, Action onDone)
        {
            yield return StartCoroutine(routine);
            onDone();
        }

        private static IEnumerator MoveOver(Transform t, Vector3 from, Vector3 to, float duration)
        {
            float elapsed = 0f;
            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                t.position = Vector3.Lerp(from, to, Mathf.Clamp01(elapsed / duration));
                yield return null;
            }
            t.position = to;
        }

        private static IEnumerator SlideDown(Transform t, float distance, float duration)
        {
            Vector3 from = t.position;
            yield return MoveOver(t, from, from + Vector3.down * distance, duration);
        }

        private static IEnumerator Hold(float seconds) { yield return new WaitForSeconds(seconds); }

        // --- skip hint ---

        // "[M] Skip" appears after a beat so the cinematic opens clean.
        private IEnumerator ShowSkipHintAfterDelay()
        {
            yield return Hold(SkipHintDelaySeconds);

            var hint = BuildSkipHint(out var group);
            cinematicProps.Add(hint);

            float elapsed = 0f;
            while (elapsed < SkipHintFadeSeconds && group != null)
            {
                elapsed += Time.deltaTime;
                group.alpha = Mathf.Clamp01(elapsed / SkipHintFadeSeconds);
                yield return null;
            }
        }

        private static GameObject BuildSkipHint(out CanvasGroup group)
        {
            var go = new GameObject("SkipHint");
            var canvas = go.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 1001;   // above the letterbox bars

            var scaler = go.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920, 1080);

            group = go.AddComponent<CanvasGroup>();
            group.alpha = 0f;

            var textGo = new GameObject("Text", typeof(RectTransform));
            textGo.transform.SetParent(go.transform, false);
            var tmp = textGo.AddComponent<TextMeshProUGUI>();
            tmp.text = "[M] Skip";
            tmp.fontSize = 28f;
            tmp.alignment = TextAlignmentOptions.BottomRight;
            tmp.color = new Color(1f, 1f, 1f, 0.75f);
            tmp.raycastTarget = false;

            var rect = textGo.GetComponent<RectTransform>();
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = new Vector2(0f, 28f);     // sits on the bottom letterbox bar
            rect.offsetMax = new Vector2(-36f, 0f);
            return go;
        }

        // --- letterbox (cinematic black bars) ---
        // ScreenSpaceOverlay so the bars always sit above the map and units; they stay
        // screen-fixed while the camera zooms and pans beneath them.

        private static GameObject BuildLetterbox(out RectTransform topBar, out RectTransform bottomBar)
        {
            var go = new GameObject("CinematicLetterbox");
            var canvas = go.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;  // always on top of map/units
            canvas.sortingOrder = 1000;                          // below the ScreenFader's fade

            var scaler = go.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920, 1080);

            topBar = MakeBar(go.transform, atTop: true);
            bottomBar = MakeBar(go.transform, atTop: false);
            return go;
        }

        private static RectTransform MakeBar(Transform parent, bool atTop)
        {
            var bar = new GameObject(atTop ? "TopBar" : "BottomBar", typeof(RectTransform));
            bar.transform.SetParent(parent, false);

            var image = bar.AddComponent<Image>();
            image.color = Color.black;
            image.raycastTarget = false;

            var rect = bar.GetComponent<RectTransform>();
            rect.anchorMin = new Vector2(0f, atTop ? 1f : 0f);
            rect.anchorMax = new Vector2(1f, atTop ? 1f : 0f);
            rect.pivot = new Vector2(0.5f, atTop ? 1f : 0f);
            rect.sizeDelta = Vector2.zero;
            return rect;
        }

        private static IEnumerator AnimateBars(RectTransform top, RectTransform bottom,
            float fromHeight, float toHeight, float duration)
        {
            float elapsed = 0f;
            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                float h = Mathf.Lerp(fromHeight, toHeight, Mathf.SmoothStep(0f, 1f, elapsed / duration));
                SetBarHeight(top, h);
                SetBarHeight(bottom, h);
                yield return null;
            }
            SetBarHeight(top, toHeight);
            SetBarHeight(bottom, toHeight);
        }

        private static void SetBarHeight(RectTransform bar, float height)
        {
            if (bar != null) bar.sizeDelta = new Vector2(bar.sizeDelta.x, height);
        }

        // --- placement / lookup ---

        private static void PlaceAt(TestUnit unit, Vector2Int tile)
        {
            if (unit == null) return;
            unit.gridPosition = tile;
            unit.SnapToGridPosition();
        }

        private static void SnapTo(TestUnit unit, Vector2Int tile) => PlaceAt(unit, tile);

        private static SpriteRenderer SpriteOf(TestUnit unit) =>
            unit != null ? unit.GetComponentInChildren<SpriteRenderer>() : null;

        private static Vector3 ToWorld(Vector2Int tile) => new(tile.x + 0.5f, tile.y + 0.5f, 0f);

        // Only the real Map 1 gets the raid — editor direct-play with the test map skips it.
        private static bool IsMap1Loaded()
        {
            var mapRenderer = FindFirstObjectByType<MapRenderer>();
            return mapRenderer != null
                && mapRenderer.CurrentMap != null
                && mapRenderer.CurrentMap.MapId == "map1_bridge";
        }
    }
}
