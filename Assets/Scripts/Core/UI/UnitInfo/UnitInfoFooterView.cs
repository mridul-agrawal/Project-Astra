using UnityEngine;
using UnityEngine.UI;
using TMPro;

namespace ProjectAstra.Core.UI.UnitInfo
{
    // Footer description line — icon + name + description of the selected row.
    public sealed class UnitInfoFooterView : MonoBehaviour
    {
        public Image Icon;
        public TextMeshProUGUI Title;
        public TextMeshProUGUI Description;

        public void Render(UnitInfoFooterModel m)
        {
            if (m == null) return;
            if (Icon != null)
            {
                if (m.Icon != null) Icon.sprite = m.Icon;
                Icon.enabled = m.Icon != null;
            }
            if (Title != null) Title.text = m.Title;
            if (Description != null) Description.text = m.Description;
        }
    }
}
