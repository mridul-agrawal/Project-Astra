using System.Collections;
using UnityEngine;
using ProjectAstra.Core.Audio;
using ProjectAstra.Core.Events;
using ProjectAstra.Core.State;
using ProjectAstra.Core.Units;

namespace ProjectAstra.Core.Turn
{
    // Singleton conductor for a battle's turn cycle. Owns the phase manager, the unit registry,
    // and the AI auto-phase timer; broadcasts phase/turn events through EventService. Started by
    // entering the BattleMap game state and lives until the scene unloads.
    public class TurnManager : MonoBehaviour
    {
        public static TurnManager Instance { get; private set; }

        [SerializeField] private bool hasAllies;
        // Placeholder: how long an AI phase visibly lingers before auto-ending. Replace once real AI exists.
        [SerializeField] private float aiPhaseDelaySeconds = 1f;

        private BattlePhaseManager phaseManager;
        private UnitRegistry unitRegistry;
        private int turnCounter;
        private Coroutine aiPhaseCoroutine;
        private IScriptedEnemyPhase scriptedPhase;

        public BattlePhase CurrentPhase => phaseManager?.CurrentPhase ?? BattlePhase.PlayerPhase;
        public int TurnCounter => turnCounter;
        public UnitRegistry UnitRegistry => unitRegistry;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;

            unitRegistry = new UnitRegistry();
            phaseManager = new BattlePhaseManager(hasAllies);
        }

        private void OnEnable()
        {
            EventService.Instance.SubscribeGameStateChanged(OnGameStateChanged);
        }

        private void Start()
        {
            var gsm = GameStateManager.Instance;
            if (gsm == null || gsm.CurrentState == GameState.BattleMap)
                StartBattle();
        }

        private void OnDisable()
        {
            if (EventService.Instance != null)
                EventService.Instance.UnsubscribeGameStateChanged(OnGameStateChanged);
        }

        private void OnDestroy()
        {
            if (Instance == this) Instance = null;
        }

        // Idempotent — called from both Start() (cold boot directly into BattleMap) and
        // OnGameStateChanged (entering BattleMap from a menu). The _turnCounter > 0 guard
        // makes the second call a no-op.
        public void StartBattle()
        {
            if (turnCounter > 0) return;

            turnCounter = 1;
            phaseManager.SetHasAllies(hasAllies || unitRegistry.HasUnitsOfFaction(Faction.Allied));
            phaseManager.Reset();
            RegisterSceneUnits();
            scriptedPhase = FindScriptedPhase();

            AudioManager.Instance?.PlayMusic(SoundId.MusicMap);
            AudioManager.Instance?.PlayAmbient(SoundId.AmbientWind);

            /*var prologue = FindPrologue();
            if (prologue != null)
                StartCoroutine(RunPrologueThenBeginPhase(prologue));
            else*/
                BeginPhase();
        }

        // A battle may stage a scripted opening (e.g. Map 1's raid) before handing control over.
        // We hold off BeginPhase — and therefore the player's first turn — until it completes.
        private IEnumerator RunPrologueThenBeginPhase(IBattlePrologue prologue)
        {
            yield return StartCoroutine(prologue.Play());
            BeginPhase();
        }

        private static IBattlePrologue FindPrologue()
        {
            foreach (var behaviour in FindObjectsByType<MonoBehaviour>(FindObjectsSortMode.None))
                if (behaviour is IBattlePrologue prologue)
                    return prologue;
            return null;
        }

        private static IScriptedEnemyPhase FindScriptedPhase()
        {
            foreach (var behaviour in FindObjectsByType<MonoBehaviour>(FindObjectsSortMode.None))
                if (behaviour is IScriptedEnemyPhase scripted)
                    return scripted;
            return null;
        }

        public void EndCurrentPhase()
        {
            StopAIPhaseCoroutine();

            var endingPhase = phaseManager.CurrentPhase;
            unitRegistry.MarkAllActed(PhaseToFaction(endingPhase));
            EventService.Instance?.RaisePhaseEnded(endingPhase);

            phaseManager.AdvancePhase();
            AdvanceTurnIfNewRound();
            BeginPhase();
        }

        // Player-facing "end my phase" entry point — wired to the pause menu's End Turn button.
        // No-ops outside PlayerPhase so pressing it mid-enemy-phase (the pause menu is reachable
        // from any phase) can't accidentally cut someone else's phase short.
        public void EndPlayerPhase()
        {
            if (phaseManager.CurrentPhase != BattlePhase.PlayerPhase) return;
            EndCurrentPhase();
        }

        public void CheckAutoEndPlayerPhase()
        {
            if (phaseManager.CurrentPhase != BattlePhase.PlayerPhase) return;
            if (unitRegistry.AllDone(Faction.Player))
                EndCurrentPhase();
        }

        private void BeginPhase()
        {
            var phase = phaseManager.CurrentPhase;
            var faction = PhaseToFaction(phase);
            unitRegistry.ResetPhaseFlags(faction);
            unitRegistry.ClearActedVisualForOthers(faction);   // enemies/allies look normal on your turn
            EventService.Instance?.RaisePhaseStarted(phase, turnCounter);

            if (phase != BattlePhase.PlayerPhase)
                aiPhaseCoroutine = StartCoroutine(RunAIPhase(phase));
        }

        // Scripted battles choreograph their AI phases; otherwise the placeholder delay keeps
        // the phase visible. EndCurrentPhase auto-marks any units that didn't act either way.
        private IEnumerator RunAIPhase(BattlePhase phase)
        {
            if (scriptedPhase != null && scriptedPhase.TryBuildPhaseScript(phase, turnCounter, out var routine))
                yield return StartCoroutine(routine);
            else
                yield return new WaitForSeconds(aiPhaseDelaySeconds);
            EndCurrentPhase();
        }

        private void RegisterSceneUnits()
        {
            foreach (var unit in FindObjectsByType<TestUnit>(FindObjectsSortMode.None))
            {
                if (unitRegistry.GetFaction(unit) == null)
                    unitRegistry.Register(unit, unit.faction);
            }
        }

        private void StopAIPhaseCoroutine()
        {
            if (aiPhaseCoroutine == null) return;
            StopCoroutine(aiPhaseCoroutine);
            aiPhaseCoroutine = null;
        }

        private void AdvanceTurnIfNewRound()
        {
            if (phaseManager.CurrentPhase != BattlePhase.PlayerPhase) return;
            turnCounter++;
            EventService.Instance?.RaiseTurnAdvanced(turnCounter);
        }

        internal static Faction PhaseToFaction(BattlePhase phase) => phase switch
        {
            BattlePhase.PlayerPhase => Faction.Player,
            BattlePhase.EnemyPhase  => Faction.Enemy,
            BattlePhase.AlliedPhase => Faction.Allied,
            _                       => Faction.Player,
        };

        private void OnGameStateChanged(StateChangeArgs args)
        {
            if (args.NewState == GameState.BattleMap && args.PreviousState != GameState.BattleMapPaused)
                StartBattle();
        }
    }
}
