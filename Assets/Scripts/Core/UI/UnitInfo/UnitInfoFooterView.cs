using UnityEngine;
using UnityEngine.UI;
using TMPro;

namespace ProjectAstra.Core.UI.UnitInfo
{
    // Footer description line — icon + name + description of the selected row.
    // §7: line one is the name and its summary, line two the longer detail.
    public sealed class UnitInfoFooterView : MonoBehaviour
    {
        public Image Icon;
        public TextMeshProUGUI Title;
        public TextMeshProUGUI Description;
        public TextMeshProUGUI Detail;

        public void Render(UnitInfoFooterModel m)
        {
            if (m == null) return;
            if (Icon != null)
            {
                if (m.Icon != null) Icon.sprite = m.Icon;
                Icon.enabled = m.Icon != null;
            }
            if (Title != null) Title.text = m.Title;
            if (Description != null) Description.text = Dashed(m.Description);
            if (Detail != null) Detail.text = m.Detail;
        }

        // The em-dash belongs to the summary, so it disappears along with it.
        private static string Dashed(string description) =>
            string.IsNullOrEmpty(description) ? "" : "— " + description;
    }
}
