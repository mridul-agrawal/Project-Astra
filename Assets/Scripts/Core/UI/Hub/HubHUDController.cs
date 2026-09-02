using UnityEngine;
using ProjectAstra.Core.Events;
using ProjectAstra.Core.Hub;
using ProjectAstra.Core.Input;
using ProjectAstra.Core.UI.Hub.Objective;
using ProjectAstra.Core.UI.Hub.Prompt;

namespace ProjectAstra.Core.UI.Hub
{
    // Composition root for the hub HUD: owns the views, news up the controllers, subscribes once.
    public sealed class HubHUDController : MonoBehaviour
    {
        [SerializeField] private InteractionPromptView promptView;
        [SerializeField] private HubObjectiveView objectiveView;
        [SerializeField] private InputGlyphData glyphData;
        [SerializeField] private HubPlayerController player;

        private InteractionPromptController prompt;
        private HubObjectiveController objectives;
        private bool boundToPlayer;

        private void Awake()
        {
            prompt = new InteractionPromptController(promptView, glyphData);
            objectives = new HubObjectiveController(objectiveView);
        }

        // Bound in Start, after the bootstrapper has loaded the visit — and Bind refreshes rather
        // than waiting for an event, so the opening objective it already announced isn't missed.
        private void Start()
        {
            objectives.Bind(HubProgressService.Instance?.Objectives);
            BindPlayer();
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
            if (InputManager.Instance != null) InputManager.Instance.OnDeviceChanged += prompt.HandleDeviceChanged;
        }

        private void Unsubscribe()
        {
            if (boundToPlayer) player.Interaction.TargetChanged -= prompt.HandleTargetChanged;
            boundToPlayer = false;

            EventService.Instance?.UnsubscribeGameStateChanged(objectives.HandleGameStateChanged);
            if (InputManager.Instance != null) InputManager.Instance.OnDeviceChanged -= prompt.HandleDeviceChanged;
            objectives.Unbind();
        }
    }
}
