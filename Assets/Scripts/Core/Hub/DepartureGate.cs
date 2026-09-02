namespace ProjectAstra.Core.Hub
{
    // Decides whether a visit is allowed to leave for its battle, and says why not when it isn't.
    //
    // Pure C# so the rules test on their own. Two of them matter:
    //
    //   Nothing departs early. Every authored objective has to be finished first — that is the one
    //   hard rule the spec states about leaving a visit.
    //
    //   The destination comes from the visit, and the campaign has to agree with it. The spec is
    //   explicit that the next battle is authored content and must never be worked out as "the map
    //   after this one", so a disagreement between the two is a content error rather than a guess.
    public static class DepartureGate
    {
        public static bool CanDepart(bool objectivesComplete, string visitDestination,
            string nextCampaignMapId, out string problem)
        {
            if (!objectivesComplete)
            {
                problem = "not every objective in this visit is finished";
                return false;
            }

            if (string.IsNullOrEmpty(visitDestination))
            {
                problem = "the visit does not name a battle to depart to";
                return false;
            }

            if (string.IsNullOrEmpty(nextCampaignMapId))
            {
                problem = $"the campaign has no battle after this visit, but it departs to '{visitDestination}'";
                return false;
            }

            if (nextCampaignMapId != visitDestination)
            {
                problem = $"the visit departs to '{visitDestination}' but the campaign goes to '{nextCampaignMapId}'";
                return false;
            }

            problem = null;
            return true;
        }
    }
}
