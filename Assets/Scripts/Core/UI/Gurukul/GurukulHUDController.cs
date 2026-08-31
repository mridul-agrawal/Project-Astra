using UnityEngine;
using ProjectAstra.Core.Gurukul;
using ProjectAstra.Core.Input;
using ProjectAstra.Core.UI.Gurukul.Objective;
using ProjectAstra.Core.UI.Gurukul.Prompt;

namespace ProjectAstra.Core.UI.Gurukul
{
    // Composition root for the hub HUD. Owns the views, news up the controllers, and is the single
    // place anything subscribes — the same arrangement BattleHUDUIController uses for the battle
    // map, so both HUDs read the same way.
    public sealed class GurukulHUDController : MonoBehaviour
    {
        [SerializeField] private InteractionPromptView promptView;
        [SerializeField] private GurukulObjectiveView objectiveView;
        [SerializeField] private InputGlyphData glyphData;
        [SerializeField] private GurukulInteractionDriver interactionDriver;
        [SerializeField] private GurukulInputRouter router;

        private InteractionPromptController prompt;
        private GurukulObjectiveController objectives;

        private void Awake()
        {
            prompt = new InteractionPromptController(promptView, glyphData);
            objectives = new GurukulObjectiveController(objectiveView);

            if (interactionDriver == null) interactionDriver = FindFirstObjectByType<GurukulInteractionDriver>();
            if (router == null) router = FindFirstObjectByType<GurukulInputRouter>();
        }

        // Bound in Start, after the bootstrapper has loaded the visit — and Bind refreshes rather
        // than waiting for an event, so the opening objective it already announced isn't missed.
        private void Start() => objectives.Bind(GurukulProgressService.Instance?.Objectives);

        private void OnEnable() => Subscribe();
        private void OnDisable() => Unsubscribe();

        private void Subscribe()
        {
            if (interactionDriver != null) interactionDriver.PromptChanged += prompt.HandleTargetChanged;
            if (router != null) router.States.StateChanged += objectives.HandleSubStateChanged;
            if (InputManager.Instance != null) InputManager.Instance.OnDeviceChanged += prompt.HandleDeviceChanged;
        }

        private void Unsubscribe()
        {
            if (interactionDriver != null) interactionDriver.PromptChanged -= prompt.HandleTargetChanged;
            if (router != null) router.States.StateChanged -= objectives.HandleSubStateChanged;
            if (InputManager.Instance != null) InputManager.Instance.OnDeviceChanged -= prompt.HandleDeviceChanged;
            objectives.Unbind();
        }
    }
}
