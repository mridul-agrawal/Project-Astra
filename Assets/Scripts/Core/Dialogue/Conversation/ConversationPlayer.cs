using System;
using System.Collections.Generic;
using UnityEngine;
using ProjectAstra.Core.Input;
using ProjectAstra.Core.State;
using ProjectAstra.Core.UI.Dialogue.Choice;

namespace ProjectAstra.Core.Dialogue.Conversation
{
    // Runs a branching conversation: drives the graph, plays its scripts through DialogueService,
    // puts choices on screen, and holds the Dialogue game state for the whole exchange.
    //
    // Owns the confirm button outright while it runs. DialogueService is told not to bind input for
    // these scripts, so one press physically cannot be read as both "advance the line" and "pick
    // this option" — the same guarantee the hub's router used to provide, now living with the
    // conversation so a battle map gets it too.
    public sealed class ConversationPlayer : MonoBehaviour, IConversationPresenter
    {
        // Inspector References:
        [SerializeField] private ChoiceMenuView choiceView;
        [SerializeField] private ConversationGraphDatabase conversationDatabase;

        // Private Variables:
        private readonly InteractLatch confirm = new();
        private readonly InteractLatch cancel = new();
        private readonly InteractLatch skip = new();
        private readonly InteractLatch menuUp = new();
        private readonly InteractLatch menuDown = new();

        private ChoiceMenuController choices;
        private ConversationRunner runner;
        private Action scriptFinished;

        // What this conversation remembers between visits. Left null by a caller with nothing to
        // remember — a one-off exchange on a battle map, say.
        public IConversationMemory Memory { get; set; }

        public bool IsRunning => runner != null && runner.IsRunning;

        // Fires for each SetFlag node so a caller's progression can act on it.
        public event Action<string> FlagRaised;
        public event Action<string> ConversationFinished;

        private void Awake() => choices = new ChoiceMenuController(choiceView);

        public bool TryStart(string conversationId)
        {
            if (IsRunning || string.IsNullOrEmpty(conversationId)) return false;

            ConversationGraphData graph = FindGraph(conversationId);
            if (graph == null) return false;
            if (!EnterDialogueState()) return false;

            SuppressHeldButtons();
            StartRunner(graph, conversationId);
            return true;
        }

        private ConversationGraphData FindGraph(string conversationId)
        {
            ConversationGraphData graph = conversationDatabase != null ? conversationDatabase.Get(conversationId) : null;
            if (graph == null)
                Debug.LogError($"[Conversation] No conversation graph with id '{conversationId}'.");
            return graph;
        }

        private void StartRunner(ConversationGraphData graph, string conversationId)
        {
            runner = new ConversationRunner(graph, this, Memory);
            runner.FlagRaised += OnFlagRaised;
            runner.Completed += () => OnConversationCompleted(conversationId);
            runner.Begin();
        }


        // Input:
        // Polled behind one latch per button rather than subscribed, because Confirm is a plain
        // multicast event with no way to mark a press consumed and no release binding to latch on.
        private void Update()
        {
            if (!IsRunning) return;

            bool confirmPressed = confirm.Consume(IsHeld(GameInputAction.Confirm));
            if (choices.IsOpen) PumpChoiceInput(confirmPressed);
            else PumpDialogueInput(confirmPressed);
        }

        private void PumpDialogueInput(bool confirmPressed)
        {
            if (confirmPressed) DialogueService.Instance?.Advance();
            else if (skip.Consume(IsHeld(GameInputAction.SkipDialogue))) DialogueService.Instance?.SkipCurrent();
        }

        private void PumpChoiceInput(bool confirmPressed)
        {
            int step = ResolveMenuStep();
            if (step != 0) choices.Move(step);

            if (confirmPressed) choices.Confirm();
            else if (cancel.Consume(IsHeld(GameInputAction.Cancel))) choices.Cancel();
        }

        // One step per press — a choice list doesn't auto-repeat.
        private int ResolveMenuStep()
        {
            bool up = menuUp.Consume(IsHeld(GameInputAction.CursorUp));
            bool down = menuDown.Consume(IsHeld(GameInputAction.CursorDown));
            if (up) return -1;
            return down ? 1 : 0;
        }

        private static bool IsHeld(GameInputAction action) =>
            InputManager.Instance != null && InputManager.Instance.IsActionHeld(action);

        // The press that opened the conversation is still down this frame; without this it would
        // also advance the first line.
        private void SuppressHeldButtons()
        {
            confirm.Suppress();
            cancel.Suppress();
            skip.Suppress();
            menuUp.Suppress();
            menuDown.Suppress();
        }


        // Game State:
        // The conversation holds Dialogue for the whole exchange, not per script, so a graph made
        // of five scripts doesn't flicker back to the map four times on its way through.
        private bool EnterDialogueState()
        {
            GameStateManager states = GameStateManager.Instance;
            if (states == null) return true;
            if (states.CurrentState == GameState.Dialogue) return true;
            return states.RequestTransition(GameState.Dialogue, nameof(ConversationPlayer));
        }

        private void LeaveDialogueState()
        {
            GameStateManager states = GameStateManager.Instance;
            if (states != null && states.CanReturnToCaller()) states.ReturnToCaller(nameof(ConversationPlayer));
        }


        // --- IConversationPresenter ---

        public void BeginConversation() => DialogueView()?.BeginConversation();

        public void PlayScript(DialogueScript script, Action onFinished)
        {
            scriptFinished = onFinished;
            DialogueService.Instance.Play(script, DialogueTriggeringContext.Conversation, OnScriptComplete);
        }

        private void OnScriptComplete()
        {
            Action finished = scriptFinished;
            scriptFinished = null;
            finished?.Invoke();
        }

        public void ShowChoices(IReadOnlyList<ConversationChoice> options, bool allowCancel,
            Action<int> onChosen, Action onCancelled)
        {
            choices.Show(options, allowCancel, onChosen, onCancelled);
        }

        public void EndConversation()
        {
            choices.Hide();
            DialogueView()?.EndConversation();
            LeaveDialogueState();
        }

        private void OnFlagRaised(string flagId) => FlagRaised?.Invoke(flagId);

        private void OnConversationCompleted(string conversationId)
        {
            runner = null;
            ConversationFinished?.Invoke(conversationId);
        }

        // The one dialogue view is owned by DialogueService and lives across scenes, so it is found
        // rather than wired.
        private static ProjectAstra.Core.UI.Dialogue.DialogueView DialogueView() =>
            FindFirstObjectByType<ProjectAstra.Core.UI.Dialogue.DialogueView>(FindObjectsInactive.Include);
    }
}
