using System;
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

        [SerializeField] private DialogueSpeakerRegistry _speakerRegistry;
        [SerializeField] private DialogueSettings _settings;

        private readonly Queue<Pending> _queue = new();
        private IDialogueView _view;
        private DialogueRunner _runner;
        private Action _currentCallback;
        private bool _holdsBattleMapState;
        private bool _inputBound;

        private struct Pending
        {
            public DialogueScript Script;
            public DialogueContext Context;
            public Action OnComplete;
        }

        public bool IsPlaying => _runner != null && _runner.IsRunning;

        public void Play(DialogueScript script, DialogueContext context, Action onComplete = null)
        {
            if (script == null) { Debug.LogError("[DialogueService] Play called with null script."); return; }

            _queue.Enqueue(new Pending { Script = script, Context = context, OnComplete = onComplete });
            if (_runner == null) StartNext();
        }

        private void Awake()
        {
            if (Instance != null && Instance != this) { Destroy(gameObject); return; }
            Instance = this;
            DontDestroyOnLoad(gameObject);
            InstantiateView();
        }

        private void Update()
        {
            if (_runner != null && _runner.IsRunning) _runner.Tick(Time.deltaTime);
        }

        private void InstantiateView()
        {
            var prefab = Resources.Load<GameObject>(ViewResourcePath);
            if (prefab == null)
            {
                Debug.LogError($"[DialogueService] View prefab not found at Resources/{ViewResourcePath}.");
                return;
            }

            var instance = Instantiate(prefab);
            DontDestroyOnLoad(instance);
            _view = instance.GetComponent<IDialogueView>();
            if (_view == null)
            {
                Debug.LogError($"[DialogueService] Prefab at Resources/{ViewResourcePath} has no IDialogueView component.");
                return;
            }
            _view.Hide();
        }

        private void StartNext()
        {
            var pending = _queue.Dequeue();
            _currentCallback = pending.OnComplete;

            EnterBattleMapStateIfNeeded(pending.Context);

            _runner = new DialogueRunner(pending.Script, _speakerRegistry, _view, pending.Context, _settings.CharsPerSecond);
            _runner.OnComplete += HandleRunnerComplete;
            BindInput();
            _runner.Start();
        }

        private void HandleRunnerComplete()
        {
            var callback = _currentCallback;
            _currentCallback = null;
            _runner = null;
            UnbindInput();

            // Hand control back to the map only once nothing else is queued.
            if (_queue.Count == 0) ExitBattleMapStateIfHeld();

            callback?.Invoke();

            if (_queue.Count > 0) StartNext();
        }

        private void EnterBattleMapStateIfNeeded(DialogueContext context)
        {
            if (context != DialogueContext.BattleMap || _holdsBattleMapState) return;
            GameStateManager.Instance?.RequestTransition(GameState.Dialogue, nameof(DialogueService));
            _holdsBattleMapState = true;
        }

        private void ExitBattleMapStateIfHeld()
        {
            if (!_holdsBattleMapState) return;
            GameStateManager.Instance?.RequestTransition(GameState.BattleMap, nameof(DialogueService));
            _holdsBattleMapState = false;
        }

        private void BindInput()
        {
            if (_inputBound || InputManager.Instance == null) return;
            InputManager.Instance.OnConfirm += OnConfirm;
            InputManager.Instance.OnSkipDialogue += OnSkip;
            _inputBound = true;
        }

        private void UnbindInput()
        {
            if (!_inputBound || InputManager.Instance == null) { _inputBound = false; return; }
            InputManager.Instance.OnConfirm -= OnConfirm;
            InputManager.Instance.OnSkipDialogue -= OnSkip;
            _inputBound = false;
        }

        private void OnConfirm() => _runner?.Confirm();
        private void OnSkip() => _runner?.Skip();

        private void OnDestroy()
        {
            if (Instance == this) Instance = null;
            UnbindInput();
        }
    }
}
