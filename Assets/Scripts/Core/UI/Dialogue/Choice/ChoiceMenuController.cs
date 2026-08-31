using System;
using System.Collections.Generic;
using ProjectAstra.Core.Dialogue.Conversation;

namespace ProjectAstra.Core.UI.Dialogue.Choice
{
    // Owns which option the cursor is on and what happens when it is confirmed. Plain C#, so the
    // navigation rules — wrapping, skipping greyed rows, refusing to confirm a used topic — test
    // without a Canvas.
    public sealed class ChoiceMenuController
    {
        public ChoiceMenuView choiceView;
        public ChoiceMenuModel choiceModel;

        private Action<int> onChosen;
        private Action onCancelled;
        private bool allowCancel;

        public ChoiceMenuController(ChoiceMenuView view)
        {
            choiceView = view;
            choiceModel = new ChoiceMenuModel();
            Render();
        }

        public bool IsOpen => choiceModel.Visible;
        public int HighlightedIndex { get; private set; }

        public void Show(IReadOnlyList<ConversationChoice> choices, bool cancellable,
            Action<int> chosen, Action cancelled)
        {
            onChosen = chosen;
            onCancelled = cancelled;
            allowCancel = cancellable;

            choiceModel.Rows.Clear();
            foreach (ConversationChoice choice in choices)
                choiceModel.Rows.Add(new ChoiceRowVM { Label = choice.Label, Enabled = choice.Enabled });

            choiceModel.Visible = choiceModel.Rows.Count > 0;
            HighlightedIndex = FirstEnabled();
            Render();
        }

        public void Hide()
        {
            choiceModel.Visible = false;
            Render();
        }

        // Wraps around, and steps over anything greyed out so the cursor never rests on a topic
        // she has already asked.
        public void Move(int step)
        {
            int count = choiceModel.Rows.Count;
            if (!IsOpen || step == 0 || count == 0 || HighlightedIndex < 0) return;

            int candidate = HighlightedIndex;
            for (int i = 0; i < count; i++)
            {
                candidate = (candidate + step + count) % count;
                if (!choiceModel.Rows[candidate].Enabled) continue;
                HighlightedIndex = candidate;
                Render();
                return;
            }
        }

        public void Confirm()
        {
            if (!IsOpen) return;
            if (HighlightedIndex < 0 || !choiceModel.Rows[HighlightedIndex].Enabled) return;

            int chosen = HighlightedIndex;
            Action<int> callback = onChosen;
            Hide();
            callback?.Invoke(chosen);
        }

        // A knowledge check can't be backed out of. When cancelling isn't allowed the menu simply
        // stays put, rather than closing and leaving the conversation nowhere to go.
        public void Cancel()
        {
            if (!IsOpen || !allowCancel) return;

            Action callback = onCancelled;
            Hide();
            callback?.Invoke();
        }

        private int FirstEnabled()
        {
            for (int i = 0; i < choiceModel.Rows.Count; i++)
                if (choiceModel.Rows[i].Enabled) return i;
            return choiceModel.Rows.Count > 0 ? 0 : -1;
        }

        private void Render()
        {
            for (int i = 0; i < choiceModel.Rows.Count; i++)
                choiceModel.Rows[i].Highlighted = i == HighlightedIndex;
            choiceView.Render(choiceModel);
        }
    }
}
