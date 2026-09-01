using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using ProjectAstra.Core.Audio;
using ProjectAstra.Core.Dialogue;

namespace ProjectAstra.Core.UI.Dialogue
{
    // The uGUI surface for dialogue: an optional full-screen still, left/right/center
    // portraits, a name label, and the bottom text box. The runner owns all timing —
    // this class only renders what it's told. Built by DialogueViewBuilder.
    public class DialogueView : MonoBehaviour, IDialogueView
    {
        private static readonly Color ActiveTint = Color.white;
        private static readonly Color DimTint = new(0.45f, 0.45f, 0.45f, 1f);

        [SerializeField] private GameObject root;
        [SerializeField] private Image fullScreenImage;
        [SerializeField] private Image leftPortrait;
        [SerializeField] private Image rightPortrait;
        [SerializeField] private Image centerPortrait;

        [Header("Nameplates — shown on the active speaker's side")]
        [SerializeField] private GameObject namePlateLeft;
        [SerializeField] private TMP_Text nameLabelLeft;
        [SerializeField] private GameObject namePlateRight;
        [SerializeField] private TMP_Text nameLabelRight;

        [SerializeField] private TMP_Text bodyText;
        [SerializeField] private GameObject continueHint;

        [Tooltip("The options row shown under the box during a Choice node. Optional — a script " +
                 "with no choices never needs it.")]
        [SerializeField] private Choice.ChoiceMenuView choices;

        private int lastVisibleCount;

        // A hub conversation is several scripts in a row with choices between them. Left alone, each
        // script would clear the portraits on the way in and switch the whole box off on the way
        // out, so the presentation would blink at every choice point. Bracketing the run holds the
        // box open and keeps the cast on screen until the conversation itself is over.
        private bool inConversation;

        public void BeginConversation() => inConversation = true;

        public void EndConversation()
        {
            inConversation = false;
            Hide();
        }

        public void Show(DialogueTriggeringContext context)
        {
            if (!inConversation) ResetPortraits();
            if (root != null) root.SetActive(true);
        }

        public void ShowLine(in DialogueLineView line)
        {
            ApplyBackground(line.Background);
            ApplyPortrait(line.Portrait, line.Position, line.Facing);
            ApplyName(line.SpeakerName, line.Position);
            bodyText.text = line.Text ?? string.Empty;
            bodyText.maxVisibleCharacters = 0;
            lastVisibleCount = 0;
            SetContinueHintVisible(false);
        }

        public void SetVisibleCharacters(int count)
        {
            // One soft blip per newly typed character — skip whitespace and reveal-all jumps.
            if (count == lastVisibleCount + 1 && bodyText.text != null
                && count >= 1 && count <= bodyText.text.Length
                && !char.IsWhiteSpace(bodyText.text[count - 1]))
            {
                AudioManager.Instance?.Play(SoundId.DialogueBlip);
            }
            lastVisibleCount = count;
            bodyText.maxVisibleCharacters = count;
        }

        public void ShowChoices(IReadOnlyList<DialogueChoiceView> options, int highlighted)
        {
            if (choices != null) choices.Render(options, highlighted);
        }

        public void HideChoices()
        {
            if (choices != null) choices.SetVisible(false);
        }

        public void SetContinueHintVisible(bool visible)
        {
            if (continueHint != null) continueHint.SetActive(visible);
        }

        public void Hide()
        {
            if (inConversation) return;
            if (root != null) root.SetActive(false);
        }

        private void ApplyBackground(Sprite background)
        {
            if (fullScreenImage == null) return;
            fullScreenImage.sprite = background;
            fullScreenImage.enabled = background != null;
        }

        // Assigns the speaking portrait to its side and keeps it; the active side is
        // lit, every other shown portrait dims — the "facing each other" feel.
        private void ApplyPortrait(Sprite portrait, PortraitPosition position, PortraitFacing facing)
        {
            if (portrait != null && position != PortraitPosition.None)
            {
                var slot = SlotFor(position);
                AssignPortrait(slot, portrait);
                ApplyFacing(slot, facing);
            }

            Tint(leftPortrait, position == PortraitPosition.Left);
            Tint(rightPortrait, position == PortraitPosition.Right);
            Tint(centerPortrait, position == PortraitPosition.Center);
        }

        // Art faces Left by default; Right mirrors the portrait horizontally.
        private static void ApplyFacing(Image slot, PortraitFacing facing)
        {
            if (slot == null) return;
            var scale = slot.rectTransform.localScale;
            scale.x = Mathf.Abs(scale.x) * (facing == PortraitFacing.Right ? -1f : 1f);
            slot.rectTransform.localScale = scale;
        }

        private Image SlotFor(PortraitPosition position) => position switch
        {
            PortraitPosition.Right => rightPortrait,
            PortraitPosition.Center => centerPortrait,
            _ => leftPortrait
        };

        private static void AssignPortrait(Image slot, Sprite portrait)
        {
            if (slot == null) return;
            slot.sprite = portrait;
            slot.enabled = true;
        }

        private static void Tint(Image slot, bool active)
        {
            if (slot == null || !slot.enabled) return;
            slot.color = active ? ActiveTint : DimTint;
        }

        // Narrator/system lines have no name — hide both plates; otherwise show the
        // plate on the active speaker's side (Left/Center → left, Right → right).
        private void ApplyName(string speakerName, PortraitPosition position)
        {
            bool hasName = !string.IsNullOrEmpty(speakerName);
            bool right = position == PortraitPosition.Right;
            SetPlate(namePlateLeft, nameLabelLeft, hasName && !right, speakerName);
            SetPlate(namePlateRight, nameLabelRight, hasName && right, speakerName);
        }

        private static void SetPlate(GameObject plate, TMP_Text label, bool show, string speakerName)
        {
            if (show && label != null) label.text = speakerName;
            if (plate != null) plate.SetActive(show);
        }

        // A fresh conversation shouldn't inherit the previous one's portraits.
        private void ResetPortraits()
        {
            ResetSlot(leftPortrait);
            ResetSlot(rightPortrait);
            ResetSlot(centerPortrait);
        }

        private static void ResetSlot(Image slot)
        {
            if (slot == null) return;
            slot.sprite = null;
            slot.enabled = false;
            var scale = slot.rectTransform.localScale;
            scale.x = Mathf.Abs(scale.x);
            slot.rectTransform.localScale = scale;
        }
    }
}
