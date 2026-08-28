using System;
using UnityEngine;
using ProjectAstra.Core.Dialogue;

namespace ProjectAstra.Core.Flow
{
    // Append only — Campaign.asset stores the kind as an int, so reordering rewrites every
    // authored step into something else.
    public enum CampaignStepKind
    {
        Cutscene,
        Battle,
        HubVisit
    }

    [Serializable]
    public class CampaignStep
    {
        [SerializeField] private CampaignStepKind kind;
        [SerializeField] private CutsceneId cutscene;
        [SerializeField] private string mapId;
        [SerializeField] private string visitId;

        public CampaignStepKind Kind => kind;
        public CutsceneId Cutscene => cutscene;
        public string MapId => mapId;
        public string VisitId => visitId;
    }
}
