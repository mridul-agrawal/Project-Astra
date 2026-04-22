using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace ProjectAstra.Core.UI
{
    /// <summary>
    /// Ref-holder on the WarLedger prefab root. WarLedgerUI reads + mutates
    /// these at runtime to populate the three columns. Entry rows are
    /// cloned from templates (prefab children) so the hierarchy can flex with
    /// any number of entries per column.
    /// </summary>
    public class WarLedgerRefs : MonoBehaviour
    {
        [Header("Parchment chrome")]
        public Image parchmentSheet;          // the baked 1680x952 background

        [Header("Chapter meta")]
        public TextMeshProUGUI chapterEyebrow; // "CHAPTER"
        public TextMeshProUGUI chapterNumber;  // "04"
        public TextMeshProUGUI chapterTitle;

        [Header("Left column — Those Who Fell")]
        public TextMeshProUGUI leftHeadDeva;
        public TextMeshProUGUI leftHeadEn;
        public RectTransform leftEntriesContainer;
        public GameObject leftEntryTemplate;      // disabled template with Name + Epitaph children
        public TextMeshProUGUI leftUnnamedTail;

        [Header("Middle column — What Was Kept and What Was Not")]
        public TextMeshProUGUI middleHeadDeva;
        public TextMeshProUGUI middleHeadEn;
        public RectTransform middleEntriesContainer;
        public GameObject middleEntryTemplate;    // disabled template with CommitText + Resolution children + KeptRule

        [Header("Right column — The Living")]
        public TextMeshProUGUI rightHeadDeva;
        public TextMeshProUGUI rightHeadEn;
        public RectTransform rightEntriesContainer;
        public GameObject rightEntryTemplate;     // disabled template with Name+State + optional Note

        [Header("Footer")]
        public TextMeshProUGUI footerContinue;
    }
}
