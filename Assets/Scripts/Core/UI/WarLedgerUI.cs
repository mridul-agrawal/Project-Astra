using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using ProjectAstra.Core.Progression;

namespace ProjectAstra.Core.UI
{
    /// <summary>
    /// War's Ledger runtime controller. Subscribes to GameStateEventChannel;
    /// when the state enters WarLedger it reads DeathRegistry / CommitmentTracker
    /// / ICivilianThreadService / IDeathEpitaphProvider and populates the prefab.
    /// Dismissed on Confirm — fires a transition to ChapterClear.
    ///
    /// Spec-reminders enforced here:
    ///   - Ledger grants NO stats / XP / gold / karma. Purely informational.
    ///   - Kept and "Not kept." render in the same typography; the only visual
    ///     distinction is a vermillion underline under "Kept." (baked into the
    ///     Middle entry template as a child that the runtime shows/hides).
    ///   - Right column may be empty (civilian thread stub) — render the header,
    ///     no row spam.
    /// </summary>
    public class WarLedgerUI : MonoBehaviour
    {
        public static bool HasInputFocus { get; private set; }

        [SerializeField] private GameObject _popupInstance;
        [SerializeField] private GameStateEventChannel _stateChannel;

        private WarLedgerRefs _refs;
        private bool _subscribed;

        private void Awake()
        {
            if (_stateChannel != null)
            {
                _stateChannel.Register(OnStateChanged);
                _subscribed = true;
            }
        }

        private void OnDestroy()
        {
            if (_subscribed && _stateChannel != null) _stateChannel.Unregister(OnStateChanged);
            if (HasInputFocus) Hide();
        }

        private void OnStateChanged(GameStateEventChannel.StateChangeArgs args)
        {
            if (args.NewState == GameState.WarLedger) Show();
            else if (args.PreviousState == GameState.WarLedger) Hide();
        }

        public void Show()
        {
            if (!ActivateUI()) return;

            PopulateChapterMeta();
            PopulateLeftColumn();
            PopulateMiddleColumn();
            PopulateRightColumn();

            HasInputFocus = true;
            if (InputManager.Instance != null)
                InputManager.Instance.OnConfirm += OnConfirm;
        }

        public void Hide()
        {
            HasInputFocus = false;
            if (InputManager.Instance != null)
                InputManager.Instance.OnConfirm -= OnConfirm;
            if (_popupInstance != null) _popupInstance.SetActive(false);
        }

        // ============================================================
        // Population passes
        // ============================================================

        private bool ActivateUI()
        {
            if (_popupInstance == null)
            {
                Debug.LogError("WarLedgerUI: _popupInstance not wired. Run scene setup or rebuild the prefab.");
                return false;
            }
            if (_refs == null) _refs = _popupInstance.GetComponent<WarLedgerRefs>();
            if (_refs == null)
            {
                Debug.LogError("WarLedgerUI: prefab missing WarLedgerRefs.");
                return false;
            }
            _popupInstance.SetActive(true);
            _popupInstance.transform.SetAsLastSibling();
            return true;
        }

        private void PopulateChapterMeta()
        {
            if (_refs.chapterEyebrow != null) _refs.chapterEyebrow.text = "CHAPTER";
            if (_refs.chapterNumber != null)  _refs.chapterNumber.text  = ChapterContext.CurrentChapterNumber.ToString("D2");
            if (_refs.chapterTitle != null)
            {
                var title = ChapterContext.CurrentChapterTitle;
                _refs.chapterTitle.gameObject.SetActive(!string.IsNullOrEmpty(title));
                _refs.chapterTitle.text = title ?? "";
            }
        }

        private void PopulateLeftColumn()
        {
            if (_refs.leftEntriesContainer == null || _refs.leftEntryTemplate == null) return;

            ClearDynamicChildren(_refs.leftEntriesContainer, _refs.leftEntryTemplate);

            var registry = DeathRegistry.Instance;
            if (registry != null)
            {
                // Collect named entries for this chapter, ordered player → named enemy → civilian.
                var entries = new List<DeathEntry>();
                foreach (var e in registry.ForCurrentChapter())
                    if (e.IsNamed) entries.Add(e);
                entries.Sort((a, b) => FactionOrder(a.faction) - FactionOrder(b.faction));

                foreach (var e in entries) AddLeftEntry(e);
            }

            // Unnamed tail
            int unnamed = registry?.UnnamedEnemyDeathCountForCurrentChapter() ?? 0;
            if (_refs.leftUnnamedTail != null)
            {
                if (unnamed > 0)
                {
                    _refs.leftUnnamedTail.text =
                        $"And {unnamed} other{(unnamed == 1 ? "" : "s")} whose names are not recorded.";
                    _refs.leftUnnamedTail.gameObject.SetActive(true);
                }
                else
                {
                    _refs.leftUnnamedTail.gameObject.SetActive(false);
                }
            }
        }

        private static int FactionOrder(DeathFaction f) => f switch
        {
            DeathFaction.Player      => 0,
            DeathFaction.EnemyNamed  => 1,
            DeathFaction.Civilian    => 2,
            _                        => 3,
        };

        private void AddLeftEntry(DeathEntry entry)
        {
            var go = Instantiate(_refs.leftEntryTemplate, _refs.leftEntriesContainer);
            go.name = "LeftEntry_" + (entry.unitName ?? entry.unitId);
            go.SetActive(true);

            var nameTmp = go.transform.Find("Name")?.GetComponent<TextMeshProUGUI>();
            if (nameTmp != null)
            {
                var nameText = entry.unitName;
                if (!string.IsNullOrEmpty(entry.className) && entry.faction != DeathFaction.Civilian)
                    nameText += ", " + entry.className;
                nameTmp.text = nameText;
            }

            var epitTmp = go.transform.Find("Epitaph")?.GetComponent<TextMeshProUGUI>();
            if (epitTmp != null) epitTmp.text = entry.epitaph ?? "";
        }

        private void PopulateMiddleColumn()
        {
            if (_refs.middleEntriesContainer == null || _refs.middleEntryTemplate == null) return;
            ClearDynamicChildren(_refs.middleEntriesContainer, _refs.middleEntryTemplate);

            var tracker = CommitmentTracker.Instance;
            if (tracker == null) return;
            var resolved = tracker.ResolvedThisChapter();
            for (int i = 0; i < resolved.Count; i++)
            {
                var r = resolved[i];
                var go = Instantiate(_refs.middleEntryTemplate, _refs.middleEntriesContainer);
                go.name = "MiddleEntry_" + r.commitmentId;
                go.SetActive(true);

                var text = go.transform.Find("CommitText")?.GetComponent<TextMeshProUGUI>();
                if (text != null) text.text = r.commitmentText;

                var res = go.transform.Find("Resolution")?.GetComponent<TextMeshProUGUI>();
                if (res != null) res.text = r.resolution == CommitmentResolution.Kept ? "Kept." : "Not kept.";

                var rule = go.transform.Find("KeptRule")?.gameObject;
                if (rule != null) rule.SetActive(r.resolution == CommitmentResolution.Kept);

                var sep = go.transform.Find("Sep")?.gameObject;
                if (sep != null) sep.SetActive(i < resolved.Count - 1);
            }
        }

        private void PopulateRightColumn()
        {
            if (_refs.rightEntriesContainer == null || _refs.rightEntryTemplate == null) return;
            ClearDynamicChildren(_refs.rightEntriesContainer, _refs.rightEntryTemplate);

            var civilian = WarLedgerServices.CivilianThreadService ?? NullCivilianThreadService.Instance;
            var entries = civilian.ForCurrentChapter();

            foreach (var e in entries)
            {
                var go = Instantiate(_refs.rightEntryTemplate, _refs.rightEntriesContainer);
                go.name = "RightEntry_" + e.civilianName;
                go.SetActive(true);

                var nameState = go.transform.Find("NameState")?.GetComponent<TextMeshProUGUI>();
                if (nameState != null)
                {
                    nameState.text = e.civilianName + "  \u00B7 " + StatusLabel(e.status) + ".";
                }

                var note = go.transform.Find("Note")?.GetComponent<TextMeshProUGUI>();
                if (note != null)
                {
                    if (!string.IsNullOrEmpty(e.statusNote))
                    {
                        note.text = e.statusNote;
                        note.gameObject.SetActive(true);
                    }
                    else note.gameObject.SetActive(false);
                }
            }
        }

        private static string StatusLabel(CivilianStatus s) => s switch
        {
            CivilianStatus.Safe      => "Safe",
            CivilianStatus.Displaced => "Displaced",
            CivilianStatus.Lost      => "Lost",
            _                        => "",
        };

        // Walks a container, deactivates every instantiated child except the template
        // GameObject itself. The template stays inactive and gets cloned on each
        // population pass.
        private static void ClearDynamicChildren(RectTransform container, GameObject template)
        {
            var toDelete = new List<GameObject>();
            foreach (Transform child in container)
            {
                if (child.gameObject != template) toDelete.Add(child.gameObject);
            }
            foreach (var go in toDelete)
            {
#if UNITY_EDITOR
                if (!Application.isPlaying) DestroyImmediate(go);
                else Destroy(go);
#else
                Destroy(go);
#endif
            }
        }

        // ============================================================
        // Input
        // ============================================================

        private void OnConfirm()
        {
            if (!HasInputFocus) return;
            Hide();
            GameStateManager.Instance?.RequestTransition(GameState.ChapterClear);
        }
    }
}
