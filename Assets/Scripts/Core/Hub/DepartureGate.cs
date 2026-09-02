namespace ProjectAstra.Core.Hub
{
    // Decides whether a visit may leave for its battle, and says why not when it can't.
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
