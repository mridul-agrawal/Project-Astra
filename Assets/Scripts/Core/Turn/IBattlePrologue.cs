using System.Collections;

namespace ProjectAstra.Core.Turn
{
    // A scripted sequence that plays once a battle has registered its units but before the
    // first phase begins. TurnManager yields to it, so the player gets no control until it
    // finishes. Optional — a battle with no prologue component just starts normally.
    public interface IBattlePrologue
    {
        IEnumerator Play();
    }
}
