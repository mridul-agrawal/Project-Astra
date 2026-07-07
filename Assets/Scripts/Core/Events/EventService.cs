using System;
using UnityEngine;
using ProjectAstra.Core.State;
using ProjectAstra.Core.Turn;
using ProjectAstra.Core.Combat;
using ProjectAstra.Core.Dialogue;

namespace ProjectAstra.Core.Events
{
    // The one place that holds every ScriptableObject event channel. Any script —
    // MonoBehaviour or plain C# — raises and subscribes through this service's facade
    // (RaiseX / SubscribeX) instead of touching a channel, so the channels are wired
    // exactly once (here) and nothing else in the project ever names one.
    //
    // Runs first (negative execution order) so Instance is set before any listener's
    // Awake/OnEnable/Start touches it.
    [DefaultExecutionOrder(-1000)]
    public class EventService : MonoBehaviour
    {
        public static EventService Instance { get; private set; }

        [Header("Event Channels — wired once, here")]
        [SerializeField] private GameStateEventChannel gameState;
        [SerializeField] private TurnEventChannel turn;
        [SerializeField] private UnitDeathEventChannel unitDeath;
        [SerializeField] private BattleDialogueEventChannel battleDialogue;

        // Publish/subscribe facade. Callers ask the service to raise or listen; the
        // channel assets stay sealed behind here so nothing else ever names a channel.

        // Game state
        public void RaiseGameStateChanged(GameState previous, GameState next) =>
            gameState?.Raise(new StateChangeArgs { PreviousState = previous, NewState = next });
        public void SubscribeGameStateChanged(Action<StateChangeArgs> handler) => gameState?.Register(handler);
        public void UnsubscribeGameStateChanged(Action<StateChangeArgs> handler) => gameState?.Unregister(handler);

        // Turn cycle
        public void RaisePhaseStarted(BattlePhase phase, int turnNumber) => turn?.RaisePhaseStarted(phase, turnNumber);
        public void SubscribePhaseStarted(Action<BattlePhase, int> handler) => turn?.RegisterPhaseStarted(handler);
        public void UnsubscribePhaseStarted(Action<BattlePhase, int> handler) => turn?.UnregisterPhaseStarted(handler);

        public void RaisePhaseEnded(BattlePhase phase) => turn?.RaisePhaseEnded(phase);
        public void SubscribePhaseEnded(Action<BattlePhase> handler) => turn?.RegisterPhaseEnded(handler);
        public void UnsubscribePhaseEnded(Action<BattlePhase> handler) => turn?.UnregisterPhaseEnded(handler);

        public void RaiseTurnAdvanced(int turnNumber) => turn?.RaiseTurnAdvanced(turnNumber);
        public void SubscribeTurnAdvanced(Action<int> handler) => turn?.RegisterTurnAdvanced(handler);
        public void UnsubscribeTurnAdvanced(Action<int> handler) => turn?.UnregisterTurnAdvanced(handler);

        public void RaisePhaseBannerFinished(BattlePhase phase, int turnNumber) => turn?.RaisePhaseBannerFinished(phase, turnNumber);
        public void SubscribePhaseBannerFinished(Action<BattlePhase, int> handler) => turn?.RegisterPhaseBannerFinished(handler);
        public void UnsubscribePhaseBannerFinished(Action<BattlePhase, int> handler) => turn?.UnregisterPhaseBannerFinished(handler);

        // Unit death
        public void RaiseUnitDeath(UnitDeathEventArgs args) => unitDeath?.Raise(args);
        public void SubscribeUnitDeath(Action<UnitDeathEventArgs> handler) => unitDeath?.Register(handler);
        public void UnsubscribeUnitDeath(Action<UnitDeathEventArgs> handler) => unitDeath?.Unregister(handler);

        // Battle-map dialogue moments
        public void RaiseBattleDialogue(BattleDialogueEventType eventType) => battleDialogue?.Raise(eventType);
        public void SubscribeBattleDialogue(Action<BattleDialogueEventType> handler) => battleDialogue?.Register(handler);
        public void UnsubscribeBattleDialogue(Action<BattleDialogueEventType> handler) => battleDialogue?.Unregister(handler);

        private void Awake()
        {
            if (Instance != null && Instance != this) { Destroy(gameObject); return; }
            Instance = this;
            DontDestroyOnLoad(gameObject);
            WarnOnMissingChannels();
        }

        private void OnDestroy()
        {
            if (Instance == this) Instance = null;
        }

        // A forgotten wiring should fail loudly here instead of a listener silently going dead.
        private void WarnOnMissingChannels()
        {
            if (gameState == null) Debug.LogError("[EventService] GameState channel is not wired.");
            if (turn == null) Debug.LogError("[EventService] Turn channel is not wired.");
            if (unitDeath == null) Debug.LogError("[EventService] UnitDeath channel is not wired.");
            if (battleDialogue == null) Debug.LogError("[EventService] BattleDialogue channel is not wired.");
        }

        // Awake doesn't run in EditMode tests; this lets a fixture stand the service up by hand.
        internal void InitializeForTest(GameStateEventChannel gameState, TurnEventChannel turn,
            UnitDeathEventChannel unitDeath, BattleDialogueEventChannel battleDialogue)
        {
            this.gameState = gameState;
            this.turn = turn;
            this.unitDeath = unitDeath;
            this.battleDialogue = battleDialogue;
            Instance = this;
        }
    }
}
