using ProjectAstra.Core;
using ProjectAstra.Core.Hub.Events;

namespace ProjectAstra.Core.Editor
{
    // Which of an action's fields each kind actually uses, and what each one means there.
    //
    // The action carries every field for every kind, so without this an inspector shows eight boxes
    // of which two matter. This is the contract with HubEventRunner: if the runner starts reading a
    // different field, this table is what has to change with it.
    public readonly struct HubEventActionShape
    {
        public readonly string Summary;
        public readonly string TargetLabel;
        public readonly HubIdKind TargetKind;
        public readonly string ValueLabel;
        public readonly HubIdKind ValueKind;
        public readonly string SecondsLabel;
        public readonly string FlagLabel;
        public readonly bool UsesPosition;
        public readonly bool UsesFacing;
        public readonly bool UsesRoute;
        public readonly bool UsesState;

        public bool UsesTarget => TargetLabel != null;
        public bool UsesValue => ValueLabel != null;
        public bool UsesSeconds => SecondsLabel != null;
        public bool UsesFlag => FlagLabel != null;

        private HubEventActionShape(string summary,
            string targetLabel = null, HubIdKind targetKind = default,
            string valueLabel = null, HubIdKind valueKind = default,
            string secondsLabel = null, string flagLabel = null,
            bool position = false, bool facing = false, bool route = false, bool state = false)
        {
            Summary = summary;
            TargetLabel = targetLabel;
            TargetKind = targetKind;
            ValueLabel = valueLabel;
            ValueKind = valueKind;
            SecondsLabel = secondsLabel;
            FlagLabel = flagLabel;
            UsesPosition = position;
            UsesFacing = facing;
            UsesRoute = route;
            UsesState = state;
        }

        public static HubEventActionShape Of(HubEventActionKind kind) => kind switch
        {
            HubEventActionKind.PlayConversation => new HubEventActionShape(
                "Somebody says something, and the event waits for it to finish.",
                valueLabel: "Conversation", valueKind: HubIdKind.Conversation),

            HubEventActionKind.SetFacing => new HubEventActionShape(
                "Turns someone to look a different way.",
                targetLabel: "Who", targetKind: HubIdKind.Character, facing: true),

            HubEventActionKind.WalkCharacter => new HubEventActionShape(
                "Walks someone along a route. Each leg goes across then up, never diagonally. " +
                "Speed is in tiles per second, or 0 to walk at the usual pace.",
                targetLabel: "Who", targetKind: HubIdKind.Character,
                secondsLabel: "Speed", route: true),

            HubEventActionKind.RelocateCharacter => new HubEventActionShape(
                "Moves someone somewhere else at once, in this room or another.",
                targetLabel: "Who", targetKind: HubIdKind.Character,
                valueLabel: "To room", valueKind: HubIdKind.Location, position: true, facing: true),

            HubEventActionKind.SetCharacterPresent => new HubEventActionShape(
                "Puts someone in the room, or takes them out of it.",
                targetLabel: "Who", targetKind: HubIdKind.Character, flagLabel: "Present"),

            HubEventActionKind.SetInteractableState => new HubEventActionShape(
                "Changes what an object does when she walks up to it.",
                targetLabel: "Which object", targetKind: HubIdKind.Interactable, state: true),

            HubEventActionKind.SetGate => new HubEventActionShape(
                "Opens or shuts a gate. What opens on it is the door's own business.",
                targetLabel: "Gate", targetKind: HubIdKind.Gate, flagLabel: "Open"),

            HubEventActionKind.RaiseFlag => new HubEventActionShape(
                "Announces that something happened, for an objective to be waiting on.",
                valueLabel: "Signal", valueKind: HubIdKind.Signal),

            HubEventActionKind.FocusCamera => new HubEventActionShape(
                "Points the camera at someone until the event ends.",
                targetLabel: "Who", targetKind: HubIdKind.Character),

            HubEventActionKind.Wait => new HubEventActionShape(
                "Holds for a moment before the next action.",
                secondsLabel: "Seconds"),

            HubEventActionKind.Depart => new HubEventActionShape(
                "Ends the visit and starts a battle.",
                valueLabel: "Battle", valueKind: HubIdKind.Map),

            _ => new HubEventActionShape("")
        };
    }
}
