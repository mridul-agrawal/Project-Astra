using System;
using UnityEngine;
using System.Collections.Generic;
using ProjectAstra.Core.State;
using ProjectAstra.Core.Turn;
using ProjectAstra.Core.Combat;
using ProjectAstra.Core.Cursor;
using ProjectAstra.Core.Dialogue;
using ProjectAstra.Core.Units;
using ProjectAstra.Core.Quests;

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
        [SerializeField] private CursorEventChannel cursor;
        [SerializeField] private QuestEventChannel quest;
        [SerializeField] private GameplaySignalChannel gameplaySignals;

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

        // Grid cursor
        public void RaiseCursorStepped(Vector2Int position) => cursor?.RaiseCursorStepped(position);
        public void SubscribeCursorStepped(Action<Vector2Int> handler) => cursor?.RegisterCursorStepped(handler);
        public void UnsubscribeCursorStepped(Action<Vector2Int> handler) => cursor?.UnregisterCursorStepped(handler);

        public void RaiseHoverChanged(CursorHover hover, TestUnit unit) => cursor?.RaiseHoverChanged(hover, unit);
        public void SubscribeHoverChanged(Action<CursorHover, TestUnit> handler) => cursor?.RegisterHoverChanged(handler);
        public void UnsubscribeHoverChanged(Action<CursorHover, TestUnit> handler) => cursor?.UnregisterHoverChanged(handler);

        public void RaiseUnitSelected(TestUnit unit) => cursor?.RaiseUnitSelected(unit);
        public void SubscribeUnitSelected(Action<TestUnit> handler) => cursor?.RegisterUnitSelected(handler);
        public void UnsubscribeUnitSelected(Action<TestUnit> handler) => cursor?.UnregisterUnitSelected(handler);

        public void RaisePathPreviewChanged(IReadOnlyList<Vector2Int> path) => cursor?.RaisePathPreviewChanged(path);
        public void SubscribePathPreviewChanged(Action<IReadOnlyList<Vector2Int>> handler) => cursor?.RegisterPathPreviewChanged(handler);
        public void UnsubscribePathPreviewChanged(Action<IReadOnlyList<Vector2Int>> handler) => cursor?.UnregisterPathPreviewChanged(handler);

        public void RaiseMoveConfirmed(TestUnit unit, Vector2Int destination) => cursor?.RaiseMoveConfirmed(unit, destination);
        public void SubscribeMoveConfirmed(Action<TestUnit, Vector2Int> handler) => cursor?.RegisterMoveConfirmed(handler);
        public void UnsubscribeMoveConfirmed(Action<TestUnit, Vector2Int> handler) => cursor?.UnregisterMoveConfirmed(handler);

        public void RaiseMoveCancelled(TestUnit unit) => cursor?.RaiseMoveCancelled(unit);
        public void SubscribeMoveCancelled(Action<TestUnit> handler) => cursor?.RegisterMoveCancelled(handler);
        public void UnsubscribeMoveCancelled(Action<TestUnit> handler) => cursor?.UnregisterMoveCancelled(handler);

        public void RaiseSelectionCancelled() => cursor?.RaiseSelectionCancelled();
        public void SubscribeSelectionCancelled(Action handler) => cursor?.RegisterSelectionCancelled(handler);
        public void UnsubscribeSelectionCancelled(Action handler) => cursor?.UnregisterSelectionCancelled(handler);

        public void RaiseUnitSpentTurn(TestUnit unit) => cursor?.RaiseUnitSpentTurn(unit);
        public void SubscribeUnitSpentTurn(Action<TestUnit> handler) => cursor?.RegisterUnitSpentTurn(handler);
        public void UnsubscribeUnitSpentTurn(Action<TestUnit> handler) => cursor?.UnregisterUnitSpentTurn(handler);

        public void RaiseCursorError(CursorErrorKind kind) => cursor?.RaiseErrorFeedback(kind);
        public void SubscribeCursorError(Action<CursorErrorKind> handler) => cursor?.RegisterErrorFeedback(handler);
        public void UnsubscribeCursorError(Action<CursorErrorKind> handler) => cursor?.UnregisterErrorFeedback(handler);

        // Quests — what the quest system announces
        public void RaiseQuestStarted(QuestData started) => quest?.RaiseQuestStarted(started);
        public void SubscribeQuestStarted(Action<QuestData> handler) => quest?.RegisterQuestStarted(handler);
        public void UnsubscribeQuestStarted(Action<QuestData> handler) => quest?.UnregisterQuestStarted(handler);

        public void RaiseObjectiveActivated(ObjectiveStatus status) => quest?.RaiseObjectiveActivated(status);
        public void SubscribeObjectiveActivated(Action<ObjectiveStatus> handler) => quest?.RegisterObjectiveActivated(handler);
        public void UnsubscribeObjectiveActivated(Action<ObjectiveStatus> handler) => quest?.UnregisterObjectiveActivated(handler);

        public void RaiseObjectiveProgressed(ObjectiveStatus status) => quest?.RaiseObjectiveProgressed(status);
        public void SubscribeObjectiveProgressed(Action<ObjectiveStatus> handler) => quest?.RegisterObjectiveProgressed(handler);
        public void UnsubscribeObjectiveProgressed(Action<ObjectiveStatus> handler) => quest?.UnregisterObjectiveProgressed(handler);

        public void RaiseObjectiveCompleted(QuestObjective objective) => quest?.RaiseObjectiveCompleted(objective);
        public void SubscribeObjectiveCompleted(Action<QuestObjective> handler) => quest?.RegisterObjectiveCompleted(handler);
        public void UnsubscribeObjectiveCompleted(Action<QuestObjective> handler) => quest?.UnregisterObjectiveCompleted(handler);

        public void RaiseQuestCompleted(QuestData completed) => quest?.RaiseQuestCompleted(completed);
        public void SubscribeQuestCompleted(Action<QuestData> handler) => quest?.RegisterQuestCompleted(handler);
        public void UnsubscribeQuestCompleted(Action<QuestData> handler) => quest?.UnregisterQuestCompleted(handler);

        // Gameplay signals — what happened, in the words of whoever it happened to
        public void RaiseGameplaySignal(GameplaySignal signal) => gameplaySignals?.Raise(signal);
        public void SubscribeGameplaySignal(Action<GameplaySignal> handler) => gameplaySignals?.Register(handler);
        public void UnsubscribeGameplaySignal(Action<GameplaySignal> handler) => gameplaySignals?.Unregister(handler);

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
            if (cursor == null) Debug.LogError("[EventService] Cursor channel is not wired.");
            if (quest == null) Debug.LogError("[EventService] Quest channel is not wired.");
            if (gameplaySignals == null) Debug.LogError("[EventService] GameplaySignal channel is not wired.");
        }

        // Awake doesn't run in EditMode tests; this lets a fixture stand the service up by hand.
        internal void InitializeForTest(GameStateEventChannel gameState, TurnEventChannel turn,
            UnitDeathEventChannel unitDeath, BattleDialogueEventChannel battleDialogue,
            CursorEventChannel cursor = null, QuestEventChannel quest = null,
            GameplaySignalChannel gameplaySignals = null)
        {
            this.quest = quest;
            this.gameplaySignals = gameplaySignals;
            this.gameState = gameState;
            this.turn = turn;
            this.unitDeath = unitDeath;
            this.battleDialogue = battleDialogue;
            this.cursor = cursor;
            Instance = this;
        }
    }
}
