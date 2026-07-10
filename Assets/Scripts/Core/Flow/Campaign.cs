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

        // The step at this index, or null when the index has run past the end of the campaign.
        public CampaignStep StepAt(int index) => (index >= 0 && index < steps.Count) ? steps[index] : null;
    }
}
