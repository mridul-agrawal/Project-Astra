using System;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

namespace ProjectAstra.Core.UI.UnitInfo
{
    // STATS tab — the equipped-weapon panel + nine stat rows (fixed, pre-placed).
    public sealed class UnitStatsView : MonoBehaviour
    {
        [Serializable]
        public struct StatRowWidgets
        {
            public Image Icon;
            public TextMeshProUGUI Label;
            public TextMeshProUGUI Value;
            public TextMeshProUGUI Cap;
            public Image Fill;
            public GameObject Highlight;
            public RectTransform RowRoot;       // what the §8 focus marker sizes itself to
        }

        [Header("Tab root")]
        public GameObject Root;

        [Header("Equipped weapon")]
        public GameObject WeaponPanel;
        public TextMeshProUGUI WeaponName;
        public TextMeshProUGUI WeaponTypeLabel;
        public Image WeaponIcon;
        public TextMeshProUGUI Mt, Hit, Crt, Rng;
        public GameObject WeaponHighlight;      // §5 makes the card the first focus stop

        [Header("Stat rows (STR…MOVE, 9)")]
        public StatRowWidgets[] Rows;

        public void SetTabActive(bool active) { if (Root != null) Root.SetActive(active); }

        public void Render(UnitStatsModel m)
        {
            if (m == null) return;
            RenderWeapon(m.Weapon);
            if (Rows == null || m.Rows == null) return;
            for (int i = 0; i < Rows.Length && i < m.Rows.Length; i++)
                RenderRow(Rows[i], m.Rows[i]);
        }

        // A row index of -1 means focus is parked on the weapon card instead.
        public void SetSelected(int index)
        {
            if (Rows == null) return;
            for (int i = 0; i < Rows.Length; i++)
                if (Rows[i].Highlight != null) Rows[i].Highlight.SetActive(i == index);
        }

        public void SetWeaponSelected(bool selected)
        {
            if (WeaponHighlight != null) WeaponHighlight.SetActive(selected);
        }

        public RectTransform WeaponFocusRect =>
            WeaponPanel != null ? WeaponPanel.GetComponent<RectTransform>() : null;

        public RectTransform FocusRectFor(int index) =>
            Rows != null && index >= 0 && index < Rows.Length ? Rows[index].RowRoot : null;

        // §8 flips the focused row's label to white and leaves the rest muted.
        public void ApplyLabelEmphasis(int index, Color focused, Color resting)
        {
            if (Rows == null) return;
            for (int i = 0; i < Rows.Length; i++)
                if (Rows[i].Label != null) Rows[i].Label.color = i == index ? focused : resting;
        }

        private void RenderWeapon(EquippedWeaponVM w)
        {
            if (WeaponPanel != null) WeaponPanel.SetActive(w != null && w.HasWeapon);
            if (w == null || !w.HasWeapon) return;
            if (WeaponName != null) WeaponName.text = w.Name;
            if (WeaponTypeLabel != null) WeaponTypeLabel.text = w.WeaponType.ToString().ToUpper();
            if (WeaponIcon != null && w.Sigil != null) WeaponIcon.sprite = w.Sigil;
            if (Mt != null) Mt.text = w.Mt.ToString();
            if (Hit != null) Hit.text = w.Hit.ToString();
            if (Crt != null) Crt.text = w.Crt.ToString();
            if (Rng != null) Rng.text = w.RangeText;
        }

        private static void RenderRow(StatRowWidgets ui, StatRowVM r)
        {
            if (ui.Label != null) ui.Label.text = r.Label;
            if (ui.Value != null) ui.Value.text = r.Value.ToString();
            if (ui.Cap != null) ui.Cap.text = r.Cap.ToString();
            if (ui.Icon != null && r.Icon != null) ui.Icon.sprite = r.Icon;
            if (ui.Fill != null) ui.Fill.fillAmount = r.Fraction;
        }
    }
}
