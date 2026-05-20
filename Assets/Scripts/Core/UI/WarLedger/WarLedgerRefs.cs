using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace ProjectAstra.Core.UI.WarLedger
{
    // Ref-holder attached to the WarLedger prefab root. WarLedgerUI reads
    // and mutates these at runtime to populate the three columns. Entry rows
    // are cloned from disabled templates (prefab children) so the hierarchy
    // can flex with any number of entries per column.
    public class WarLedgerRefs : MonoBehaviour
    {
        [Header("Parchment chrome")]
        public Image parchmentSheet;          // baked 1680×952 background

        [Header("Chapter meta")]
        public TextMeshProUGUI chapterEyebrow; // "CHAPTER"
        public TextMeshProUGUI chapterNumber;  // "04"
        public TextMeshProUGUI chapterTitle;

        [Header("Left column — Those Who Fell")]
        public TextMeshProUGUI leftHeadDeva;
        public TextMeshProUGUI leftHeadEn;
        public RectTransform leftEntriesContainer;
        public GameObject leftEntryTemplate;      // disabled template (Name + Epitaph)
        public TextMeshProUGUI leftUnnamedTail;

        [Header("Middle column — What Was Kept and What Was Not")]
        public TextMeshProUGUI middleHeadDeva;
        public TextMeshProUGUI middleHeadEn;
        public RectTransform middleEntriesContainer;
        public GameObject middleEntryTemplate;    // disabled template (CommitText + Resolution + KeptRule)

        [Header("Right column — The Living")]
        public TextMeshProUGUI rightHeadDeva;
        public TextMeshProUGUI rightHeadEn;
        public RectTransform rightEntriesContainer;
        public GameObject rightEntryTemplate;     // disabled template (Name + State + optional Note)

        [Header("Footer")]
        public TextMeshProUGUI footerContinue;
    }
}
