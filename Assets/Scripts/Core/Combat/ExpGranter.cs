using System;
using System.Collections;
using System.Collections.Generic;
using ProjectAstra.Core.State;
using ProjectAstra.Core.UI;
using ProjectAstra.Core.UI.Overlays;
using ProjectAstra.Core.UI.Progression;
using ProjectAstra.Core.UI.UnitInfo;
using ProjectAstra.Core.Units;
using UnityEngine;

namespace ProjectAstra.Core.Combat
{
    // Experience Scaling orchestrator. Routes EXP grants through three steps:
    //   1. Filter (Player faction only; UnitInstance present).
    //   2. EXP counter overlay animation.
    //   3. AddExp → chained level-up screens while CurrentEXP ≥ 100 and not
    //      at the promoted cap.
    //
    // Grants that arrive during an active animation are queued and drained
    // in order so the UI never collides.
    //
    // Unpromoted Lv-20 units silently accumulate EXP in the background (spec:
    // "still gain EXP from actions, but the counter does not advance") —
    // UnitInfoPanelUI shows '--' for them.
    public class ExpGranter : MonoBehaviour
    {
        public static ExpGranter Instance { get; private set; }

        [SerializeField] private ExpGainOverlayUI _expGainOverlay;
        [SerializeField] private LevelUpScreenUI _levelUpScreen;

        private static readonly Func<int, bool> RollRandom =
            growthRate => UnityEngine.Random.Range(0, 100) < growthRate;

        private readonly Queue<Pending> _queue = new Queue<Pending>();
        private bool _draining;

        private struct Pending
        {
            public TestUnit recipient;
            public int amount;
            public Action onComplete;
        }

        private void Awake()
        {
            if (Instance != null && Instance != this) { Destroy(gameObject); return; }
            Instance = this;
        }

        private void OnDestroy()
        {
            if (Instance == this) Instance = null;
        }

        public void Grant(TestUnit recipient, int amount, Action onComplete = null)
        {
            if (!IsGrantValid(recipient, amount))
            {
                onComplete?.Invoke();
                return;
            }

            if (IsUnpromotedAtLevelCap(recipient.UnitInstance))
            {
                // Spec: still gain EXP, but counter does not advance and no
                // level-ups fire.
                recipient.UnitInstance.AddExp(amount);
                onComplete?.Invoke();
                return;
            }

            _queue.Enqueue(new Pending { recipient = recipient, amount = amount, onComplete = onComplete });
            if (!_draining) StartCoroutine(Drain());
        }

        private static bool IsGrantValid(TestUnit recipient, int amount)
        {
            return amount > 0
                && recipient != null
                && recipient.faction == Faction.Player
                && recipient.UnitInstance != null;
        }

        private static bool IsUnpromotedAtLevelCap(UnitInstance inst)
        {
            return inst.CurrentClass != null
                && !inst.CurrentClass.IsPromoted
                && inst.Level >= UnitInstance.PromotedLevelCap;
        }

        private IEnumerator Drain()
        {
            _draining = true;
            while (_queue.Count > 0)
            {
                var pending = _queue.Dequeue();
                yield return PlayGrant(pending);
                pending.onComplete?.Invoke();
            }
            _draining = false;
        }

        private IEnumerator PlayGrant(Pending p)
        {
            var unit = p.recipient;
            var inst = unit?.UnitInstance;
            if (inst == null) yield break;

            int preExp = inst.CurrentEXP;

            if (_expGainOverlay != null)
                yield return _expGainOverlay.Play(unit, preExp, p.amount);

            inst.AddExp(p.amount);

            while (inst.CurrentEXP >= UnitInstance.ExpPerLevel && !inst.IsAtLevelCap)
                yield return PlayLevelUp(unit);
        }

        private IEnumerator PlayLevelUp(TestUnit unit)
        {
            var inst = unit.UnitInstance;
            if (inst == null) yield break;

            int preLevel = inst.Level;
            var preStats = inst.Stats;
            var portrait = inst.Definition != null ? inst.Definition.Portrait : null;

            var gains = inst.ApplyLevelUp(RollRandom);
            inst.ConsumeExpForLevelUp();

            GameStateManager.Instance?.RequestTransition(GameState.LevelUpScreen, nameof(ExpGranter));

            if (_levelUpScreen != null)
                yield return _levelUpScreen.Play(unit, preStats, gains, preLevel, portrait);

            GameStateManager.Instance?.RequestTransition(GameState.BattleMap, nameof(ExpGranter));
        }
    }
}
