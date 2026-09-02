using ProjectAstra.Core.Animation;
using UnityEngine;

namespace ProjectAstra.Core.Hub.Interaction
{
    // Which rung of the spec's ladder a target sits on when several are in range. Lower wins.
    // The spec's first rung — an open menu or a running line — never reaches here, because the
    // controller is silent unless the hub has control.
    public enum InteractionPriority
    {
        Character = 0,
        ObjectiveObject = 1,
        Door = 2,
        Inspectable = 3
    }

    // Where the player is and which way she is looking, which is all a target needs to decide
    // whether she can reach it.
    public readonly struct InteractorPose
    {
        public readonly Vector2 Position;
        public readonly Facing Facing;

        public InteractorPose(Vector2 position, Facing facing)
        {
            Position = position;
            Facing = facing;
        }
    }

    // Anything the player can press INTERACT on.
    public interface IInteractable
    {
        // The verb to show, and whether to show one at all.
        HubVerb Verb { get; }

        // Is this a target at all right now? A character with nothing to say is not — no prompt,
        // no blank dialogue box. Being locked is a different question: a locked door is still
        // available, and answers with a denial line when pressed.
        bool IsAvailable { get; }

        // Which rung, when several are in range at once.
        InteractionPriority Priority { get; }

        // Where the prompt points and what "most directly in front" is measured from.
        Vector2 InteractionPoint { get; }

        // Close enough is decided by the trigger; this is everything else — facing, line of sight,
        // or whatever else this particular thing wants to insist on.
        bool CanReach(InteractorPose player);

        // What a press does. The only place that knows.
        void Interact(InteractorPose player);
    }
}
