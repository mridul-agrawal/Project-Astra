using System;
using System.Collections.Generic;
using UnityEngine;
using ProjectAstra.Core.Dialogue;
using ProjectAstra.Core.UI.Gurukul.Choice;

namespace ProjectAstra.Core.Gurukul.Conversation
{
    // Runs hub conversations for real: drives the graph, plays its scripts through DialogueService,
    // puts choices on screen, and moves the hub's sub-state as it goes.
    //
    // Owns no input of its own — every press arrives from GurukulInputRouter, which is what keeps
    // one button from being read by both the dialogue box and the choice list.
    public sealed class GurukulConversationPlayer : MonoBehaviour, IConversationPresenter
    {
        [SerializeField] private GurukulInputRouter router;
        [SerializeField] private ChoiceMenuView choiceView;
        [SerializeField] private ConversationGraphDatabase conversationDatabase;

        private ChoiceMenuController choices;
        private GurukulConversationRunner runner;
        private Action scriptFinished;

        public bool IsRunning => runner != null && runner.IsRunning;

        // Fires for each SetFlag node so the visit's progression can act on it.
        public event Action<string> FlagRaised;
        public event Action<string> ConversationFinished;

        private void Awake()
        {
            choices = new ChoiceMenuController(choiceView);
            if (router == null) router = FindFirstObjectByType<GurukulInputRouter>();
        }

        public void Bind(GurukulInputRouter inputRouter) => router = inputRouter;

        public bool TryStart(string conversationId)
        {
            if (IsRunning || string.IsNullOrEmpty(conversationId)) return false;

            ConversationGraphData graph = conversationDatabase != null ? conversationDatabase.Get(conversationId) : null;
            if (graph == null)
            {
                Debug.LogError($"[GurukulConversation] No conversation graph with id '{conversationId}'.");
                return false;
            }

            if (!router.States.TryTransition(GurukulSubState.Conversation)) return false;

            runner = new GurukulConversationRunner(graph, this, GurukulProgressService.Instance?.State);
            runner.FlagRaised += OnFlagRaised;
            runner.Completed += () => OnConversationCompleted(conversationId);
            runner.Begin();
            return true;
        }

        private void Update()
        {
            if (router == null) return;

            if (choices.IsOpen) PumpChoiceInput();
            else if (router.States.CurrentState == GurukulSubState.Conversation) PumpDialogueInput();
        }

        private void PumpDialogueInput()
        {
            if (router.AdvancePressed) DialogueService.Instance?.Advance();
            else if (router.SkipPressed) DialogueService.Instance?.SkipCurrent();
        }

        private void PumpChoiceInput()
        {
            if (router.MenuStep != 0) choices.Move(router.MenuStep);
            if (router.ConfirmPressed) choices.Confirm();
            else if (router.CancelPressed) choices.Cancel();
        }

        // --- IConversationPresenter ---

        public void BeginConversation() => DialogueView().BeginConversation();

        public void PlayScript(DialogueScript script, Action onFinished)
        {
            router.States.TryTransition(GurukulSubState.Conversation);
            scriptFinished = onFinished;
            DialogueService.Instance.Play(script, DialogueTriggeringContext.Gurukul, OnScriptComplete);
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
            router.States.TryTransition(GurukulSubState.ChoiceOrQuiz);
            choices.Show(options, allowCancel,
                index => { ReturnToConversation(); onChosen(index); },
                () => { ReturnToConversation(); onCancelled(); });
        }

        private void ReturnToConversation() => router.States.TryTransition(GurukulSubState.Conversation);

        public void EndConversation()
        {
            choices.Hide();
            DialogueView().EndConversation();
            router.States.TryTransition(GurukulSubState.FreeExploration);
        }

        private void OnFlagRaised(string flagId) => FlagRaised?.Invoke(flagId);

        private void OnConversationCompleted(string conversationId)
        {
            runner = null;
            ConversationFinished?.Invoke(conversationId);
        }

        // The one dialogue view is owned by DialogueService and lives across scenes, so it is found
        // rather than wired.
        private static UI.Dialogue.DialogueView DialogueView() =>
            FindFirstObjectByType<UI.Dialogue.DialogueView>(FindObjectsInactive.Include);
    }
}
