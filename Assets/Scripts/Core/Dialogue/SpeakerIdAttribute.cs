using UnityEngine;

namespace ProjectAstra.Core.Dialogue
{
    // Marks a string field as a speaker id so the inspector shows a dropdown of
    // known speakers (every DialogueSpeaker asset + the narrator) instead of a
    // free-text box that's easy to mistype. Drawn by SpeakerIdDrawer.
    public class SpeakerIdAttribute : PropertyAttribute { }
}
