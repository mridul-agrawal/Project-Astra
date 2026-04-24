using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

namespace ProjectAstra.Core.UI
{
    /// <summary>
    /// Plays an ordered list of dialogue lines through a shared overlay. Confirm
    /// advances; the last confirm completes the sequence. Caller owns the state
    /// transitions (unlike DialogueOverlayUI which hardcodes a return-to-BattleMap
    /// on confirm).
    ///
    /// Used by LordDeathWatcher (UM-02) for the Lord's last-words sequence before
    /// GameOver, but kept generic so any future scripted dialogue can reuse it.
    /// </summary>
    public class DialogueSequencePlayer : MonoBehaviour
    {
        [SerializeField] private GameObject _overlayRoot;
        [SerializeField] private TMP_Text _lineText;

        private IReadOnlyList<string> _lines;
        private int _index;
        private Action _onComplete;
        private bool _confirmBound;

        public bool IsPlaying { get; private set; }

        /// <summary>Coroutine form — yield return Play(lines) to block until done.</summary>
        public IEnumerator Play(IReadOnlyList<string> lines)
        {
            bool done = false;
            Play(lines, () => done = true);
            while (!done) yield return null;
        }

        public void Play(IReadOnlyList<string> lines, Action onComplete)
        {
            if (IsPlaying)
            {
                Debug.LogWarning("[DialogueSequencePlayer] Play called while already playing; ignoring.");
                return;
            }

            _lines = lines;
            _index = 0;
            _onComplete = onComplete;
            IsPlaying = true;

            if (_overlayRoot != null) _overlayRoot.SetActive(true);
            ShowCurrentLine();
            BindConfirm();
        }

        private void ShowCurrentLine()
        {
            if (_lineText == null) return;
            _lineText.text = (_lines != null && _index < _lines.Count) ? _lines[_index] : string.Empty;
        }

        private void Advance()
        {
            _index++;
            if (_lines == null || _index >= _lines.Count)
            {
                Finish();
                return;
            }
            ShowCurrentLine();
        }

        private void Finish()
        {
            UnbindConfirm();
            if (_overlayRoot != null) _overlayRoot.SetActive(false);
            IsPlaying = false;

            var cb = _onComplete;
            _onComplete = null;
            _lines = null;
            cb?.Invoke();
        }

        private void BindConfirm()
        {
            if (_confirmBound) return;
            if (InputManager.Instance == null) return;
            InputManager.Instance.OnConfirm += Advance;
            _confirmBound = true;
        }

        private void UnbindConfirm()
        {
            if (!_confirmBound) return;
            if (InputManager.Instance != null)
                InputManager.Instance.OnConfirm -= Advance;
            _confirmBound = false;
        }

        private void OnDisable() => UnbindConfirm();
    }
}
