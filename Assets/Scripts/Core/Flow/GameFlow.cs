using UnityEngine;
using ProjectAstra.Core.Dialogue;
using ProjectAstra.Core.Grid;
using ProjectAstra.Core.State;

namespace ProjectAstra.Core.Flow
{
    public class GameFlow : MonoBehaviour
    {
        public static GameFlow Instance { get; private set; }

        [SerializeField] private Campaign campaign;
        [SerializeField] private MapCatalog mapCatalog;
        [SerializeField] private CutsceneCatalog cutsceneCatalog;
        private int stepIndex = -1;

        private CampaignStep CurrentStep => campaign != null ? campaign.StepAt(stepIndex) : null;

        public DialogueScript CurrentCutsceneScript =>
            (CurrentStep != null && CurrentStep.Kind == CampaignStepKind.Cutscene && cutsceneCatalog != null)
                ? cutsceneCatalog.Get(CurrentStep.Cutscene) : null;

        public MapData CurrentMap =>
            (CurrentStep != null && CurrentStep.Kind == CampaignStepKind.Battle && mapCatalog != null)
                ? mapCatalog.Get(CurrentStep.MapId) : null;

        private void Awake()
        {
            if (Instance != null && Instance != this) { Destroy(gameObject); return; }
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }

        private void OnDestroy()
        {
            if (Instance == this) Instance = null;
        }

        // Start a new game from the top of the campaign.
        public void Begin() => EnterStep(0);

        // The Cutscene scene calls this when its dialogue finishes.
        public void NotifyCutsceneFinished() => EnterStep(stepIndex + 1);

        // Called when a battle is cleared/won (hook for the next beat once battle-end exists).
        public void NotifyBattleFinished() => EnterStep(stepIndex + 1);

        private void EnterStep(int index)
        {
            this.stepIndex = index;
            var step = CurrentStep;
            if (step == null)
            {
                Debug.Log("[GameFlow] Campaign complete — returning to title.");
                this.stepIndex = -1;   // a fresh Begin() restarts from the top
                RequestState(GameState.TitleScreen);
                return;
            }

            switch (step.Kind)
            {
                case CampaignStepKind.Cutscene: RequestState(GameState.Cutscene); break;
                case CampaignStepKind.Battle:   RequestState(GameState.BattleMap); break;
            }
        }

        private void RequestState(GameState state) =>
            GameStateManager.Instance.RequestTransition(state, nameof(GameFlow));
    }
}
