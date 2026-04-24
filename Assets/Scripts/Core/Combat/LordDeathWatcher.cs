using System.Collections;
using ProjectAstra.Core.UI;
using UnityEngine;

namespace ProjectAstra.Core
{
    /// <summary>
    /// UM-02 — Lord Permadeath = Game Over.
    ///
    /// Listens on the shared UnitDeathEventChannel. When a Lord dies:
    ///   1. Fades the Lord's sprite over _fadeDurationSeconds (so the death
    ///      registers visually — today's flow hides units instantly).
    ///   2. Hides the GameObject.
    ///   3. Transitions BattleMap → Dialogue and plays the Lord's authored
    ///      last-words lines through DialogueSequencePlayer.
    ///   4. Transitions Dialogue → GameOver.
    ///
    /// BattleVictoryWatcher early-returns on args.isLord, so this watcher owns
    /// the end-of-chapter conclusion for Lord deaths.
    /// </summary>
    public class LordDeathWatcher : MonoBehaviour
    {
        [SerializeField] private UnitDeathEventChannel _deathChannel;
        [SerializeField] private DialogueSequencePlayer _dialoguePlayer;

        [Tooltip("How long the Lord's sprite fades out before the last-words dialogue begins.")]
        [SerializeField] private float _fadeDurationSeconds = 1.0f;

        [Tooltip("Shown if the Lord's UnitDefinition has no authored last-words lines.")]
        [SerializeField] private string _fallbackLine = "The hero has fallen.";

        private bool _concluded;

        private void Awake()
        {
            if (_deathChannel != null) _deathChannel.Register(OnUnitDied);
        }

        private void OnDestroy()
        {
            if (_deathChannel != null) _deathChannel.Unregister(OnUnitDied);
        }

        private void OnUnitDied(UnitDeathEventArgs args)
        {
            if (_concluded) return;
            if (!args.isLord) return;

            _concluded = true;
            StartCoroutine(LordDeathSequence(args));
        }

        private IEnumerator LordDeathSequence(UnitDeathEventArgs args)
        {
            var victim = args.victim;

            yield return FadeVictim(victim);
            if (victim != null) victim.gameObject.SetActive(false);

            var lines = ResolveLastWords(victim);

            GameStateManager.Instance?.RequestTransition(GameState.Dialogue, nameof(LordDeathWatcher));

            if (_dialoguePlayer != null)
                yield return _dialoguePlayer.Play(lines);

            GameStateManager.Instance?.RequestTransition(GameState.GameOver, nameof(LordDeathWatcher));
        }

        private IEnumerator FadeVictim(TestUnit victim)
        {
            if (victim == null || _fadeDurationSeconds <= 0f) yield break;

            var sprite = victim.GetComponentInChildren<SpriteRenderer>();
            if (sprite == null) yield break;

            var startColor = sprite.color;
            float elapsed = 0f;
            while (elapsed < _fadeDurationSeconds)
            {
                elapsed += Time.deltaTime;
                float t = Mathf.Clamp01(elapsed / _fadeDurationSeconds);
                var c = startColor;
                c.a = Mathf.Lerp(startColor.a, 0f, t);
                sprite.color = c;
                yield return null;
            }
        }

        private string[] ResolveLastWords(TestUnit victim)
        {
            var def = victim != null ? victim.UnitDefinition : null;
            var authored = def != null ? def.LastWordsLines : null;
            if (authored != null && authored.Length > 0)
                return authored;
            return new[] { _fallbackLine };
        }
    }
}
