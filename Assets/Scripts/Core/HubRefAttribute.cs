using UnityEngine;

namespace ProjectAstra.Core
{
    // Marks a ScriptableObject-reference field so the inspector (and the Data Hub) draw it with
    // inline "+" (create a new target asset and wire it up) and open-in-hub buttons. This is the
    // plain runtime marker; the editor behaviour lives in HubReferenceDrawer.
    public class HubRefAttribute : PropertyAttribute { }
}
