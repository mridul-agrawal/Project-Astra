using System;
using UnityEngine;
using ProjectAstra.Core.Dialogue;
using ProjectAstra.Core.Grid;

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
        [SerializeField] private MapId map;

        public CampaignStepKind Kind => kind;
        public CutsceneId Cutscene => cutscene;
        public MapId Map => map;
    }
}
