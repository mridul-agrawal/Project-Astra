using ProjectAstra.Core.Dialogue;

namespace ProjectAstra.Core.Hub.Interaction
{
    // The script catalog, for interactables the loader built at runtime and could not wire.
    public static class HubInteractionCatalog
    {
        [UnityEngine.RuntimeInitializeOnLoadMethod(
            UnityEngine.RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetStatics() => Scripts = null;

        public static DialogueScriptCatalog Scripts { get; private set; }

        public static void Bind(DialogueScriptCatalog scripts) => Scripts = scripts;
    }
}
