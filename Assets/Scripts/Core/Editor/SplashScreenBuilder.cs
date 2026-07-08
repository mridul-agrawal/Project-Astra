using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using UnityEngine.Video;
using ProjectAstra.Core.State;
using ProjectAstra.Core.UI.Splash;

namespace ProjectAstra.Core.Editor
{
    // One-shot installer for the Rivenza logo splash. Builds the Splash scene (video -> render
    // texture -> full-screen RawImage, plus a black fade overlay), wires the controller, then
    // hooks it into the boot flow: appends Splash->TitleScreen to the transition table, makes
    // Splash the initial state, registers the scene in build settings, and disables Unity's
    // own engine splash. Re-runnable: rebuilds the scene and is idempotent on the wiring.
    public static class SplashScreenBuilder
    {
        private const int CanvasWidth = 1920;
        private const int CanvasHeight = 1080;

        private const string VideoPath = "Assets/Video/Splash/rivenza_reveal_primary_1080p.mp4";
        private const string RenderTexturePath = "Assets/Video/Splash/RivenzaSplashRT.renderTexture";
        private const string ScenePath = "Assets/Scenes/Splash.unity";
        private const string BootScenePath = "Assets/Scenes/BootScene.unity";

        [MenuItem("Project Astra/Build Splash Screen")]
        public static void Build()
        {
            var clip = AssetDatabase.LoadAssetAtPath<VideoClip>(VideoPath);
            if (clip == null)
            {
                Debug.LogError($"[SplashBuilder] Video clip not found at {VideoPath}. Import it first.");
                return;
            }

            var renderTexture = GetOrCreateRenderTexture();
            BuildSplashScene(clip, renderTexture);
            SetInitialStateToSplash();
            AppendSplashTransition();
            RegisterSceneInBuildSettings();
            DisableUnityEngineSplash();

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log("[SplashBuilder] Splash screen installed. Press Play from BootScene to verify.");
        }

        private static RenderTexture GetOrCreateRenderTexture()
        {
            var existing = AssetDatabase.LoadAssetAtPath<RenderTexture>(RenderTexturePath);
            if (existing != null) return existing;

            var rt = new RenderTexture(CanvasWidth, CanvasHeight, 0, RenderTextureFormat.ARGB32)
            {
                name = "RivenzaSplashRT"
            };
            AssetDatabase.CreateAsset(rt, RenderTexturePath);
            return rt;
        }

        private static void BuildSplashScene(VideoClip clip, RenderTexture renderTexture)
        {
            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

            var camera = CreateBlackCamera();
            var videoPlayer = CreateVideoPlayer(clip, renderTexture);
            var canvas = CreateCanvas(camera);
            CreateVideoSurface(canvas.transform, renderTexture);
            var fadeGroup = CreateFadeOverlay(canvas.transform);
            CreateController(videoPlayer, fadeGroup);

            EditorSceneManager.SaveScene(scene, ScenePath);
        }

        private static UnityEngine.Camera CreateBlackCamera()
        {
            var go = new GameObject("Camera");
            var cam = go.AddComponent<UnityEngine.Camera>();
            cam.orthographic = true;
            cam.clearFlags = CameraClearFlags.SolidColor;
            cam.backgroundColor = Color.black;     // letterbox bars when the window isn't 16:9
            go.transform.position = new Vector3(0, 0, -10);
            return cam;
        }

        private static VideoPlayer CreateVideoPlayer(VideoClip clip, RenderTexture renderTexture)
        {
            var go = new GameObject("VideoPlayer");
            var vp = go.AddComponent<VideoPlayer>();
            vp.playOnAwake = false;
            vp.isLooping = false;
            vp.waitForFirstFrame = true;
            vp.source = VideoSource.VideoClip;
            vp.clip = clip;
            vp.renderMode = VideoRenderMode.RenderTexture;
            vp.targetTexture = renderTexture;
            vp.audioOutputMode = VideoAudioOutputMode.None;   // the reveal has no audio track
            return vp;
        }

        // ScreenSpaceCamera (not Overlay) so the whole splash renders through the one camera —
        // keeps a single fullscreen composite and lets it be captured for verification.
        private static Canvas CreateCanvas(UnityEngine.Camera worldCamera)
        {
            var go = new GameObject("Canvas");
            var canvas = go.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceCamera;
            canvas.worldCamera = worldCamera;
            canvas.planeDistance = 1f;
            var scaler = go.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(CanvasWidth, CanvasHeight);
            scaler.matchWidthOrHeight = 0.5f;
            go.AddComponent<GraphicRaycaster>();
            return canvas;
        }

        // The video surface keeps the clip's 16:9 aspect via a fitter, so odd window shapes
        // letterbox against the black camera instead of stretching the logo.
        private static void CreateVideoSurface(Transform parent, RenderTexture renderTexture)
        {
            var go = new GameObject("VideoSurface", typeof(RectTransform));
            go.transform.SetParent(parent, false);

            var raw = go.AddComponent<RawImage>();
            raw.texture = renderTexture;
            raw.raycastTarget = false;
            StretchFull(go.GetComponent<RectTransform>());

            var fitter = go.AddComponent<AspectRatioFitter>();
            fitter.aspectMode = AspectRatioFitter.AspectMode.FitInParent;
            fitter.aspectRatio = (float)CanvasWidth / CanvasHeight;
        }

        private static CanvasGroup CreateFadeOverlay(Transform parent)
        {
            var go = new GameObject("FadeOverlay", typeof(RectTransform));
            go.transform.SetParent(parent, false);   // last sibling -> renders on top of the video

            var image = go.AddComponent<Image>();
            image.color = Color.black;
            image.raycastTarget = false;
            StretchFull(go.GetComponent<RectTransform>());

            var group = go.AddComponent<CanvasGroup>();
            group.alpha = 0f;
            return group;
        }

        private static void CreateController(VideoPlayer videoPlayer, CanvasGroup fadeGroup)
        {
            var go = new GameObject("SplashController");
            var controller = go.AddComponent<SplashScreenController>();
            SetField(controller, "_videoPlayer", videoPlayer);
            SetField(controller, "_fadeGroup", fadeGroup);
        }

        private static void SetInitialStateToSplash()
        {
            EditorSceneManager.OpenScene(BootScenePath);
            var gsm = Object.FindFirstObjectByType<GameStateManager>();
            if (gsm == null)
            {
                Debug.LogError("[SplashBuilder] GameStateManager not found in BootScene; initial state not set.");
                return;
            }
            SetField(gsm, "_initialState", GameState.Splash);
            EditorSceneManager.MarkSceneDirty(SceneManager.GetActiveScene());
            EditorSceneManager.SaveScene(SceneManager.GetActiveScene());
        }

        private static void AppendSplashTransition()
        {
            var guids = AssetDatabase.FindAssets("t:GameStateTransitionTable");
            if (guids.Length == 0)
            {
                Debug.LogError("[SplashBuilder] No GameStateTransitionTable asset found.");
                return;
            }

            var path = AssetDatabase.GUIDToAssetPath(guids[0]);
            var table = AssetDatabase.LoadAssetAtPath<GameStateTransitionTable>(path);
            var so = new SerializedObject(table);
            var list = so.FindProperty("validTransitions");

            for (int i = 0; i < list.arraySize; i++)
            {
                var entry = list.GetArrayElementAtIndex(i);
                if (entry.FindPropertyRelative("From").enumValueIndex == (int)GameState.Splash &&
                    entry.FindPropertyRelative("To").enumValueIndex == (int)GameState.TitleScreen)
                    return;   // already present
            }

            list.InsertArrayElementAtIndex(list.arraySize);
            var added = list.GetArrayElementAtIndex(list.arraySize - 1);
            added.FindPropertyRelative("From").enumValueIndex = (int)GameState.Splash;
            added.FindPropertyRelative("To").enumValueIndex = (int)GameState.TitleScreen;
            so.ApplyModifiedProperties();
        }

        private static void RegisterSceneInBuildSettings()
        {
            var scenes = EditorBuildSettings.scenes.ToList();
            if (scenes.Any(s => s.path == ScenePath)) return;
            scenes.Add(new EditorBuildSettingsScene(ScenePath, true));
            EditorBuildSettings.scenes = scenes.ToArray();
        }

        private static void DisableUnityEngineSplash()
        {
            PlayerSettings.SplashScreen.show = false;
            PlayerSettings.SplashScreen.showUnityLogo = false;
        }

        private static void StretchFull(RectTransform rect)
        {
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;
        }

        private static void SetField(object target, string fieldName, object value)
        {
            var type = target.GetType();
            while (type != null)
            {
                var field = type.GetField(fieldName,
                    System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                if (field != null)
                {
                    field.SetValue(target, value);
                    if (target is Object uObj) EditorUtility.SetDirty(uObj);
                    return;
                }
                type = type.BaseType;
            }
            Debug.LogWarning($"[SplashBuilder] Field '{fieldName}' not found on {target.GetType().Name}.");
        }
    }
}
