using System.Collections;
using ProjectAstra.Core.Dialogue;
using ProjectAstra.Core.Events;
using ProjectAstra.Core.State;
using ProjectAstra.Core.UI;
using ProjectAstra.Core.Units;
using UnityEngine;

namespace ProjectAstra.Core.Combat
{
    // UM-02 — Lord Permadeath = Game Over.
    //
    // Listens for unit deaths via EventService. When a Lord dies:
    //   1. Fades the Lord's sprite over _fadeDurationSeconds (so the death
    //      registers visually — today's flow hides units instantly).
    //   2. Hides the GameObject.
    //   3. Transitions BattleMap → Dialogue and plays the Lord's authored
    //      last-words lines through DialogueSequencePlayer.
    //   4. Transitions Dialogue → GameOver.
    //
    // BattleVictoryWatcher early-returns on args.isLord, so this watcher owns
    // the end-of-chapter conclusion for Lord deaths.
    public class LordDeathWatcher : MonoBehaviour
    {

        [Tooltip("How long the Lord's sprite fades out before the last-words dialogue begins.")]
        [SerializeField] private float fadeDurationSeconds = 1.0f;

        [Tooltip("Shown if the Lord's UnitDefinition has no authored last-words lines.")]
        [SerializeField] private string fallbackLine = "The hero has fallen.";

        private bool concluded;

        private void Awake()
        {
            EventService.Instance.SubscribeUnitDeath(OnUnitDied);
        }

        private void OnDestroy()
        {
            if (EventService.Instance != null) EventService.Instance.UnsubscribeUnitDeath(OnUnitDied);
        }

        private void OnUnitDied(UnitDeathEventArgs args)
        {
            if (concluded) return;
            if (!args.isLord) return;

            concluded = true;
            StartCoroutine(LordDeathSequence(args));
        }

        private IEnumerator LordDeathSequence(UnitDeathEventArgs args)
        {
            var victim = args.victim;

            yield return FadeVictim(victim);
            if (victim != null) victim.gameObject.SetActive(false);

            var script = DialogueScript.CreateRuntime("LORD_LASTWORDS",
                victim?.UnitDefinition?.Speaker?.SpeakerId, ResolveLastWords(victim));

            GameStateManager.Instance?.RequestTransition(GameState.Dialogue, nameof(LordDeathWatcher));

            if (DialogueService.Instance != null)
                yield return DialogueService.Instance.PlayRoutine(script, DialogueTriggeringContext.Cutscene);

            GameStateManager.Instance?.RequestTransition(GameState.GameOver, nameof(LordDeathWatcher));
        }

        private IEnumerator FadeVictim(TestUnit victim)
        {
            if (victim == null || fadeDurationSeconds <= 0f) yield break;

            var sprite = victim.GetComponentInChildren<SpriteRenderer>();
            if (sprite == null) yield break;

            var startColor = sprite.color;
            float elapsed = 0f;
            while (elapsed < fadeDurationSeconds)
            {
                elapsed += Time.deltaTime;
                float t = Mathf.Clamp01(elapsed / fadeDurationSeconds);
                var c = startColor;
                c.a = Mathf.Lerp(startColor.a, 0f, t);
                sprite.color = c;
                yield return null;
            }
        }

        private string[] ResolveLastWords(TestUnit victim)
        {
            var authored = victim?.UnitDefinition?.LastWordsLines;
            if (authored != null && authored.Length > 0)
                return authored;
            return new[] { fallbackLine };
        }
    }
}
