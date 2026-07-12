using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using ProjectAstra.Core.Input;
using ProjectAstra.Core.State;

namespace ProjectAstra.Core.Dialogue
{
    // The single entry point any system calls to play a dialogue script. Owns one
    // persistent view canvas across scenes, runs one script at a time, and queues
    // the rest. For BattleMap dialogue it flips the game into the Dialogue state so
    // map input is suppressed, and flips back when the queue drains.
    public class DialogueService : MonoBehaviour
    {
        public static DialogueService Instance { get; private set; }

        private const string ViewResourcePath = "UI/DialogueView";
        // A frame hitch shouldn't fast-forward the crawl in one jump; cap the step.
        private const float MaxFrameStep = 1f / 30f;

        [SerializeField] private DialogueSpeakerRegistry speakerRegistry;
        [SerializeField] private DialogueSettings settings;

        private readonly Queue<Pending> queue = new();
        private IDialogueView view;
        private DialogueRunner runner;
        private Action currentCallback;
        private bool holdsBattleMapState;
        private bool inputBound;

        private struct Pending
        {
            public DialogueScript Script;
            public DialogueTriggeringContext Context;
            public Action OnComplete;
        }

        public void Play(DialogueScript script, DialogueTriggeringContext context, Action onComplete = null)
        {
            if (script == null) { Debug.LogError("[DialogueService] Play called with null script."); return; }

            queue.Enqueue(new Pending { Script = script, Context = context, OnComplete = onComplete });
            if (runner == null) StartNext();
        }

        // Coroutine form for scripted cinematics: yield return this to block until the
        // script finishes or is skipped. Pass Cutscene context so the service leaves the
        // game state to the caller.
        public IEnumerator PlayRoutine(DialogueScript script, DialogueTriggeringContext context)
        {
            if (script == null) yield break;
            bool done = false;
            Play(script, context, () => done = true);
            while (!done) yield return null;
        }

        // Force-stops the running script from code, so a cinematic that's cut short can
        // end its dialogue too (the input-driven skip path lives in OnSkip).
        public void Skip() => runner?.Skip();

        private void Awake()
        {
            if (Instance != null && Instance != this) { Destroy(gameObject); return; }
            Instance = this;
            DontDestroyOnLoad(gameObject);
            InstantiateView();
        }

        private void Update()
        {
            if (runner != null && runner.IsRunning)
                runner.Tick(Mathf.Min(Time.unscaledDeltaTime, MaxFrameStep));
        }

        private void InstantiateView()
        {
            var prefab = Resources.Load<GameObject>(ViewResourcePath);
            if (prefab == null)
            {
                Debug.LogError($"[DialogueService] View prefab not found at Resources/{ViewResourcePath}.");
                return;
            }

            var viewInstance = Instantiate(prefab);
            DontDestroyOnLoad(viewInstance);
            view = viewInstance.GetComponent<IDialogueView>();
            if (view == null)
            {
                Debug.LogError($"[DialogueService] Prefab at Resources/{ViewResourcePath} has no IDialogueView component.");
                return;
            }
            view.Hide();
        }

        private void StartNext()
        {
            var pending = queue.Dequeue();
            currentCallback = pending.OnComplete;

            ExitBattleMapStateIfNeeded(pending.Context);

            runner = new DialogueRunner(pending.Script, speakerRegistry, view, pending.Context, settings.CharsPerSecond);
            runner.OnComplete += HandleRunnerComplete;
            BindInput();
            runner.Start();
        }

        private void HandleRunnerComplete()
        {
            var callback = currentCallback;
            currentCallback = null;
            runner = null;
            UnbindInput();

            // Hand control back to the map only once nothing else is queued.
            if (queue.Count == 0) EnterBattleMapStateIfHeld();

            callback?.Invoke();

            if (queue.Count > 0) StartNext();
        }

        private void ExitBattleMapStateIfNeeded(DialogueTriggeringContext context)
        {
            if (context != DialogueTriggeringContext.BattleMap || holdsBattleMapState) return;
            GameStateManager.Instance?.RequestTransition(GameState.Dialogue, nameof(DialogueService));
            holdsBattleMapState = true;
        }

        private void EnterBattleMapStateIfHeld()
        {
            if (!holdsBattleMapState) return;
            GameStateManager.Instance?.RequestTransition(GameState.BattleMap, nameof(DialogueService));
            holdsBattleMapState = false;
        }

        private void BindInput()
        {
            if (inputBound || InputManager.Instance == null) return;
            InputManager.Instance.OnConfirm += OnConfirm;
            InputManager.Instance.OnSkipDialogue += OnSkip;
            inputBound = true;
        }

        private void UnbindInput()
        {
            if (!inputBound || InputManager.Instance == null) { inputBound = false; return; }
            InputManager.Instance.OnConfirm -= OnConfirm;
            InputManager.Instance.OnSkipDialogue -= OnSkip;
            inputBound = false;
        }

        private void OnConfirm() => runner?.Confirm();
        private void OnSkip() => runner?.Skip();

        private void OnDestroy()
        {
            if (Instance == this) Instance = null;
            UnbindInput();
        }
    }
}
