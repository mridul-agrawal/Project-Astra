using System;
using System.Collections.Generic;
using UnityEngine;

namespace ProjectAstra.Core.Progression
{
    [Serializable]
    public struct ResolvedCommitment
    {
        public string commitmentId;
        public string commitmentText;
        public CommitmentResolution resolution;
        public int chapterResolved;
    }

    [Serializable]
    public struct CommitmentTrackerDto
    {
        public string[] activeIds;
        public ResolvedCommitment[] resolutionLog;
    }

    /// <summary>
    /// Session-scoped tracker. One live entry per active Commitment, with a
    /// runtime resolution state and the chapter it was resolved in. Ledger's
    /// Middle Column reads `ResolvedThisChapter()` at end-of-chapter.
    /// </summary>
    public class CommitmentTracker : MonoBehaviour, IPersistable<CommitmentTrackerDto>
    {
        public static CommitmentTracker Instance { get; private set; }

        private class LiveCommitment
        {
            public Commitment Asset;
            public CommitmentResolution State;
            public int ChapterResolved;   // 0 while PENDING
        }

        private readonly Dictionary<string, LiveCommitment> _active = new();
        private readonly List<ResolvedCommitment> _resolutionLog = new();

        private void Awake()
        {
            if (Instance != null && Instance != this) Destroy(Instance.gameObject);
            Instance = this;
        }

        private void OnDestroy()
        {
            if (Instance == this) Instance = null;
        }

        /// <summary>Called by a scene-level CommitmentSet on chapter start.</summary>
        public void Register(IEnumerable<Commitment> commitments)
        {
            if (commitments == null) return;
            foreach (var c in commitments)
            {
                if (c == null || string.IsNullOrEmpty(c.Id)) continue;
                if (_active.ContainsKey(c.Id)) continue;
                _active[c.Id] = new LiveCommitment { Asset = c, State = CommitmentResolution.Pending };
            }
        }

        public void Resolve(string commitmentId, CommitmentResolution outcome)
        {
            if (outcome == CommitmentResolution.Pending) return;
            if (!_active.TryGetValue(commitmentId, out var live)) return;
            if (live.State != CommitmentResolution.Pending) return; // resolution is write-once

            live.State = outcome;
            live.ChapterResolved = ChapterContext.CurrentChapterNumber;
            _resolutionLog.Add(new ResolvedCommitment {
                commitmentId   = live.Asset.Id,
                commitmentText = live.Asset.Text,
                resolution     = outcome,
                chapterResolved= live.ChapterResolved,
            });
        }

        public IReadOnlyList<ResolvedCommitment> ResolvedThisChapter()
        {
            int ch = ChapterContext.CurrentChapterNumber;
            var filtered = new List<ResolvedCommitment>();
            foreach (var r in _resolutionLog) if (r.chapterResolved == ch) filtered.Add(r);
            return filtered;
        }

        public IReadOnlyList<ResolvedCommitment> AllResolved() => _resolutionLog;

        public bool AnyPendingActive()
        {
            foreach (var kv in _active) if (kv.Value.State == CommitmentResolution.Pending) return true;
            return false;
        }

        public CommitmentTrackerDto Serialize()
        {
            var ids = new List<string>(_active.Keys);
            return new CommitmentTrackerDto {
                activeIds = ids.ToArray(),
                resolutionLog = _resolutionLog.ToArray(),
            };
        }

        public void Restore(CommitmentTrackerDto dto)
        {
            _resolutionLog.Clear();
            if (dto.resolutionLog != null) _resolutionLog.AddRange(dto.resolutionLog);
            // activeIds need the Commitment assets to be re-resolved by the caller
            // since dictionary values hold ScriptableObject refs. Out of scope today.
        }
    }

    /// <summary>
    /// Scene-level collection. Drop on a chapter root in BattleMap.unity; on
    /// Start it pushes its commitments into CommitmentTracker.
    /// </summary>
    public class CommitmentSet : MonoBehaviour
    {
        [SerializeField] private Commitment[] _commitments;
        [SerializeField] private MonoBehaviour[] _evaluators;   // must each implement IChapterOutcomeEvaluator

        private void Start()
        {
            if (CommitmentTracker.Instance == null) return;
            CommitmentTracker.Instance.Register(_commitments);
        }

        public void RunEvaluators(BattleConclusion conclusion)
        {
            if (_evaluators == null) return;
            foreach (var raw in _evaluators)
            {
                if (raw is IChapterOutcomeEvaluator evaluator)
                    evaluator.Evaluate(conclusion, CommitmentTracker.Instance);
            }
        }
    }

    /// <summary>
    /// Implement on any MonoBehaviour the chapter author wants to run at
    /// end-of-battle. CommitmentSet invokes each registered evaluator on
    /// BattleConcluded; evaluators call CommitmentTracker.Resolve(...) as
    /// appropriate.
    /// </summary>
    public interface IChapterOutcomeEvaluator
    {
        void Evaluate(BattleConclusion conclusion, CommitmentTracker tracker);
    }
}
