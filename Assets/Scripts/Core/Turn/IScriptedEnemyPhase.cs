using System.Collections;

namespace ProjectAstra.Core.Turn
{
    // A battle may choreograph its non-player phases (e.g. Map 1's scripted raider)
    // instead of running the default AI. TurnManager discovers one implementor per
    // scene; returning false for a phase falls back to the default AI behavior.
    public interface IScriptedEnemyPhase
    {
        bool TryBuildPhaseScript(BattlePhase phase, int turn, out IEnumerator routine);
    }
}
