using System;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

namespace ProjectAstra.Core.UI.UnitInfo
{
    // GEAR tab — five fixed inventory slots (icon, name, weapon chips, uses).
    public sealed class UnitGearView : MonoBehaviour
    {
        [Serializable]
        public struct GearSlotWidgets
        {
            public TextMeshProUGUI IndexLabel;
            public Image Icon;
            public TextMeshProUGUI Name;
            public GameObject Chips;          // weapon chip row (hidden for consumables/empty)
            public TextMeshProUGUI TypeBadge;
            public TextMeshProUGUI Mt, Hit, Crt, Rng;
            public TextMeshProUGUI Uses;
            public GameObject Highlight;
            public GameObject EquippedMark;
            public GameObject EmptyState;
        }

        [Header("Tab root")]
        public GameObject Root;

        [Header("Gear slots (5)")]
        public GearSlotWidgets[] Slots;

        public void SetTabActive(bool active) { if (Root != null) Root.SetActive(active); }

        public void Render(UnitGearModel m)
        {
            if (m == null || Slots == null || m.Slots == null) return;
            for (int i = 0; i < Slots.Length && i < m.Slots.Length; i++)
                RenderSlot(Slots[i], m.Slots[i]);
        }

        public void SetSelected(int index)
        {
            if (Slots == null) return;
            for (int i = 0; i < Slots.Length; i++)
                if (Slots[i].Highlight != null) Slots[i].Highlight.SetActive(i == index);
        }

        private static void RenderSlot(GearSlotWidgets ui, GearSlotVM s)
        {
            if (ui.IndexLabel != null) ui.IndexLabel.text = s.Index.ToString();
            if (ui.EmptyState != null) ui.EmptyState.SetActive(s.IsEmpty);
            if (ui.EquippedMark != null) ui.EquippedMark.SetActive(s.IsEquipped);
            if (ui.Name != null) ui.Name.text = s.IsEmpty ? "EMPTY" : s.Name;
            if (ui.Icon != null && s.Icon != null) ui.Icon.sprite = s.Icon;
            if (ui.TypeBadge != null) ui.TypeBadge.text = s.IsEmpty ? "" : s.TypeBadge;
            if (ui.Chips != null) ui.Chips.SetActive(!s.IsEmpty && s.IsWeapon);
            if (!s.IsEmpty && s.IsWeapon)
            {
                if (ui.Mt != null) ui.Mt.text = s.Mt.ToString();
                if (ui.Hit != null) ui.Hit.text = s.Hit.ToString();
                if (ui.Crt != null) ui.Crt.text = s.Crt.ToString();
                if (ui.Rng != null) ui.Rng.text = s.RangeText;
            }
            if (ui.Uses != null)
            {
                ui.Uses.gameObject.SetActive(s.ShowUses);
                if (s.ShowUses) ui.Uses.text = s.UsesText;
            }
        }
    }
}
