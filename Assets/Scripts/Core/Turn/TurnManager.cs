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

        [SerializeField] private bool _hasAllies;
        // Placeholder: how long an AI phase visibly lingers before auto-ending. Replace once real AI exists.
        [SerializeField] private float _aiPhaseDelaySeconds = 1f;

        private BattlePhaseManager _phaseManager;
        private UnitRegistry _unitRegistry;
        private int _turnCounter;
        private Coroutine _aiPhaseCoroutine;
        private IScriptedEnemyPhase _scriptedPhase;

        public BattlePhase CurrentPhase => _phaseManager?.CurrentPhase ?? BattlePhase.PlayerPhase;
        public int TurnCounter => _turnCounter;
        public UnitRegistry UnitRegistry => _unitRegistry;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;

            _unitRegistry = new UnitRegistry();
            _phaseManager = new BattlePhaseManager(_hasAllies);
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
            if (_turnCounter > 0) return;

            _turnCounter = 1;
            _phaseManager.SetHasAllies(_hasAllies || _unitRegistry.HasUnitsOfFaction(Faction.Allied));
            _phaseManager.Reset();
            RegisterSceneUnits();
            _scriptedPhase = FindScriptedPhase();

            AudioManager.Instance?.PlayMusic(SoundId.MusicMap);
            AudioManager.Instance?.PlayAmbient(SoundId.AmbientWind);

            var prologue = FindPrologue();
            if (prologue != null)
                StartCoroutine(RunPrologueThenBeginPhase(prologue));
            else
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

            var endingPhase = _phaseManager.CurrentPhase;
            _unitRegistry.MarkAllActed(PhaseToFaction(endingPhase));
            EventService.Instance?.RaisePhaseEnded(endingPhase);

            _phaseManager.AdvancePhase();
            AdvanceTurnIfNewRound();
            BeginPhase();
        }

        // Player-facing "end my phase" entry point — wired to the pause menu's End Turn button.
        // No-ops outside PlayerPhase so pressing it mid-enemy-phase (the pause menu is reachable
        // from any phase) can't accidentally cut someone else's phase short.
        public void EndPlayerPhase()
        {
            if (_phaseManager.CurrentPhase != BattlePhase.PlayerPhase) return;
            EndCurrentPhase();
        }

        public void CheckAutoEndPlayerPhase()
        {
            if (_phaseManager.CurrentPhase != BattlePhase.PlayerPhase) return;
            if (_unitRegistry.AllDone(Faction.Player))
                EndCurrentPhase();
        }

        private void BeginPhase()
        {
            var phase = _phaseManager.CurrentPhase;
            _unitRegistry.ResetPhaseFlags(PhaseToFaction(phase));
            EventService.Instance?.RaisePhaseStarted(phase, _turnCounter);

            if (phase != BattlePhase.PlayerPhase)
                _aiPhaseCoroutine = StartCoroutine(RunAIPhase(phase));
        }

        // Scripted battles choreograph their AI phases; otherwise the placeholder delay keeps
        // the phase visible. EndCurrentPhase auto-marks any units that didn't act either way.
        private IEnumerator RunAIPhase(BattlePhase phase)
        {
            if (_scriptedPhase != null && _scriptedPhase.TryBuildPhaseScript(phase, _turnCounter, out var routine))
                yield return StartCoroutine(routine);
            else
                yield return new WaitForSeconds(_aiPhaseDelaySeconds);
            EndCurrentPhase();
        }

        private void RegisterSceneUnits()
        {
            foreach (var unit in FindObjectsByType<TestUnit>(FindObjectsSortMode.None))
            {
                if (_unitRegistry.GetFaction(unit) == null)
                    _unitRegistry.Register(unit, unit.faction);
            }
        }

        private void StopAIPhaseCoroutine()
        {
            if (_aiPhaseCoroutine == null) return;
            StopCoroutine(_aiPhaseCoroutine);
            _aiPhaseCoroutine = null;
        }

        private void AdvanceTurnIfNewRound()
        {
            if (_phaseManager.CurrentPhase != BattlePhase.PlayerPhase) return;
            _turnCounter++;
            EventService.Instance?.RaiseTurnAdvanced(_turnCounter);
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
