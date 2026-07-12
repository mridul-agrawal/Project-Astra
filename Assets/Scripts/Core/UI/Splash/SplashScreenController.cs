using UnityEngine;
using UnityEngine.Video;
using UnityEngine.InputSystem;
using ProjectAstra.Core.Audio;
using ProjectAstra.Core.State;

namespace ProjectAstra.Core.UI.Splash
{
    // Plays the studio logo reveal, then hands off to the Title screen. Any key, click, or
    // gamepad press skips it. The video renders into a RenderTexture shown by a full-screen
    // RawImage in the scene; this script only drives timing and the fade-out, so the visuals
    // stay data-driven and the controller stays small.
    public class SplashScreenController : MonoBehaviour
    {
        [SerializeField] private VideoPlayer videoPlayer;
        [SerializeField] private CanvasGroup fadeGroup;        // full-screen black cover, opaque until the video renders

        private bool isExiting;
        private bool playbackBegan;
        private bool revealed;

        private void Start()
        {
            ClearVideoTexture();
            if (fadeGroup != null) fadeGroup.alpha = 1f;   // stay black until the video is rendering
            BeginPlayback();
        }

        // The render texture still holds whatever it last showed — the logo's final frame from a
        // previous run — which flashes for a frame before playback starts. Wipe it to black so
        // the reveal always begins from black.
        private void ClearVideoTexture()
        {
            var target = videoPlayer != null ? videoPlayer.targetTexture : null;
            if (target == null) return;

            var previous = RenderTexture.active;
            RenderTexture.active = target;
            GL.Clear(true, true, Color.black);
            RenderTexture.active = previous;
        }

        private void BeginPlayback()
        {
            if (videoPlayer == null)
            {
                Debug.LogError("[SplashScreen] No VideoPlayer assigned; advancing to Title.");
                ExitToTitle();
                return;
            }

            videoPlayer.isLooping = false;
            videoPlayer.loopPointReached += OnVideoFinished;
            videoPlayer.Play();
            AudioManager.Instance?.Play(SoundId.SplashSting);
        }

        private void Update()
        {
            if (isExiting) return;
            RevealWhenPlaybackStarts();
            if (SkipRequested() || PlaybackFinished())
                ExitToTitle();
        }

        // Drop the black cover the moment playback is actually running — by then frame 0 (which
        // is itself black) is in the texture, so the reveal begins seamlessly from black.
        private void RevealWhenPlaybackStarts()
        {
            if (revealed || videoPlayer == null) return;
            if (!videoPlayer.isPlaying) return;

            if (fadeGroup != null) fadeGroup.alpha = 0f;
            revealed = true;
        }

        private void OnVideoFinished(VideoPlayer _) => ExitToTitle();

        // loopPointReached is unreliable for H.264 encodes with skewed timestamps (it may never
        // fire), so we detect the end ourselves: once playback has begun, finishing means the
        // player has stopped or reached its final frame.
        private bool PlaybackFinished()
        {
            if (videoPlayer == null) return false;
            if (videoPlayer.isPlaying) playbackBegan = true;
            if (!playbackBegan) return false;

            bool reachedLastFrame = videoPlayer.frameCount > 0
                && videoPlayer.frame >= (long)videoPlayer.frameCount - 1;
            return !videoPlayer.isPlaying || reachedLastFrame;
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
            if (isExiting) return;
            isExiting = true;

            if (videoPlayer != null) videoPlayer.loopPointReached -= OnVideoFinished;
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
