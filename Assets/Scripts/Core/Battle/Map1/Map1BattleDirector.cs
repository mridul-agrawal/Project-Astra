using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using ProjectAstra.Core.Audio;
using ProjectAstra.Core.Combat;
using ProjectAstra.Core.Combat.Playback;
using ProjectAstra.Core.Cursor;
using ProjectAstra.Core.Dialogue;
using ProjectAstra.Core.Events;
using ProjectAstra.Core.Grid;
using ProjectAstra.Core.Turn;
using ProjectAstra.Core.UI.Overlays;
using ProjectAstra.Core.Units;
using CameraController = ProjectAstra.Core.Camera.CameraController;

namespace ProjectAstra.Core.Battle.Map1
{
    // Orchestrates Map 1's scripted beats after the Beat-0 prologue: the Turn-1
    // telegraph + guided move, the raider's bridge advance and hold, the deterministic
    // duel outcomes, and the objective flip to Pursue when the raider falls. All
    // authored numbers live in Map1Tuning; all combat determinism rides the
    // CombatScriptOverride seams. Inert on any map but Map 1.
    public class Map1BattleDirector : MonoBehaviour, IScriptedEnemyPhase
    {
        [SerializeField] private Map1Tuning tuning;
        [SerializeField] private EnemyIntentTelegraph telegraph;
        [SerializeField] private ObjectiveBannerUI objectiveBanner;
        [SerializeField] private DialogueScript bossTauntScript;

        private readonly ScriptedCombatRng scriptedRng = new();
        private readonly Map1BeatMachine beats = new();

        private TestUnit aranya;
        private TestUnit raiderUnit;
        private TestUnit bossUnit;
        private GridCursor cursor;
        private CameraController mapCamera;
        private UnitMover unitMover;

        private bool initialized;
        private bool active;               // false on direct-play test maps
        private bool raiderAdvanced;
        private bool pursueIntroPending;   // the raider fell; the next enemy phase owes the objective flip

        private void OnEnable()
        {
            EventService.Instance.Turn.RegisterPhaseStarted(OnPhaseStarted);
            EventService.Instance.BattleDialogue.Register(OnBattleDialogueEvent);
            EventService.Instance.UnitDeath.Register(OnUnitDied);
        }

        private void OnDisable()
        {
            if (EventService.Instance != null) EventService.Instance.Turn.UnregisterPhaseStarted(OnPhaseStarted);
            if (EventService.Instance != null) EventService.Instance.BattleDialogue.Unregister(OnBattleDialogueEvent);
            if (EventService.Instance != null) EventService.Instance.UnitDeath.Unregister(OnUnitDied);
            CombatScriptOverride.Clear();
        }

        // --- event handlers ---

        private void OnPhaseStarted(BattlePhase phase, int turn)
        {
            EnsureInitialized();
            if (!active) return;

            if (phase == BattlePhase.PlayerPhase && turn == 1 && beats.Current == Map1Beat.Defend)
                telegraph.Show(tuning.IntentPath, tuning.ThreatenedVillagerTile, tuning.CorkHintTile);
        }

        private void OnBattleDialogueEvent(BattleDialogueEventType type)
        {
            EnsureInitialized();
            if (!active) return;

            switch (type)
            {
                case BattleDialogueEventType.UnitSelected: ConstrainTurn1Movement(); break;
                case BattleDialogueEventType.PreCombat: ArmScriptedOutcomes(); break;
            }
        }

        private void OnUnitDied(UnitDeathEventArgs args)
        {
            if (!active) return;

            if (args.unitId == tuning.RaiderUnitId && beats.NotifyRaiderDied())
            {
                telegraph.Clear();
                pursueIntroPending = true;
            }
            else if (args.unitId == tuning.BossUnitId)
            {
                beats.NotifyBossDied();
            }
        }

        // --- Turn 1: the one move that must not go wrong ---

        private void ConstrainTurn1Movement()
        {
            if (beats.Current != Map1Beat.Defend) return;
            if (TurnManager.Instance == null || TurnManager.Instance.TurnCounter != 1) return;
            cursor.ConstrainMovementTo(new HashSet<Vector2Int>(tuning.GuidedMovePath));
        }

        // Re-armed fresh on every attack confirm; thanks to both enemies' 1-2 range
        // weapons the swing sequence is the same from any attack tile.
        private void ArmScriptedOutcomes()
        {
            var target = UnitAt(cursor.GridPosition);
            CombatScriptOverride.PreStepHook = null;

            if (target == raiderUnit)
            {
                scriptedRng.QueueOutcomes(tuning.RaiderDuelOutcomes);
            }
            else if (target == bossUnit)
            {
                scriptedRng.QueueOutcomes(tuning.BossDuelOutcomes);
                CombatScriptOverride.PreStepHook = BossDuelPreStep;
                // The signature beat always plays in full — a held skip would erase it.
                CombatAnimationSettingsRef.Current?.SetOneShotOverride(CombatAnimationSpeed.Normal);
            }
            else
            {
                scriptedRng.QueueOutcomes();
            }
        }

        // The low-HP line, right before her final swing — by this step boundary the
        // boss counter has already drained her to 2, so the caption lands at the
        // moment of truth.
        private IEnumerator BossDuelPreStep(CombatPlaybackContext ctx, int stepIndex)
        {
            if (ctx.Plan == null || stepIndex != ctx.Plan.Steps.Count - 1) yield break;

            var caption = BuildCaption(tuning.LowHpCaption, out var group);
            for (float t = 0f; t < 0.3f; t += Time.deltaTime)
            {
                group.alpha = Mathf.Clamp01(t / 0.3f);
                yield return null;
            }
            yield return new WaitForSeconds(tuning.CaptionHoldSeconds);
            for (float t = 0f; t < 0.3f; t += Time.deltaTime)
            {
                group.alpha = 1f - Mathf.Clamp01(t / 0.3f);
                yield return null;
            }
            Destroy(caption);
        }

        private static GameObject BuildCaption(string text, out CanvasGroup group)
        {
            var go = new GameObject("LowHpCaption");
            var canvas = go.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 1050;   // above the combat overlay

            var scaler = go.AddComponent<UnityEngine.UI.CanvasScaler>();
            scaler.uiScaleMode = UnityEngine.UI.CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920, 1080);

            group = go.AddComponent<CanvasGroup>();
            group.alpha = 0f;

            var band = new GameObject("Band", typeof(RectTransform));
            band.transform.SetParent(go.transform, false);
            var image = band.AddComponent<UnityEngine.UI.Image>();
            image.color = new Color(0f, 0f, 0f, 0.75f);
            image.raycastTarget = false;
            var bandRect = band.GetComponent<RectTransform>();
            bandRect.anchorMin = new Vector2(0f, 0f);
            bandRect.anchorMax = new Vector2(1f, 0f);
            bandRect.pivot = new Vector2(0.5f, 0f);
            bandRect.anchoredPosition = new Vector2(0f, 140f);
            bandRect.sizeDelta = new Vector2(0f, 86f);

            var textGo = new GameObject("Text", typeof(RectTransform));
            textGo.transform.SetParent(band.transform, false);
            var tmp = textGo.AddComponent<TMPro.TextMeshProUGUI>();
            tmp.text = text;
            tmp.fontSize = 38f;
            tmp.fontStyle = TMPro.FontStyles.Italic;
            tmp.alignment = TMPro.TextAlignmentOptions.Center;
            tmp.color = new Color(0.95f, 0.92f, 0.85f, 1f);
            tmp.raycastTarget = false;
            var textRect = textGo.GetComponent<RectTransform>();
            textRect.anchorMin = Vector2.zero;
            textRect.anchorMax = Vector2.one;
            textRect.offsetMin = Vector2.zero;
            textRect.offsetMax = Vector2.zero;
            return go;
        }

        // --- scripted enemy phases ---

        public bool TryBuildPhaseScript(BattlePhase phase, int turn, out IEnumerator routine)
        {
            routine = null;
            EnsureInitialized();
            if (!active || phase != BattlePhase.EnemyPhase) return false;

            if (beats.Current == Map1Beat.Defend && RaiderAlive())
            {
                routine = raiderAdvanced ? RaiderHoldsTheLine() : RaiderAdvance();
                return true;
            }
            if (pursueIntroPending)
            {
                routine = PursueIntro();
                return true;
            }
            return false;   // Pursue beat with nothing owed: the boss just looms (default delay)
        }

        // EP1: the raider charges the bridge — blocked by the cork, in range, telegraph narrows.
        private IEnumerator RaiderAdvance()
        {
            raiderAdvanced = true;
            AudioManager.Instance?.Play(SoundId.RakshasaRoar);
            yield return MoveUnit(raiderUnit, tuning.RaiderAdvancePath);
            telegraph.Show(tuning.IntentPathAfterAdvance, tuning.ThreatenedVillagerTile, tuning.CorkHintTile);
            yield return new WaitForSeconds(0.4f);
        }

        // EP2+: the raider holds the bridge with a feint — it must never actually attack
        // (a hit would chip Aranya and break the 2-HP equation; dying to a scripted
        // counter would break Turn 3).
        private IEnumerator RaiderHoldsTheLine()
        {
            yield return new WaitForSeconds(tuning.RaiderFeintHoldSeconds);
            AudioManager.Instance?.Play(SoundId.RakshasaRoar);
            yield return Lunge(raiderUnit, aranya != null ? aranya.gridPosition : tuning.CorkHintTile);
            yield return new WaitForSeconds(0.3f);
        }

        // EP3: the objective flips — frame the boss, let it taunt, banner the new goal.
        private IEnumerator PursueIntro()
        {
            pursueIntroPending = false;

            if (mapCamera != null)
            {
                mapCamera.BeginCinematic();
                yield return mapCamera.CinematicFrameTo(tuning.BossFrameTile, tuning.GameplayOrthoSize, tuning.CameraPanSeconds);
            }

            AudioManager.Instance?.Play(SoundId.RakshasaRoar);
            if (mapCamera != null) yield return mapCamera.Shake(0.12f, 0.25f);
            yield return PlayBossTaunt();

            if (objectiveBanner != null)
                yield return objectiveBanner.Show("OBJECTIVE CHANGED", "Slay the Rakshasa champion", 1.6f);

            if (mapCamera != null)
                mapCamera.EndCinematic(aranya != null ? aranya.gridPosition : tuning.BossFrameTile);
        }

        private IEnumerator PlayBossTaunt()
        {
            if (bossTauntScript == null || DialogueService.Instance == null) yield break;

            bool done = false;
            DialogueService.Instance.Play(bossTauntScript, DialogueTriggeringContext.BattleMap, () => done = true);
            while (!done) yield return null;
        }

        // --- init / lookup ---

        // Deferred to the first battle event: by then the bootstrapper has loaded the
        // map, so the Map-1 check and unit resolution are reliable.
        private void EnsureInitialized()
        {
            if (initialized) return;
            initialized = true;

            active = IsMap1Loaded();
            if (!active) return;

            ResolveActors();
            cursor = FindFirstObjectByType<GridCursor>();
            mapCamera = FindFirstObjectByType<CameraController>();
            unitMover = FindFirstObjectByType<UnitMover>();
            InstallCombatScript();
        }

        // The map-wide deterministic default (exhausted queue = every swing hits, never
        // crits) plus threshold-based level-up gains — zero dice anywhere on Map 1.
        private void InstallCombatScript()
        {
            CombatScriptOverride.Rng = scriptedRng;
            CombatScriptOverride.GrowthRoll = growthRate => growthRate >= tuning.LevelUpGrowthThreshold;
        }

        private void ResolveActors()
        {
            foreach (var unit in FindObjectsByType<TestUnit>(FindObjectsSortMode.None))
            {
                if (unit.faction == Faction.Player) aranya = unit;
                else if (unit.UnitInstance?.Definition?.UnitId == tuning.RaiderUnitId) raiderUnit = unit;
                else if (unit.UnitInstance?.Definition?.UnitId == tuning.BossUnitId) bossUnit = unit;
            }
        }

        private bool RaiderAlive() => raiderUnit != null && raiderUnit.gameObject.activeInHierarchy;

        private static TestUnit UnitAt(Vector2Int tile)
        {
            foreach (var unit in FindObjectsByType<TestUnit>(FindObjectsSortMode.None))
                if (unit.gridPosition == tile) return unit;
            return null;
        }

        private IEnumerator MoveUnit(TestUnit unit, IReadOnlyList<Vector2Int> path)
        {
            if (unit == null || unitMover == null) yield break;
            bool done = false;
            unitMover.MoveAlongPath(unit, new List<Vector2Int>(path), () => done = true);
            while (!done) yield return null;
        }

        // Token-theater feint, same shape as the prologue's kill lunge.
        private static IEnumerator Lunge(TestUnit unit, Vector2Int towardTile)
        {
            if (unit == null) yield break;
            Vector3 home = unit.transform.position;
            Vector3 strike = Vector3.Lerp(home, new Vector3(towardTile.x + 0.5f, towardTile.y + 0.5f, 0f), 0.4f);
            yield return MoveOver(unit.transform, home, strike, 0.09f);
            yield return MoveOver(unit.transform, strike, home, 0.13f);
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

        private static bool IsMap1Loaded()
        {
            var mapRenderer = FindFirstObjectByType<MapRenderer>();
            return mapRenderer != null
                && mapRenderer.CurrentMap != null
                && mapRenderer.CurrentMap.Id == MapId.Map1_BridgeAtSuvarnapur;
        }
    }
}
