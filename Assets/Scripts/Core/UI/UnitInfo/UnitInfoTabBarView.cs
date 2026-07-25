using UnityEngine;

namespace ProjectAstra.Core.UI.UnitInfo
{
    // The STATS / GEAR tab bar — swaps active/inactive chrome for whichever tab is current.
    public sealed class UnitInfoTabBarView : MonoBehaviour
    {
        public GameObject StatsActive;
        public GameObject StatsInactive;
        public GameObject GearActive;
        public GameObject GearInactive;

        public void Render(UnitInfoTab active)
        {
            bool stats = active == UnitInfoTab.Stats;
            if (StatsActive != null) StatsActive.SetActive(stats);
            if (StatsInactive != null) StatsInactive.SetActive(!stats);
            if (GearActive != null) GearActive.SetActive(!stats);
            if (GearInactive != null) GearInactive.SetActive(stats);
        }
    }
}
