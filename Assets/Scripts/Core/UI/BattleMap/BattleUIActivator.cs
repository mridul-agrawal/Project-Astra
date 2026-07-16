using UnityEngine;

namespace ProjectAstra.Core.UI.BattleMap
{
    // Wakes the always-on battle-UI listeners at battle-map load.
    //
    // Every child of the UI Canvas starts DISABLED in the scene (the project rule:
    // no UI screen is enabled by default). Most screens are fine with that — their
    // opener re-enables them on demand. But a few must be ALIVE for the whole battle
    // to do their job: they subscribe to events or run coroutines, and a disabled
    // GameObject never runs Awake/OnEnable, so it would never hear its trigger. This
    // enables exactly those, once, early — before the first phase fires. They keep
    // their own visible content hidden until it is actually needed.
    //
    // Lives on an always-active object (the Canvas root), NOT on a disabled child.
    [DefaultExecutionOrder(-100)]
    public class BattleUIActivator : MonoBehaviour
    {
        [Tooltip("Screens that must be alive during battle to function. They self-hide their content until needed.")]
        [SerializeField] private GameObject[] screensToActivate;

        private void Awake()
        {
            if (screensToActivate == null) return;
            foreach (var screen in screensToActivate)
                if (screen != null && !screen.activeSelf)
                    screen.SetActive(true);
        }
    }
}
