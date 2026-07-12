using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using ProjectAstra.Core.Combat;
using ProjectAstra.Core.Events;
using ProjectAstra.Core.Input;
using ProjectAstra.Core.Progression;
using ProjectAstra.Core.State;

namespace ProjectAstra.Core.UI.WarLedger
{
    // War's Ledger runtime controller. Subscribes to game-state changes via EventService;
    // on entering WarLedger it reads DeathRegistry / CommitmentTracker /
    // ICivilianThreadService / IDeathEpitaphProvider and populates the prefab.
    // Dismissed on Confirm — fires a transition to ChapterClear.
    //
    // Spec invariants enforced here:
    //   • The Ledger grants no stats / XP / gold / karma. Purely informational.
    //   • "Kept." and "Not kept." render in the same typography; the only
    //     visual difference is a vermillion underline under "Kept." (baked
    //     into the Middle entry template as a child that the runtime
    //     shows/hides).
    //   • The right column may be empty (civilian thread stub) — render the
    //     header, no row spam.
    public class WarLedgerUI : MonoBehaviour
    {
        public static bool HasInputFocus { get; private set; }

        [SerializeField] private GameObject popupInstance;

        private WarLedgerRefs refs;
        private bool subscribed;

        private void Awake()
        {
            if (EventService.Instance != null)
            {
                EventService.Instance.SubscribeGameStateChanged(OnStateChanged);
                subscribed = true;
            }
        }

        private void OnDestroy()
        {
            if (subscribed && EventService.Instance != null)
                EventService.Instance.UnsubscribeGameStateChanged(OnStateChanged);
            if (HasInputFocus) Hide();
        }

        private void OnStateChanged(StateChangeArgs args)
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
            if (popupInstance != null) popupInstance.SetActive(false);
        }

        // ============================================================
        // Population passes
        // ============================================================

        private bool ActivateUI()
        {
            if (popupInstance == null)
            {
                Debug.LogError("WarLedgerUI: _popupInstance not wired. Run scene setup or rebuild the prefab.");
                return false;
            }
            if (refs == null) refs = popupInstance.GetComponent<WarLedgerRefs>();
            if (refs == null)
            {
                Debug.LogError("WarLedgerUI: prefab missing WarLedgerRefs.");
                return false;
            }
            popupInstance.SetActive(true);
            popupInstance.transform.SetAsLastSibling();
            return true;
        }

        private void PopulateChapterMeta()
        {
            if (refs.chapterEyebrow != null) refs.chapterEyebrow.text = "CHAPTER";
            if (refs.chapterNumber != null)  refs.chapterNumber.text  = ChapterContext.CurrentChapterNumber.ToString("D2");
            if (refs.chapterTitle != null)
            {
                var title = ChapterContext.CurrentChapterTitle;
                refs.chapterTitle.gameObject.SetActive(!string.IsNullOrEmpty(title));
                refs.chapterTitle.text = title ?? "";
            }
        }

        private void PopulateLeftColumn()
        {
            if (refs.leftEntriesContainer == null || refs.leftEntryTemplate == null) return;

            ClearDynamicChildren(refs.leftEntriesContainer, refs.leftEntryTemplate);

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
            if (refs.leftUnnamedTail != null)
            {
                if (unnamed > 0)
                {
                    refs.leftUnnamedTail.text =
                        $"And {unnamed} other{(unnamed == 1 ? "" : "s")} whose names are not recorded.";
                    refs.leftUnnamedTail.gameObject.SetActive(true);
                }
                else
                {
                    refs.leftUnnamedTail.gameObject.SetActive(false);
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
            var go = Instantiate(refs.leftEntryTemplate, refs.leftEntriesContainer);
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
            if (refs.middleEntriesContainer == null || refs.middleEntryTemplate == null) return;
            ClearDynamicChildren(refs.middleEntriesContainer, refs.middleEntryTemplate);

            var tracker = CommitmentTracker.Instance;
            if (tracker == null) return;
            var resolved = tracker.ResolvedThisChapter();
            for (int i = 0; i < resolved.Count; i++)
            {
                var r = resolved[i];
                var go = Instantiate(refs.middleEntryTemplate, refs.middleEntriesContainer);
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
            if (refs.rightEntriesContainer == null || refs.rightEntryTemplate == null) return;
            ClearDynamicChildren(refs.rightEntriesContainer, refs.rightEntryTemplate);

            var civilian = WarLedgerServices.CivilianThreadService ?? NullCivilianThreadService.Instance;
            var entries = civilian.ForCurrentChapter();

            foreach (var e in entries)
            {
                var go = Instantiate(refs.rightEntryTemplate, refs.rightEntriesContainer);
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
