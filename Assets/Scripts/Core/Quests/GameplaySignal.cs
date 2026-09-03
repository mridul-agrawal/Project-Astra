namespace ProjectAstra.Core.Quests
{
    // The kinds of thing the game can tell the quest system about. Append only.
    public enum GameplaySignalKind
    {
        ConversationFinished,
        ObjectInspected,
        SignalRaised
    }

    // Something that happened, in the words of whoever it happened to.
    public readonly struct GameplaySignal
    {
        public readonly GameplaySignalKind Kind;
        public readonly string Id;

        public GameplaySignal(GameplaySignalKind kind, string id)
        {
            Kind = kind;
            Id = id;
        }

        public static GameplaySignal Conversation(string conversationId) =>
            new(GameplaySignalKind.ConversationFinished, conversationId);

        public static GameplaySignal Inspection(string interactableId) =>
            new(GameplaySignalKind.ObjectInspected, interactableId);

        // A named flag: a quiz passed, an authored event finished, anything design wants to key off.
        public static GameplaySignal Signal(string signalId) =>
            new(GameplaySignalKind.SignalRaised, signalId);

        public override string ToString() => $"{Kind}:{Id}";
    }
}
