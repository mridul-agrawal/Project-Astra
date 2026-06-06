using UnityEngine;
using UnityEngine.Video;
using UnityEngine.InputSystem;
using ProjectAstra.Core.State;

namespace ProjectAstra.Core.UI.Splash
{
    // Plays the studio logo reveal, then hands off to the Title screen. Any key, click, or
    // gamepad press skips it. The video renders into a RenderTexture shown by a full-screen
    // RawImage in the scene; this script only drives timing and the fade-out, so the visuals
    // stay data-driven and the controller stays small.
    public class SplashScreenController : MonoBehaviour
    {
        [SerializeField] private VideoPlayer _videoPlayer;
        [SerializeField] private CanvasGroup _fadeGroup;        // full-screen black cover, opaque until the video renders

        private bool _isExiting;
        private bool _playbackBegan;
        private bool _revealed;

        private void Start()
        {
            ClearVideoTexture();
            if (_fadeGroup != null) _fadeGroup.alpha = 1f;   // stay black until the video is rendering
            BeginPlayback();
        }

        // The render texture still holds whatever it last showed — the logo's final frame from a
        // previous run — which flashes for a frame before playback starts. Wipe it to black so
        // the reveal always begins from black.
        private void ClearVideoTexture()
        {
            var target = _videoPlayer != null ? _videoPlayer.targetTexture : null;
            if (target == null) return;

            var previous = RenderTexture.active;
            RenderTexture.active = target;
            GL.Clear(true, true, Color.black);
            RenderTexture.active = previous;
        }

        private void BeginPlayback()
        {
            if (_videoPlayer == null)
            {
                Debug.LogError("[SplashScreen] No VideoPlayer assigned; advancing to Title.");
                ExitToTitle();
                return;
            }

            _videoPlayer.isLooping = false;
            _videoPlayer.loopPointReached += OnVideoFinished;
            _videoPlayer.Play();
        }

        private void Update()
        {
            if (_isExiting) return;
            RevealWhenPlaybackStarts();
            if (SkipRequested() || PlaybackFinished())
                ExitToTitle();
        }

        // Drop the black cover the moment playback is actually running — by then frame 0 (which
        // is itself black) is in the texture, so the reveal begins seamlessly from black.
        private void RevealWhenPlaybackStarts()
        {
            if (_revealed || _videoPlayer == null) return;
            if (!_videoPlayer.isPlaying) return;

            if (_fadeGroup != null) _fadeGroup.alpha = 0f;
            _revealed = true;
        }

        private void OnVideoFinished(VideoPlayer _) => ExitToTitle();

        // loopPointReached is unreliable for H.264 encodes with skewed timestamps (it may never
        // fire), so we detect the end ourselves: once playback has begun, finishing means the
        // player has stopped or reached its final frame.
        private bool PlaybackFinished()
        {
            if (_videoPlayer == null) return false;
            if (_videoPlayer.isPlaying) _playbackBegan = true;
            if (!_playbackBegan) return false;

            bool reachedLastFrame = _videoPlayer.frameCount > 0
                && _videoPlayer.frame >= (long)_videoPlayer.frameCount - 1;
            return !_videoPlayer.isPlaying || reachedLastFrame;
        }

        // Raw device polling rather than the action map: the splash runs outside any input
        // context, so there are no enabled actions to listen to — we just want "anything".
        private static bool SkipRequested()
        {
            return (Keyboard.current?.anyKey.wasPressedThisFrame ?? false)
                || (Mouse.current?.leftButton.wasPressedThisFrame ?? false)
                || (Gamepad.current?.buttonSouth.wasPressedThisFrame ?? false)
                || (Gamepad.current?.startButton.wasPressedThisFrame ?? false);
        }

        private void ExitToTitle()
        {
            if (_isExiting) return;
            _isExiting = true;

            if (_videoPlayer != null) _videoPlayer.loopPointReached -= OnVideoFinished;
            AdvanceToTitle();   // ScreenFader fades the swap to the Title scene
        }

        // The state machine drives the actual scene swap. When the scene is played directly in
        // the editor (no boot flow), there's no manager to ask — log and stop rather than throw.
        private void AdvanceToTitle()
        {
            var gsm = GameStateManager.Instance;
            if (gsm == null)
            {
                Debug.LogWarning("[SplashScreen] No GameStateManager (scene played directly?). Cannot advance to Title.");
                return;
            }

            gsm.RequestTransition(GameState.TitleScreen, nameof(SplashScreenController));
        }
    }
}
