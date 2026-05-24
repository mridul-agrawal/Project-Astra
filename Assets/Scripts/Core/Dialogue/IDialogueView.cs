using UnityEngine;

namespace ProjectAstra.Core.Dialogue
{
    // Everything the runner needs to know about one line, already resolved
    // (portrait sprite picked, name looked up). The view just renders it.
    public readonly struct DialogueLineView
    {
        public readonly Sprite Portrait;
        public readonly PortraitPosition Position;
        public readonly string SpeakerName;
        public readonly string Text;
        public readonly Sprite Background;

        public DialogueLineView(Sprite portrait, PortraitPosition position, string speakerName, string text, Sprite background)
        {
            Portrait = portrait;
            Position = position;
            SpeakerName = speakerName;
            Text = text;
            Background = background;
        }
    }

    // The presentation surface the runner drives. Kept as an interface so the
    // runner can be unit-tested against a fake, with no Canvas or Unity time.
    public interface IDialogueView
    {
        void Show(DialogueContext context);
        void ShowLine(in DialogueLineView line);
        void SetVisibleCharacters(int count);
        void SetContinueHintVisible(bool visible);
        void Hide();
    }
}
