using System.Collections;
using UnityEngine;

namespace ProjectAstra.Core.UI.BattleMap
{
    // Coroutine helper that fades a SpriteRenderer's alpha to 0 over a fixed
    // duration. Used by SkipModePlaybackController for the brief death-fade on
    // the map sprite (and reusable by anything else that wants a one-shot
    // fade-out). Mirrors LordDeathWatcher.FadeVictim's loop verbatim.
    public static class SpriteFader
    {
        public static IEnumerator FadeOut(SpriteRenderer renderer, float duration)
        {
            if (renderer == null || duration <= 0f) yield break;
            var startColor = renderer.color;
            float elapsed = 0f;
            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                float t = Mathf.Clamp01(elapsed / duration);
                var c = startColor;
                c.a = Mathf.Lerp(startColor.a, 0f, t);
                renderer.color = c;
                yield return null;
            }
        }
    }
}
