using System.Collections;
using UnityEngine;
using ProjectAstra.Core.State;
using ProjectAstra.Core.Units;

namespace ProjectAstra.Core.Turn
{
    // Singleton conductor for a battle's turn cycle. Owns the phase manager, the unit registry,
    // and the AI auto-phase timer; broadcasts phase/turn events on TurnEventChannel. Started by
    // entering the BattleMap game state and lives until the scene unloads.
    public class TurnManager : MonoBehaviour
    {
        public static TurnManager Instance { get; private set; }

        [SerializeField] private TurnEventChannel _turnEventChannel;
        [SerializeField] private GameStateEventChannel _stateChangedChannel;
        [SerializeField] private bool _hasAllies;
        // Placeholder: how long an AI phase visibly lingers before auto-ending. Replace once real AI exists.
        [SerializeField] private float _aiPhaseDelaySeconds = 1f;

        private BattlePhaseManager _phaseManager;
        private UnitRegistry _unitRegistry;
        private int _turnCounter;
        private Coroutine _aiPhaseCoroutine;

        public BattlePhase CurrentPhase => _phaseManager?.CurrentPhase ?? BattlePhase.PlayerPhase;
        public int TurnCounter => _turnCounter;
        public UnitRegistry UnitRegistry => _unitRegistry;
        public TurnEventChannel TurnEventChannel => _turnEventChannel;

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
            if (_stateChangedChannel != null)
                _stateChangedChannel.Register(OnGameStateChanged);
        }

        private void Start()
        {
            var gsm = GameStateManager.Instance;
            if (gsm == null || gsm.CurrentState == GameState.BattleMap)
                StartBattle();
        }

        private void OnDisable()
        {
            if (_stateChangedChannel != null)
                _stateChangedChannel.Unregister(OnGameStateChanged);
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
            BeginPhase();
        }

        public void EndCurrentPhase()
        {
            StopAIPhaseCoroutine();

            var endingPhase = _phaseManager.CurrentPhase;
            _unitRegistry.MarkAllActed(PhaseToFaction(endingPhase));
            _turnEventChannel?.RaisePhaseEnded(endingPhase);

            _phaseManager.AdvancePhase();
            AdvanceTurnIfNewRound();
            BeginPhase();
        }

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
            _turnEventChannel?.RaisePhaseStarted(phase, _turnCounter);

            if (phase != BattlePhase.PlayerPhase)
                _aiPhaseCoroutine = StartCoroutine(RunAIPhase());
        }

        // Placeholder AI: waits the configured delay so the phase reads visibly, then ends.
        // EndCurrentPhase auto-marks any units that didn't act, so no per-unit logic is needed here yet.
        private IEnumerator RunAIPhase()
        {
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
            _turnEventChannel?.RaiseTurnAdvanced(_turnCounter);
        }

        internal static Faction PhaseToFaction(BattlePhase phase) => phase switch
        {
            BattlePhase.PlayerPhase => Faction.Player,
            BattlePhase.EnemyPhase  => Faction.Enemy,
            BattlePhase.AlliedPhase => Faction.Allied,
            _                       => Faction.Player,
        };

        private void OnGameStateChanged(GameStateEventChannel.StateChangeArgs args)
        {
            if (args.NewState == GameState.BattleMap && args.PreviousState != GameState.BattleMapPaused)
                StartBattle();
        }
    }
}
