using System.Collections.Generic;

namespace ProjectAstra.Core.UI.Dialogue.Choice
{
    public sealed class ChoiceRowVM
    {
        public string Label = "";
        public bool Enabled = true;
        public bool Highlighted;
    }

    // The choice list as it should currently read: the options, and which one the cursor is on.
    public sealed class ChoiceMenuModel
    {
        public bool Visible;
        public readonly List<ChoiceRowVM> Rows = new();
    }
}
