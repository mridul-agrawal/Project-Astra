namespace ProjectAstra.Core.Dialogue
{
    // What a node in a script does when the runner reaches it. Append only — authored
    // lines store the kind as an int.
    public enum DialogueNodeKind
    {
        // Speak. The only kind that shows text and waits for the player.
        Line,

        // Offer an authored list of options; the one picked decides which node comes next.
        Choice,

        // Continue at a labelled node instead of the next one in the list.
        Jump,

        // Announce an id and carry straight on. Whoever asked for the script decides what
        // it means — an objective ticking over, a gate opening, a departure. The dialogue
        // system never finds out.
        Signal
    }
}
