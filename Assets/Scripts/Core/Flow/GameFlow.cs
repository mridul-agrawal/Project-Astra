using UnityEngine;
using ProjectAstra.Core.Dialogue;
using ProjectAstra.Core.Grid;
using ProjectAstra.Core.State;

namespace ProjectAstra.Core.Flow
{
    // The campaign director. The whole game's running order lives in the Campaign asset, read
    // top-to-bottom. GameFlow drives the scene-state machine: it decides which cutscene script
    // plays and which battle map loads, advancing one step each time a cutscene or battle reports
    // it finished. The Cutscene and BattleMap scenes stay "dumb" — they ask GameFlow what to
    // present (CurrentCutsceneScript / CurrentMap) and call back when done.
    public class GameFlow : MonoBehaviour
    {
        public static GameFlow Instance { get; private set; }

        [SerializeField] private Campaign _campaign;
        [SerializeField] private MapCatalog _mapCatalog;
        [SerializeField] private CutsceneCatalog _cutsceneCatalog;

        private int _index = -1;

        // The current beat, or null when the index has run past the end (or no campaign is wired —
        // e.g. editor direct-play, where scenes fall back to their own defaults).
        private CampaignStep Current => _campaign != null ? _campaign.StepAt(_index) : null;

        public DialogueScript CurrentCutsceneScript =>
            (Current != null && Current.Kind == CampaignStepKind.Cutscene && _cutsceneCatalog != null)
                ? _cutsceneCatalog.Get(Current.Cutscene) : null;

        public MapData CurrentMap =>
            (Current != null && Current.Kind == CampaignStepKind.Battle && _mapCatalog != null)
                ? _mapCatalog.Get(Current.Map) : null;

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
        public void NotifyCutsceneFinished() => EnterStep(_index + 1);

        // Called when a battle is cleared/won (hook for the next beat once battle-end exists).
        public void NotifyBattleFinished() => EnterStep(_index + 1);

        private void EnterStep(int index)
        {
            _index = index;
            var step = Current;
            if (step == null)
            {
                Debug.Log("[GameFlow] Campaign complete — returning to title.");
                _index = -1;   // a fresh Begin() restarts from the top
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
