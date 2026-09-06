using UnityEngine;
using ProjectAstra.Core.Events;
using ProjectAstra.Core.Hub;
using ProjectAstra.Core.Input;
using ProjectAstra.Core.Quests;
using ProjectAstra.Core.UI.BattleMap.HUD;
using ProjectAstra.Core.UI.Hub.Objective;
using ProjectAstra.Core.UI.Hub.Prompt;

namespace ProjectAstra.Core.UI.Hub
{
    // Composition root for the hub HUD: owns the views, news up the controllers, subscribes once.
    public sealed class HubHUDController : MonoBehaviour
    {
        [SerializeField] private InteractionPromptView promptView;
        [SerializeField] private ObjectiveView objectiveView;
        [SerializeField] private InputGlyphData glyphData;
        [SerializeField] private HubPlayerController player;

        private InteractionPromptController prompt;
        private HubObjectiveController objectives;
        private bool boundToPlayer;

        private void Awake()
        {
            WakeUp();
            prompt = new InteractionPromptController(promptView, glyphData);
            objectives = new HubObjectiveController(objectiveView);
        }

        // Every child of a UI canvas is off in the scene, the project's rule. These two run their own
        // coroutines and hide their own content, so they have to be alive from the start — the same
        // job BattleUIActivator does on the battle map.
        private void WakeUp()
        {
            Wake(promptView);
            Wake(objectiveView);
        }

        private static void Wake(Component view)
        {
            if (view != null && !view.gameObject.activeSelf) view.gameObject.SetActive(true);
        }

        // The opening objective is announced during the bootstrapper's Start, before this one runs,
        // so the first line is caught by asking rather than by waiting for the next event.
        private void Start()
        {
            BindPlayer();
            RefreshObjective();
        }

        private void OnEnable() => Subscribe();
        private void OnDisable() => Unsubscribe();

        // The prompt is let go a moment after the target is, so standing on the edge of something's
        // reach doesn't strobe it.
        private void Update() => prompt.Tick(Time.deltaTime);

        // She is built by the bootstrapper mid-Start, so this is tried from both OnEnable and Start
        // and only the one that finds her sticks.
        private void BindPlayer()
        {
            if (boundToPlayer) return;
            if (player == null) player = FindFirstObjectByType<HubPlayerController>();
            if (player == null) return;

            player.Interaction.TargetChanged += prompt.HandleTargetChanged;
            boundToPlayer = true;
        }

        private void Subscribe()
        {
            BindPlayer();
            EventService.Instance?.SubscribeGameStateChanged(objectives.HandleGameStateChanged);
            EventService.Instance?.SubscribeObjectiveActivated(objectives.HandleObjectiveActivated);
            EventService.Instance?.SubscribeObjectiveProgressed(objectives.HandleObjectiveProgressed);
            EventService.Instance?.SubscribeObjectiveCompleted(objectives.HandleObjectiveCompleted);
            EventService.Instance?.SubscribeQuestCompleted(objectives.HandleQuestCompleted);
            if (InputManager.Instance == null) return;
            InputManager.Instance.OnDeviceChanged += prompt.HandleDeviceChanged;
            InputManager.Instance.OnPeekObjective += objectives.HandlePeek;
        }

        private void Unsubscribe()
        {
            if (boundToPlayer) player.Interaction.TargetChanged -= prompt.HandleTargetChanged;
            boundToPlayer = false;

            EventService.Instance?.UnsubscribeGameStateChanged(objectives.HandleGameStateChanged);
            EventService.Instance?.UnsubscribeObjectiveActivated(objectives.HandleObjectiveActivated);
            EventService.Instance?.UnsubscribeObjectiveProgressed(objectives.HandleObjectiveProgressed);
            EventService.Instance?.UnsubscribeObjectiveCompleted(objectives.HandleObjectiveCompleted);
            EventService.Instance?.UnsubscribeQuestCompleted(objectives.HandleQuestCompleted);
            if (InputManager.Instance == null) return;
            InputManager.Instance.OnDeviceChanged -= prompt.HandleDeviceChanged;
            InputManager.Instance.OnPeekObjective -= objectives.HandlePeek;
        }

        private void RefreshObjective()
        {
            QuestRunner runner = QuestManager.Instance?.Runner;
            if (runner != null) objectives.HandleObjectiveActivated(runner.Status);
        }
    }
}
