using System;
using UnityEngine;
using ProjectAstra.Core.Dialogue;

namespace ProjectAstra.Core.Flow
{
    public enum CampaignStepKind 
    { 
        Cutscene, 
        Battle 
    }

    [Serializable]
    public class CampaignStep
    {
        [SerializeField] private CampaignStepKind kind;
        [SerializeField] private CutsceneId cutscene;
        [SerializeField] private string mapId;

        public CampaignStepKind Kind => kind;
        public CutsceneId Cutscene => cutscene;
        public string MapId => mapId;
    }
}
