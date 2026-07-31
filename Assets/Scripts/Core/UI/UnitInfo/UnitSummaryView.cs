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

        public void Render(UnitSummaryModel model)
        {
            if (model == null) return;
            if (UnitName != null) UnitName.text = model.UnitName;
            if (Portrait != null && model.Portrait != null) Portrait.sprite = model.Portrait;
            if (ClassLabel != null) ClassLabel.text = model.ClassName;
            if (ClassIcon != null && model.ClassIcon != null) ClassIcon.sprite = model.ClassIcon;
            if (Level != null) Level.text = "Lv " + model.Level;
            if (HpValue != null) HpValue.text = model.CurrentHP + " / " + model.MaxHP;
            if (HpFill != null) HpFill.fillAmount = model.HpFraction;

            if (ExpRow != null) ExpRow.SetActive(model.ShowExp);
            if (model.ShowExp)
            {
                if (ExpValue != null) ExpValue.text = model.ExpText;
                if (ExpFill != null) ExpFill.fillAmount = model.ExpFraction;
            }

            if (StatusRow != null) StatusRow.SetActive(model.ShowStatus);
            if (model.ShowStatus && StatusText != null) StatusText.text = model.StatusText;
        }
    }
}
