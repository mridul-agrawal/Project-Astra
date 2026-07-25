using UnityEngine;
using UnityEngine.UI;
using TMPro;

namespace ProjectAstra.Core.UI.UnitInfo
{
    // Left-column summary — portrait, name, class, level, HP, EXP, status.
    // Shared by both tabs. Passive: one Render(model), no logic.
    public sealed class UnitSummaryView : MonoBehaviour
    {
        [Header("Identity")]
        public Image Portrait;
        public TextMeshProUGUI UnitName;
        public TextMeshProUGUI ClassLabel;
        public Image ClassIcon;
        public TextMeshProUGUI Level;

        [Header("HP")]
        public TextMeshProUGUI HpValue;
        public Image HpFill;

        [Header("EXP")]
        public GameObject ExpRow;
        public TextMeshProUGUI ExpValue;
        public Image ExpFill;

        [Header("Status")]
        public GameObject StatusRow;
        public TextMeshProUGUI StatusText;

        public void Render(UnitSummaryModel m)
        {
            if (m == null) return;
            if (UnitName != null) UnitName.text = m.UnitName;
            if (Portrait != null && m.Portrait != null) Portrait.sprite = m.Portrait;
            if (ClassLabel != null) ClassLabel.text = m.ClassName;
            if (ClassIcon != null && m.ClassIcon != null) ClassIcon.sprite = m.ClassIcon;
            if (Level != null) Level.text = "Lv " + m.Level;
            if (HpValue != null) HpValue.text = m.CurrentHP + " / " + m.MaxHP;
            if (HpFill != null) HpFill.fillAmount = m.HpFraction;

            if (ExpRow != null) ExpRow.SetActive(m.ShowExp);
            if (m.ShowExp)
            {
                if (ExpValue != null) ExpValue.text = m.ExpText;
                if (ExpFill != null) ExpFill.fillAmount = m.ExpFraction;
            }

            if (StatusRow != null) StatusRow.SetActive(m.ShowStatus);
            if (m.ShowStatus && StatusText != null) StatusText.text = m.StatusText;
        }
    }
}
