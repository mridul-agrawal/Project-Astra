using System.Collections.Generic;
using UnityEngine;

namespace ProjectAstra.Core.Dialogue
{
    // Everything the runner needs to know about one line, already resolved
    // (portrait sprite picked, name looked up). The view just renders it.
    public readonly struct DialogueLineView
    {
        public readonly Sprite Portrait;
        public readonly PortraitPosition Position;
        public readonly PortraitFacing Facing;
        public readonly string SpeakerName;
        public readonly string Text;
        public readonly Sprite Background;

        public DialogueLineView(Sprite portrait, PortraitPosition position, PortraitFacing facing,
            string speakerName, string text, Sprite background)
        {
            Portrait = portrait;
            Position = position;
            Facing = facing;
            SpeakerName = speakerName;
            Text = text;
            Background = background;
        }
    }

    // One option as it should currently read. Disabled means already taken — shown greyed
    // rather than removed, so a topic menu doesn't reshuffle under the player.
    public readonly struct DialogueChoiceView
    {
        public readonly string Label;
        public readonly bool Enabled;

        public DialogueChoiceView(string label, bool enabled)
        {
            Label = label;
            Enabled = enabled;
        }
    }

    // The presentation surface the runner drives. Kept as an interface so the
    // runner can be unit-tested against a fake, with no Canvas or Unity time.
    public interface IDialogueView
    {
        void Show(DialogueTriggeringContext context);
        void ShowLine(in DialogueLineView line);
        void SetVisibleCharacters(int count);
        void SetContinueHintVisible(bool visible);

        // The runner owns which option is highlighted, exactly as it owns the crawl. The view
        // draws what it is handed and decides nothing.
        void ShowChoices(IReadOnlyList<DialogueChoiceView> options, int highlighted);
        void HideChoices();

        void Hide();
    }
}
