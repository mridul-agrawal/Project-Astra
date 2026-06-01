using System.Collections;
using UnityEngine;

namespace ProjectAstra.Core.UI.BattleMap
{
    // Brief white flash on a unit's SpriteRenderer when it takes a hit in
    // Skip mode (no overlay scene). Coroutine — start from a MonoBehaviour
    // via StartCoroutine(HitFlashEffect.Flash(...)).
    public static class HitFlashEffect
    {
        public static IEnumerator Flash(SpriteRenderer renderer, float duration, Color flashColor)
        {
            if (renderer == null || duration <= 0f) yield break;
            var original = renderer.color;
            renderer.color = flashColor;
            yield return new WaitForSeconds(duration);
            if (renderer != null) renderer.color = original;
        }
    }
}
