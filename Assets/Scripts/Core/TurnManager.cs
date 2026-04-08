using System.Collections;
using UnityEngine;

namespace ProjectAstra.Core
{
    public class TurnManager : MonoBehaviour
    {
        public static TurnManager Instance { get; private set; }

        [SerializeField] private TurnEventChannel _turnEventChannel;
        [SerializeField] private GameStateEventChannel _stateChangedChannel;
        [SerializeField] private bool _hasAllies;

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

        #region Battle lifecycle

        public void StartBattle()
        {
            if (_turnCounter > 0) return;

            _turnCounter = 1;
            _phaseManager.SetHasAllies(_hasAllies || _unitRegistry.HasUnitsOfFaction(Faction.Allied));
            _phaseManager.Reset();
            RegisterSceneUnits();
            BeginPhase();
        }

        private void RegisterSceneUnits()
        {
            foreach (var unit in FindObjectsByType<TestUnit>(FindObjectsSortMode.None))
            {
                if (_unitRegistry.GetFaction(unit) == null)
                    _unitRegistry.Register(unit, unit.faction);
            }
        }

        #endregion

        #region Phase management

        private void BeginPhase()
        {
            var phase = _phaseManager.CurrentPhase;
            var faction = PhaseToFaction(phase);
            _unitRegistry.ResetPhaseFlags(faction);

            _turnEventChannel?.RaisePhaseStarted(phase, _turnCounter);

            if (phase != BattlePhase.PlayerPhase)
                _aiPhaseCoroutine = StartCoroutine(ExecuteAIPhase(phase));
        }

        private IEnumerator ExecuteAIPhase(BattlePhase phase)
        {
            yield return new WaitForSeconds(1f);

            var faction = PhaseToFaction(phase);
            var units = _unitRegistry.GetActableUnits(faction);
            foreach (var unit in units)
                _unitRegistry.MarkActed(unit);

            EndCurrentPhase();
        }

        public void EndCurrentPhase()
        {
            if (_aiPhaseCoroutine != null)
            {
                StopCoroutine(_aiPhaseCoroutine);
                _aiPhaseCoroutine = null;
            }

            var endingPhase = _phaseManager.CurrentPhase;
            var faction = PhaseToFaction(endingPhase);

            foreach (var unit in _unitRegistry.GetActableUnits(faction))
                _unitRegistry.MarkActed(unit);

            _turnEventChannel?.RaisePhaseEnded(endingPhase);
            _phaseManager.AdvancePhase();

            if (_phaseManager.CurrentPhase == BattlePhase.PlayerPhase)
            {
                _turnCounter++;
                _turnEventChannel?.RaiseTurnAdvanced(_turnCounter);
            }

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

        #endregion

        #region Helpers

        private static Faction PhaseToFaction(BattlePhase phase)
        {
            return phase switch
            {
                BattlePhase.PlayerPhase => Faction.Player,
                BattlePhase.EnemyPhase => Faction.Enemy,
                BattlePhase.AlliedPhase => Faction.Allied,
                _ => Faction.Player
            };
        }

        private void OnGameStateChanged(GameStateEventChannel.StateChangeArgs args)
        {
            if (args.NewState == GameState.BattleMap && args.PreviousState != GameState.BattleMapPaused)
                StartBattle();
        }

        #endregion
    }
}
