using System;
using UnityEngine;
using ProjectAstra.Core.Animation;

namespace ProjectAstra.Core.Gurukul
{
    // A way between two rooms. Authored on the location it stands in.
    //
    // A doorway is an interaction, not a walk-through hole: she has to stand at it, face it, and
    // press INTERACT. Walking into a door does nothing, which is what the spec asks for.
    [Serializable]
    public struct GurukulDoor
    {
        public string doorId;

        [Tooltip("Where she has to stand next to, in tiles.")]
        public Vector2 position;

        [Tooltip("Enter for a way in, Leave for a way out.")]
        public GurukulVerb verb;

        [Tooltip("Room this leads to. Leave empty on an exit to send her back through whichever door she came in by — which is how six student houses share one interior.")]
        public string targetLocationId;

        public Vector2 targetSpawn;
        public Facing targetFacing;

        [Tooltip("Which house this door belongs to, for the shared student-house interior. Empty for a room that is only itself.")]
        public string houseIdentityId;

        [Tooltip("Shut until this gate opens. Empty for a door that is always usable.")]
        public string requiredGate;

        [Tooltip("Played instead while the door is shut. A gated door she can walk up to has to say why.")]
        public string deniedConversationId;

        // An exit with no authored destination goes back the way she came.
        public bool ReturnsToPreviousRoom => string.IsNullOrEmpty(targetLocationId);
    }
}
