namespace ProjectAstra.Core.State
{
    // Payload for a game-state transition: the state we left and the one we entered.
    public struct StateChangeArgs
    {
        public GameState PreviousState;
        public GameState NewState;
    }
}
