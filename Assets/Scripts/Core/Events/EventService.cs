using UnityEngine;
using ProjectAstra.Core.State;
using ProjectAstra.Core.Turn;
using ProjectAstra.Core.Combat;
using ProjectAstra.Core.Dialogue;

namespace ProjectAstra.Core.Events
{
    // The one place that holds every ScriptableObject event channel. Any script —
    // MonoBehaviour or plain C# — reaches a bus through EventService.Instance.<Channel>
    // instead of carrying its own wired reference, so the channels are wired exactly
    // once (here) and stay decoupled from the gameplay managers that raise on them.
    //
    // Runs first (negative execution order) so Instance is set before any listener's
    // Awake/OnEnable/Start touches it.
    [DefaultExecutionOrder(-1000)]
    public class EventService : MonoBehaviour
    {
        public static EventService Instance { get; private set; }

        [Header("Event Channels — wired once, here")]
        [SerializeField] private GameStateEventChannel gameState;
        [SerializeField] private TurnEventChannel turn;
        [SerializeField] private UnitDeathEventChannel unitDeath;
        [SerializeField] private BattleDialogueEventChannel battleDialogue;

        public GameStateEventChannel GameState => gameState;
        public TurnEventChannel Turn => turn;
        public UnitDeathEventChannel UnitDeath => unitDeath;
        public BattleDialogueEventChannel BattleDialogue => battleDialogue;

        private void Awake()
        {
            if (Instance != null && Instance != this) { Destroy(gameObject); return; }
            Instance = this;
            DontDestroyOnLoad(gameObject);
            WarnOnMissingChannels();
        }

        private void OnDestroy()
        {
            if (Instance == this) Instance = null;
        }

        // A forgotten wiring should fail loudly here instead of a listener silently going dead.
        private void WarnOnMissingChannels()
        {
            if (gameState == null) Debug.LogError("[EventService] GameState channel is not wired.");
            if (turn == null) Debug.LogError("[EventService] Turn channel is not wired.");
            if (unitDeath == null) Debug.LogError("[EventService] UnitDeath channel is not wired.");
            if (battleDialogue == null) Debug.LogError("[EventService] BattleDialogue channel is not wired.");
        }

        // Awake doesn't run in EditMode tests; this lets a fixture stand the service up by hand.
        internal void InitializeForTest(GameStateEventChannel gameState, TurnEventChannel turn,
            UnitDeathEventChannel unitDeath, BattleDialogueEventChannel battleDialogue)
        {
            this.gameState = gameState;
            this.turn = turn;
            this.unitDeath = unitDeath;
            this.battleDialogue = battleDialogue;
            Instance = this;
        }
    }
}
