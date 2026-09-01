namespace ProjectAstra.Core.Dialogue
{
    // What a script needs to remember between runs so it can show different content the
    // second time: whether it has been played before, and which options have been taken.
    //
    // An interface because the dialogue system must never learn what a visit or an objective
    // is. The hub hands over its visit state; a battle map hands over nothing, in which case
    // every question answers "no" and nothing is recorded.
    public interface IDialogueMemory
    {
        bool HasPlayed(string scriptId);
        void MarkPlayed(string scriptId);
        bool HasChosen(string scriptId, string optionId);
        void MarkChosen(string scriptId, string optionId);
    }
}
