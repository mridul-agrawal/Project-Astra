using UnityEngine;
using TMPro;

namespace ProjectAstra.Core.UI.UnitInfo
{
    // Hold-to-inspect detail overlay. Minimal for now — enlarges the selected
    // stat/item name + description; richer breakdowns come later.
    public sealed class UnitInspectView : MonoBehaviour
    {
        public GameObject Root;
        public TextMeshProUGUI Title;
        public TextMeshProUGUI Body;

        public void Show(string title, string body)
        {
            if (Title != null) Title.text = title;
            if (Body != null) Body.text = body;
            if (Root != null) Root.SetActive(true);
        }

        public void Hide() { if (Root != null) Root.SetActive(false); }
    }
}
