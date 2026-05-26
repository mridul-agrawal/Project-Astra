using TMPro;
using UnityEngine;
using UnityEngine.UI;
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

        [SerializeField] private GameObject _root;
        [SerializeField] private Image _fullScreenImage;
        [SerializeField] private Image _leftPortrait;
        [SerializeField] private Image _rightPortrait;
        [SerializeField] private Image _centerPortrait;

        [Header("Nameplates — shown on the active speaker's side")]
        [SerializeField] private GameObject _namePlateLeft;
        [SerializeField] private TMP_Text _nameLabelLeft;
        [SerializeField] private GameObject _namePlateRight;
        [SerializeField] private TMP_Text _nameLabelRight;

        [SerializeField] private TMP_Text _bodyText;
        [SerializeField] private GameObject _continueHint;

        public void Show(DialogueTriggeringContext context)
        {
            ResetPortraits();
            if (_root != null) _root.SetActive(true);
        }

        public void ShowLine(in DialogueLineView line)
        {
            ApplyBackground(line.Background);
            ApplyPortrait(line.Portrait, line.Position, line.Facing);
            ApplyName(line.SpeakerName, line.Position);
            _bodyText.text = line.Text ?? string.Empty;
            _bodyText.maxVisibleCharacters = 0;
            SetContinueHintVisible(false);
        }

        public void SetVisibleCharacters(int count) => _bodyText.maxVisibleCharacters = count;

        public void SetContinueHintVisible(bool visible)
        {
            if (_continueHint != null) _continueHint.SetActive(visible);
        }

        public void Hide()
        {
            if (_root != null) _root.SetActive(false);
        }

        private void ApplyBackground(Sprite background)
        {
            if (_fullScreenImage == null) return;
            _fullScreenImage.sprite = background;
            _fullScreenImage.enabled = background != null;
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

            Tint(_leftPortrait, position == PortraitPosition.Left);
            Tint(_rightPortrait, position == PortraitPosition.Right);
            Tint(_centerPortrait, position == PortraitPosition.Center);
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
            PortraitPosition.Right => _rightPortrait,
            PortraitPosition.Center => _centerPortrait,
            _ => _leftPortrait
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
            SetPlate(_namePlateLeft, _nameLabelLeft, hasName && !right, speakerName);
            SetPlate(_namePlateRight, _nameLabelRight, hasName && right, speakerName);
        }

        private static void SetPlate(GameObject plate, TMP_Text label, bool show, string speakerName)
        {
            if (show && label != null) label.text = speakerName;
            if (plate != null) plate.SetActive(show);
        }

        // A fresh conversation shouldn't inherit the previous one's portraits.
        private void ResetPortraits()
        {
            ResetSlot(_leftPortrait);
            ResetSlot(_rightPortrait);
            ResetSlot(_centerPortrait);
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
