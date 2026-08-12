using System;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

namespace ProjectAstra.Core.UI.UnitInfo
{
    // GEAR tab — five fixed inventory slots (icon, name, weapon chips, uses).
    // §9 is an accordion: every slot is always rendered, and the focused one grows to show its
    // chip row. The growth is animated here because a plain-C# controller has no frame to tick.
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

            [Header("§9 accordion")]
            public GameObject SlotRoot;
            public LayoutElement Sizer;
            public Image SlotBackground;
            public GameObject Expansion;
            public CanvasGroup ExpansionFader;
            public GameObject GradeChip;
            public TextMeshProUGUI GradeBadge;
            public Image GradeBadgeFill;
            public Image GradeBadgeBorder;
            public TextMeshProUGUI Weight;
            public GameObject UsesTrack;
            public Image UsesBar;
            public GameObject EffectChip;
            public TextMeshProUGUI Effect;
            public GameObject IconBox;
            public GameObject DashedFrame;
        }

        [Header("Tab root")]
        public GameObject Root;

        [Header("Gear slots (5)")]
        public GearSlotWidgets[] Slots;

        [Header("§9 heights, in canvas px")]
        public float CompactHeight = 112f;
        public float WeaponSelectedHeight = 184f;
        public float ConsumableSelectedHeight = 160f;

        [Header("§9 colours")]
        public Color CompactBackground = new Color32(0x17, 0x1b, 0x22, 0xff);
        public Color SelectedBackground = new Color32(0x1a, 0x20, 0x2b, 0xff);

        [Header("§10 timing")]
        public float SlotGrowSeconds = 0.12f;
        public float ExpansionFadeSeconds = 0.1f;

        private UnitGearModel model;
        private int selectedIndex = -1;
        private float[] targetHeights;
        private bool snapNextChange;

        public void SetTabActive(bool active) { if (Root != null) Root.SetActive(active); }

        public RectTransform FocusRectFor(int index) =>
            Slots != null && index >= 0 && index < Slots.Length && Slots[index].SlotRoot != null
                ? Slots[index].SlotRoot.GetComponent<RectTransform>()
                : null;

        // §8 skips empty slots, so the controller asks the model through the view it already holds.
        public bool IsSlotEmpty(int index)
        {
            GearSlotVM slot = SlotModel(index);
            return slot == null || slot.IsEmpty;
        }

        public void Render(UnitGearModel m)
        {
            if (m == null || Slots == null || m.Slots == null) return;

            model = m;
            for (int i = 0; i < Slots.Length && i < m.Slots.Length; i++)
                RenderSlot(Slots[i], m.Slots[i]);

            ResizeTargets();
            ApplyHeights(true);
        }

        // §9 clamps the growth to instant while a held key repeats, so nothing queues up.
        public void SetSelected(int index) => SetSelected(index, false);

        public void SetSelected(int index, bool instant)
        {
            selectedIndex = index;
            snapNextChange = instant;

            if (Slots == null) return;
            for (int i = 0; i < Slots.Length; i++)
                ApplySelectionChrome(Slots[i], i == index);

            ResizeTargets();
            if (instant) ApplyHeights(true);
        }

        private void ApplySelectionChrome(GearSlotWidgets ui, bool selected)
        {
            if (ui.Highlight != null) ui.Highlight.SetActive(selected);
            if (ui.SlotBackground != null)
                ui.SlotBackground.color = selected ? SelectedBackground : CompactBackground;
            if (ui.Expansion != null) ui.Expansion.SetActive(selected);
        }

        private void ResizeTargets()
        {
            if (Slots == null) return;
            if (targetHeights == null || targetHeights.Length != Slots.Length)
                targetHeights = new float[Slots.Length];

            for (int i = 0; i < Slots.Length; i++)
                targetHeights[i] = HeightFor(i);
        }

        private float HeightFor(int index)
        {
            if (index != selectedIndex) return CompactHeight;

            GearSlotVM slot = SlotModel(index);
            if (slot == null || slot.IsEmpty) return CompactHeight;
            return slot.IsWeapon ? WeaponSelectedHeight : ConsumableSelectedHeight;
        }

        private GearSlotVM SlotModel(int index)
        {
            if (model == null || model.Slots == null) return null;
            return index >= 0 && index < model.Slots.Length ? model.Slots[index] : null;
        }

        private void Update()
        {
            if (Slots == null || targetHeights == null) return;
            ApplyHeights(snapNextChange);
            snapNextChange = false;
        }

        private void ApplyHeights(bool instant)
        {
            if (Slots == null || targetHeights == null) return;

            float step = SlotGrowSeconds > 0f ? Time.unscaledDeltaTime / SlotGrowSeconds : 1f;
            for (int i = 0; i < Slots.Length; i++)
                StepHeight(Slots[i], targetHeights[i], instant ? 1f : step);
        }

        private void StepHeight(GearSlotWidgets ui, float target, float step)
        {
            if (ui.Sizer == null) return;

            float current = ui.Sizer.preferredHeight;
            ui.Sizer.preferredHeight = Mathf.Approximately(step, 1f)
                ? target
                : Mathf.Lerp(current, target, Mathf.Clamp01(step * EaseOutWeight));

            FadeExpansion(ui, step);
        }

        // Lerping toward the target each frame gives an ease-out curve for free; the weight just
        // sets how much of the remaining distance a full step covers.
        private const float EaseOutWeight = 2.2f;

        private void FadeExpansion(GearSlotWidgets ui, float step)
        {
            if (ui.ExpansionFader == null) return;

            bool visible = ui.Expansion != null && ui.Expansion.activeSelf;
            float target = visible ? 1f : 0f;
            float fade = ExpansionFadeSeconds > 0f ? Time.unscaledDeltaTime / ExpansionFadeSeconds : 1f;
            ui.ExpansionFader.alpha = Mathf.Approximately(step, 1f)
                ? target
                : Mathf.MoveTowards(ui.ExpansionFader.alpha, target, fade);
        }

        private void RenderSlot(GearSlotWidgets ui, GearSlotVM s)
        {
            if (ui.IndexLabel != null) ui.IndexLabel.text = s.Index.ToString();
            if (ui.EmptyState != null) ui.EmptyState.SetActive(s.IsEmpty);
            if (ui.EquippedMark != null) ui.EquippedMark.SetActive(s.IsEquipped);
            if (ui.Name != null) ui.Name.text = s.IsEmpty ? "EMPTY SLOT" : s.Name;
            if (ui.Icon != null && s.Icon != null) ui.Icon.sprite = s.Icon;
            if (ui.TypeBadge != null) ui.TypeBadge.text = s.IsEmpty ? "" : s.TypeBadge;

            RenderWeaponChips(ui, s);
            RenderConsumable(ui, s);
            RenderUses(ui, s);
            RenderGrade(ui, s);
            RenderEmptyTreatment(ui, s);
        }

        // §9 draws an empty slot as a dashed outline with a dashed icon box, so the solid frame
        // and the item glyph both step aside.
        private static void RenderEmptyTreatment(GearSlotWidgets ui, GearSlotVM s)
        {
            if (ui.DashedFrame != null) ui.DashedFrame.SetActive(s.IsEmpty);
            if (ui.SlotBackground != null) ui.SlotBackground.enabled = !s.IsEmpty;
            if (ui.Icon != null) ui.Icon.enabled = !s.IsEmpty;
            if (ui.IconBox != null) ui.IconBox.SetActive(true);
        }

        private static void RenderWeaponChips(GearSlotWidgets ui, GearSlotVM s)
        {
            bool weapon = !s.IsEmpty && s.IsWeapon;
            if (ui.Chips != null) ui.Chips.SetActive(weapon);
            if (!weapon) return;

            if (ui.Mt != null) ui.Mt.text = s.Mt.ToString();
            if (ui.Hit != null) ui.Hit.text = s.Hit.ToString();
            if (ui.Crt != null) ui.Crt.text = s.Crt.ToString();
            if (ui.Rng != null) ui.Rng.text = s.RangeText;
            if (ui.Weight != null) ui.Weight.text = s.Weight.ToString();
        }

        private static void RenderConsumable(GearSlotWidgets ui, GearSlotVM s)
        {
            bool consumable = !s.IsEmpty && !s.IsWeapon;
            if (ui.EffectChip != null) ui.EffectChip.SetActive(consumable);
            if (consumable && ui.Effect != null) ui.Effect.text = s.EffectText;
        }

        private static void RenderUses(GearSlotWidgets ui, GearSlotVM s)
        {
            if (ui.Uses != null)
            {
                ui.Uses.gameObject.SetActive(s.ShowUses);
                if (s.ShowUses) ui.Uses.text = s.UsesText;
            }
            if (ui.UsesTrack != null) ui.UsesTrack.SetActive(s.ShowUses);
            if (ui.UsesBar != null) ui.UsesBar.fillAmount = s.UsesFraction;
        }

        // Prf is a solid gold badge with a dark letter; letter grades leave the fill empty and
        // carry their tier colour on the border and the letter itself.
        private static void RenderGrade(GearSlotWidgets ui, GearSlotVM s)
        {
            bool graded = !s.IsEmpty && s.IsWeapon && !string.IsNullOrEmpty(s.Grade);
            if (ui.GradeChip != null) ui.GradeChip.SetActive(graded);
            if (!graded) return;

            if (ui.GradeBadge != null)
            {
                ui.GradeBadge.text = s.Grade;
                ui.GradeBadge.color = s.GradeIsPersonal ? DarkLetter : s.GradeColor;
            }
            if (ui.GradeBadgeFill != null)
                ui.GradeBadgeFill.color = s.GradeIsPersonal ? s.GradeColor : Transparent(s.GradeColor);
            if (ui.GradeBadgeBorder != null) ui.GradeBadgeBorder.color = s.GradeColor;
        }

        private static readonly Color DarkLetter = new Color32(0x14, 0x17, 0x1d, 0xff);

        private static Color Transparent(Color color) => new Color(color.r, color.g, color.b, 0f);
    }
}
