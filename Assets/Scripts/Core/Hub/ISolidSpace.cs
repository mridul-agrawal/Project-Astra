using UnityEngine;

namespace ProjectAstra.Core.Hub
{
    // The one question movement asks of the world: can she stand here?
    //
    // Behind it is either the scene's colliders or, in a test, a rect somebody drew by hand. That is
    // the point — the sweep and the route search stay testable without a scene.
    public interface ISolidSpace
    {
        bool IsBlocked(Rect footprint);
    }
}
