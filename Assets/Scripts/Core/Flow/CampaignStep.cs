using System;
using UnityEngine;
using ProjectAstra.Core.Dialogue;
using ProjectAstra.Core.Grid;

namespace ProjectAstra.Core.Flow
{
    // The kind of beat a campaign step is. The label decides which id below is meaningful.
    public enum CampaignStepKind { Cutscene, Battle }

    // One beat in the campaign playlist: a labelled envelope carrying just the id its kind needs.
    // A Cutscene step uses 'cutscene'; a Battle step uses 'map'. A custom inspector drawer shows
    // only the field that matches the selected kind. Both id fields are enums, so the designer
    // picks from a dropdown — no strings to mistype.
    [Serializable]
    public class CampaignStep
    {
        [SerializeField] private CampaignStepKind kind;
        [SerializeField] private CutsceneId cutscene;
        [SerializeField] private MapId map;

        public CampaignStepKind Kind => kind;
        public CutsceneId Cutscene => cutscene;
        public MapId Map => map;
    }
}
