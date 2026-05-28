using UnityEngine;
using ProjectAstra.Core.Dialogue;
using ProjectAstra.Core.Grid;
using ProjectAstra.Core.State;

namespace ProjectAstra.Core.Flow
{
    // The campaign director. The whole game's running order lives in the Campaign array below,
    // read top-to-bottom. GameFlow drives the scene-state machine: it decides which cutscene
    // script plays and which battle map loads, advancing one step each time a cutscene or
    // battle reports it finished. The Cutscene and BattleMap scenes stay "dumb" — they ask
    // GameFlow what to present (CurrentCutsceneScript / CurrentMap) and call back when done.
    public class GameFlow : MonoBehaviour
    {
        public static GameFlow Instance { get; private set; }

        [SerializeField] private MapCatalog _mapCatalog;
        [SerializeField] private CutsceneCatalog _cutsceneCatalog;

        // The entire chapter order. Add a line to add a beat — this is the one place to edit.
        private static readonly Step[] Campaign =
        {
            Step.PlayCutscene(CutsceneId.Opening),
            Step.LoadBattle(MapId.Map1_BridgeAtSuvarnapur),
        };

        private int _index = -1;

        private Step Current => (_index >= 0 && _index < Campaign.Length) ? Campaign[_index] : Step.None;

        // What the current scene should present — null when the step isn't of that kind, or
        // when no campaign is running (editor direct-play falls back to scene-local defaults).
        public DialogueScript CurrentCutsceneScript =>
            (Current.Kind == StepKind.Cutscene && _cutsceneCatalog != null) ? _cutsceneCatalog.Get(Current.Cutscene) : null;

        public MapData CurrentMap =>
            (Current.Kind == StepKind.Battle && _mapCatalog != null) ? _mapCatalog.Get(Current.Map) : null;

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
            switch (Current.Kind)
            {
                case StepKind.Cutscene: RequestState(GameState.Cutscene); break;
                case StepKind.Battle:   RequestState(GameState.BattleMap); break;
                default:                Debug.Log("[GameFlow] Campaign complete — no further steps."); break;
            }
        }

        private void RequestState(GameState state) =>
            GameStateManager.Instance.RequestTransition(state, nameof(GameFlow));

        private enum StepKind { None, Cutscene, Battle }

        private readonly struct Step
        {
            public readonly StepKind Kind;
            public readonly CutsceneId Cutscene;
            public readonly MapId Map;

            private Step(StepKind kind, CutsceneId cutscene, MapId map)
            {
                Kind = kind; Cutscene = cutscene; Map = map;
            }

            public static readonly Step None = new(StepKind.None, CutsceneId.None, MapId.None);
            public static Step PlayCutscene(CutsceneId id) => new(StepKind.Cutscene, id, MapId.None);
            public static Step LoadBattle(MapId id) => new(StepKind.Battle, CutsceneId.None, id);
        }
    }
}
