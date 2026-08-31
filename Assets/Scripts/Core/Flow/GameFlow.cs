using UnityEngine;
using ProjectAstra.Core.Dialogue;
using ProjectAstra.Core.Grid;
using ProjectAstra.Core.Gurukul;
using ProjectAstra.Core.State;

namespace ProjectAstra.Core.Flow
{
    public class GameFlow : MonoBehaviour
    {
        public static GameFlow Instance { get; private set; }

        [SerializeField] private Campaign campaign;
        [SerializeField] private MapCatalog mapCatalog;
        [SerializeField] private CutsceneCatalog cutsceneCatalog;
        [SerializeField] private GurukulVisitDatabase visitDatabase;
        private int stepIndex = -1;

        private CampaignStep CurrentStep => campaign != null ? campaign.StepAt(stepIndex) : null;

        public DialogueScript CurrentCutsceneScript =>
            (CurrentStep != null && CurrentStep.Kind == CampaignStepKind.Cutscene && cutsceneCatalog != null)
                ? cutsceneCatalog.Get(CurrentStep.Cutscene) : null;

        public MapData CurrentMap =>
            (CurrentStep != null && CurrentStep.Kind == CampaignStepKind.Battle && mapCatalog != null)
                ? mapCatalog.Get(CurrentStep.MapId) : null;

        // The battle the campaign goes to after this step, if the next step is one. A hub visit
        // checks this against the destination it authored, so a disagreement is caught as a content
        // error rather than silently taking whichever one happened to win.
        public string NextBattleMapId
        {
            get
            {
                CampaignStep next = campaign != null ? campaign.StepAt(stepIndex + 1) : null;
                return next != null && next.Kind == CampaignStepKind.Battle ? next.MapId : null;
            }
        }

        public GurukulVisitData CurrentVisit =>
            (CurrentStep != null && CurrentStep.Kind == CampaignStepKind.HubVisit && visitDatabase != null)
                ? visitDatabase.Get(CurrentStep.VisitId) : null;

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

        // Entering the battle map without having walked the campaign to it — pressing Play on
        // the scene, or a dev boot that skips the intro — used to leave the campaign unstarted,
        // so the map bootstrapper silently loaded its editor fallback instead of the map the
        // campaign actually points at. Snap to the first battle so what you get is what the
        // real flow would have given you. A no-op once a battle step is already current.
        public MapData EnsureBattleStepStarted()
        {
            if (CurrentMap != null) return CurrentMap;

            int firstBattle = campaign != null ? campaign.IndexOfFirst(CampaignStepKind.Battle) : -1;
            if (firstBattle < 0) return null;

            stepIndex = firstBattle;
            return CurrentMap;
        }

        // Same idea for the hub: pressing Play on the Gurukul scene should give you the visit the
        // campaign would have, not whatever fallback the bootstrapper is holding.
        public GurukulVisitData EnsureHubStepStarted()
        {
            if (CurrentVisit != null) return CurrentVisit;

            int firstVisit = campaign != null ? campaign.IndexOfFirst(CampaignStepKind.HubVisit) : -1;
            if (firstVisit < 0) return null;

            stepIndex = firstVisit;
            return CurrentVisit;
        }

        // The Cutscene scene calls this when its dialogue finishes.
        public void NotifyCutsceneFinished() => EnterStep(stepIndex + 1);

        // Called when a battle is cleared/won (hook for the next beat once battle-end exists).
        public void NotifyBattleFinished() => EnterStep(stepIndex + 1);

        // Called once a hub visit's departure has committed.
        public void NotifyHubVisitFinished() => EnterStep(stepIndex + 1);

        private void EnterStep(int index)
        {
            int previousIndex = stepIndex;
            stepIndex = index;

            CampaignStep step = CurrentStep;
            if (step == null) { FinishCampaign(); return; }

            // A refused transition means the campaign never actually moved, so leaving the index
            // on the new step would point it at a beat the game isn't playing.
            if (!RequestStateFor(step.Kind)) stepIndex = previousIndex;
        }

        private void FinishCampaign()
        {
            Debug.Log("[GameFlow] Campaign complete — returning to title.");
            stepIndex = -1;   // a fresh Begin() restarts from the top
            RequestState(GameState.TitleScreen);
        }

        private bool RequestStateFor(CampaignStepKind kind)
        {
            switch (kind)
            {
                case CampaignStepKind.Cutscene: return RequestState(GameState.Cutscene);
                case CampaignStepKind.Battle:   return RequestState(GameState.BattleMap);
                case CampaignStepKind.HubVisit: return RequestState(GameState.Gurukul);
                default: return false;
            }
        }

        private bool RequestState(GameState state) =>
            GameStateManager.Instance.RequestTransition(state, nameof(GameFlow));
    }
}
