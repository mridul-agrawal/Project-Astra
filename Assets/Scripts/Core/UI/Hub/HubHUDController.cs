using UnityEngine;
using ProjectAstra.Core.Events;
using ProjectAstra.Core.Hub;
using ProjectAstra.Core.Input;
using ProjectAstra.Core.UI.Hub.Objective;
using ProjectAstra.Core.UI.Hub.Prompt;

namespace ProjectAstra.Core.UI.Hub
{
    // Composition root for the hub HUD. Owns the views, news up the controllers, and is the single
    // place anything subscribes — the same arrangement BattleHUDUIController uses for the battle
    // map, so both HUDs read the same way.
    public sealed class HubHUDController : MonoBehaviour
    {
        [SerializeField] private InteractionPromptView promptView;
        [SerializeField] private HubObjectiveView objectiveView;
        [SerializeField] private InputGlyphData glyphData;
        [SerializeField] private HubInteractionDriver interactionDriver;
        [SerializeField] private HubInputRouter router;

        private InteractionPromptController prompt;
        private HubObjectiveController objectives;

        private void Awake()
        {
            prompt = new InteractionPromptController(promptView, glyphData);
            objectives = new HubObjectiveController(objectiveView);

            if (interactionDriver == null) interactionDriver = FindFirstObjectByType<HubInteractionDriver>();
            if (router == null) router = FindFirstObjectByType<HubInputRouter>();
        }

        // Bound in Start, after the bootstrapper has loaded the visit — and Bind refreshes rather
        // than waiting for an event, so the opening objective it already announced isn't missed.
        private void Start() => objectives.Bind(HubProgressService.Instance?.Objectives);

        private void OnEnable() => Subscribe();
        private void OnDisable() => Unsubscribe();

        private void Subscribe()
        {
            if (interactionDriver != null) interactionDriver.PromptChanged += prompt.HandleTargetChanged;
            EventService.Instance?.SubscribeGameStateChanged(objectives.HandleGameStateChanged);
            if (InputManager.Instance != null) InputManager.Instance.OnDeviceChanged += prompt.HandleDeviceChanged;
        }

        private void Unsubscribe()
        {
            if (interactionDriver != null) interactionDriver.PromptChanged -= prompt.HandleTargetChanged;
            EventService.Instance?.UnsubscribeGameStateChanged(objectives.HandleGameStateChanged);
            if (InputManager.Instance != null) InputManager.Instance.OnDeviceChanged -= prompt.HandleDeviceChanged;
            objectives.Unbind();
        }
    }
}
