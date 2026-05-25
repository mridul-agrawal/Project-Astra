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
        [SerializeField] private TMP_Text _nameLabel;
        [SerializeField] private TMP_Text _bodyText;
        [SerializeField] private GameObject _continueHint;

        public void Show(DialogueTriggeringContext context)
        {
            if (_root != null) _root.SetActive(true);
        }

        public void ShowLine(in DialogueLineView line)
        {
            ApplyBackground(line.Background);
            ApplyPortrait(line.Portrait, line.Position);
            ApplyName(line.SpeakerName);
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
        private void ApplyPortrait(Sprite portrait, PortraitPosition position)
        {
            if (portrait != null && position != PortraitPosition.None)
                AssignPortrait(SlotFor(position), portrait);

            Tint(_leftPortrait, position == PortraitPosition.Left);
            Tint(_rightPortrait, position == PortraitPosition.Right);
            Tint(_centerPortrait, position == PortraitPosition.Center);
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

        private void ApplyName(string speakerName)
        {
            if (_nameLabel == null) return;
            _nameLabel.text = speakerName ?? string.Empty;
            _nameLabel.gameObject.SetActive(!string.IsNullOrEmpty(speakerName));
        }
    }
}
