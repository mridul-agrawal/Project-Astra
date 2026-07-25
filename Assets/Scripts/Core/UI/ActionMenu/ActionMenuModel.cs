using System.Collections.Generic;

namespace ProjectAstra.Core.UI.ActionMenu
{
    public enum ActionChoice { Attack, Heal, Fortify, Item, Trade, Supply, Wait }

    public readonly struct ActionMenuEntry
    {
        public readonly string Label;
        public readonly ActionChoice Choice;

        public ActionMenuEntry(string label, ActionChoice choice)
        {
            Label = label;
            Choice = choice;
        }
    }

    public sealed class ActionMenuModel
    {
        private readonly List<ActionMenuEntry> entries = new();

        public IReadOnlyList<ActionMenuEntry> Entries => entries;

        public void Add(string label, ActionChoice choice) => entries.Add(new ActionMenuEntry(label, choice));

        // Labels in menu order, handed to the generic SelectionMenuView.
        public List<string> ToLabels()
        {
            var labels = new List<string>(entries.Count);
            foreach (var entry in entries)
                labels.Add(entry.Label);
            return labels;
        }
    }
}
