using System.Collections.Generic;
using UnityEngine;

namespace ProjectAstra.Core.Flow
{
    // The whole game's running order, authored by a designer in the inspector. GameFlow reads it
    // top-to-bottom, one step at a time. Add or reorder beats here — no code change needed.
    [CreateAssetMenu(fileName = "Campaign", menuName = "Project Astra/Core/Campaign")]
    public class Campaign : ScriptableObject
    {
        [SerializeField] private List<CampaignStep> steps = new();

        public int Count => steps.Count;

        public CampaignStep StepAt(int index) => (index >= 0 && index < steps.Count) ? steps[index] : null;

        // Used when the battle map is entered without the campaign having been walked to it —
        // pressing Play on the scene, or a dev boot that skips the intro.
        public int IndexOfFirstBattle()
        {
            for (int i = 0; i < steps.Count; i++)
                if (steps[i] != null && steps[i].Kind == CampaignStepKind.Battle) return i;
            return -1;
        }
    }
}
