using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;
using ProjectAstra.Core.Camera;
using ProjectAstra.Core.Gurukul;
using ProjectAstra.Core.Dialogue.Conversation;
using ProjectAstra.Core.Gurukul.Events;
using ProjectAstra.Core.UI.Gurukul;
using ProjectAstra.Core.UI.Dialogue.Choice;
using ProjectAstra.Core.UI.Gurukul.Marker;
using ProjectAstra.Core.Units;

namespace ProjectAstra.Core.Editor
{
    // Builds the Gurukul scene: the pixel-perfect camera the hub shares with the battle map, the
    // transform rooms get instantiated under, and the two components that drive a visit.
    //
    // No 2D light, deliberately — the battle map has none either; its map and unit sprites are
    // unlit, so adding one would only make the hub read differently from every other scene.
    //
    // Rebuilding replaces the scene file, so keep anything hand-placed out of it.
    public static class GurukulSceneBuilder
    {
        private const string ScenePath = "Assets/Scenes/HubExploration.unity";

        [MenuItem("Project Astra/Gurukul/Build Hub Scene")]
        public static void BuildScene()
        {
            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

            GurukulCameraRig cameraRig = CreateCamera();
            GurukulLocationHost host = CreateLocationHost();
            GurukulScreenFade fade = CreateFadeOverlay();
            GurukulInteractionDriver driver = CreateHubRoot(host, cameraRig, fade);
            WireHud(driver);

            EditorSceneManager.SaveScene(scene, ScenePath);
            GurukulSetupTool.RunSetup();
            Debug.Log($"[GurukulSceneBuilder] Built {ScenePath}.");
        }

        // Greybox art and data first, then the scene that points at them.
        [MenuItem("Project Astra/Gurukul/Build Everything (Greybox)")]
        public static void BuildEverything()
        {
            GurukulGreyboxBuilder.Build();
            BuildScene();
        }

        // MapCamera owns the 480x270 / 32 PPU pixel-perfect setup and adds the PixelPerfectCamera
        // itself on Awake, so the hub reads at the same scale without restating any of it.
        private static GurukulCameraRig CreateCamera()
        {
            var go = new GameObject("Main Camera") { tag = "MainCamera" };
            var camera = go.AddComponent<UnityEngine.Camera>();
            camera.orthographic = true;
            camera.clearFlags = CameraClearFlags.SolidColor;
            camera.backgroundColor = Color.black;
            go.transform.position = new Vector3(0f, 0f, -10f);

            go.AddComponent<MapCamera>();
            return go.AddComponent<GurukulCameraRig>();
        }

        private static GurukulLocationHost CreateLocationHost()
        {
            var go = new GameObject("LocationHost");
            return go.AddComponent<GurukulLocationHost>();
        }

        private static GurukulInteractionDriver CreateHubRoot(GurukulLocationHost host, GurukulCameraRig cameraRig,
            GurukulScreenFade fade)
        {
            var go = new GameObject("Gurukul");
            var router = go.AddComponent<GurukulInputRouter>();
            var driver = go.AddComponent<GurukulInteractionDriver>();
            var conversations = go.AddComponent<ConversationPlayer>();
            var director = go.AddComponent<GurukulVisitDirector>();
            var loader = go.AddComponent<GurukulLocationLoader>();
            var transition = go.AddComponent<GurukulLocationTransition>();
            var markers = go.AddComponent<GurukulMarkerManager>();
            var events = go.AddComponent<GurukulEventRunner>();
            var areaTriggers = go.AddComponent<GurukulAreaTriggerWatcher>();
            var departures = go.AddComponent<GurukulDepartureController>();
            var bootstrapper = go.AddComponent<GurukulBootstrapper>();

            var cast = new GameObject("Cast").transform;
            var playerRoot = new GameObject("Player").transform;

            WireLoader(loader, host, cast);
            WireTransition(transition, router, loader, fade);
            WireBootstrapper(bootstrapper, loader, cameraRig, router, driver, playerRoot);
            WireConversationPlayer(conversations);
            WireDeparture(departures, router);
            WireMarkers(markers, router, cameraRig);
            WireDirector(director, driver, conversations, transition, events, departures);
            WireEvents(events, areaTriggers, router, conversations, cameraRig, loader);
            return driver;
        }

        private static void WireLoader(GurukulLocationLoader loader, GurukulLocationHost host, Transform cast)
        {
            var serialized = new SerializedObject(loader);
            serialized.FindProperty("locationHost").objectReferenceValue = host;
            serialized.FindProperty("locationDatabase").objectReferenceValue = FindAsset<GurukulLocationDatabase>();
            serialized.FindProperty("unitDatabase").objectReferenceValue = FindAsset<UnitDatabase>();
            serialized.FindProperty("castRoot").objectReferenceValue = cast;
            serialized.ApplyModifiedProperties();
        }

        private static void WireTransition(GurukulLocationTransition transition, GurukulInputRouter router,
            GurukulLocationLoader loader, GurukulScreenFade fade)
        {
            var serialized = new SerializedObject(transition);
            serialized.FindProperty("router").objectReferenceValue = router;
            serialized.FindProperty("loader").objectReferenceValue = loader;
            serialized.FindProperty("fade").objectReferenceValue = fade;
            serialized.ApplyModifiedProperties();
        }

        private static void WireBootstrapper(GurukulBootstrapper bootstrapper, GurukulLocationLoader loader,
            GurukulCameraRig cameraRig, GurukulInputRouter router, GurukulInteractionDriver driver, Transform playerRoot)
        {
            var serialized = new SerializedObject(bootstrapper);
            serialized.FindProperty("loader").objectReferenceValue = loader;
            serialized.FindProperty("cameraRig").objectReferenceValue = cameraRig;
            serialized.FindProperty("router").objectReferenceValue = router;
            serialized.FindProperty("interactionDriver").objectReferenceValue = driver;
            serialized.FindProperty("playerRoot").objectReferenceValue = playerRoot;
            serialized.FindProperty("events").objectReferenceValue = Object.FindFirstObjectByType<GurukulEventRunner>();
            serialized.FindProperty("director").objectReferenceValue = Object.FindFirstObjectByType<GurukulVisitDirector>();
            serialized.FindProperty("fallbackVisit").objectReferenceValue = FindAsset<GurukulVisitData>();
            serialized.ApplyModifiedProperties();
        }

        // Its own canvas above the HUD, so a doorway fade covers the objective line and the prompt
        // as well as the world.
        private static GurukulScreenFade CreateFadeOverlay()
        {
            var go = new GameObject("FadeOverlay", typeof(Canvas), typeof(CanvasGroup));

            var canvas = go.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 100;

            var black = new GameObject("Black", typeof(RectTransform), typeof(Image));
            black.transform.SetParent(go.transform, false);
            black.GetComponent<Image>().color = Color.black;

            var rect = black.GetComponent<RectTransform>();
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;

            return go.AddComponent<GurukulScreenFade>();
        }

        private static void WireConversationPlayer(ConversationPlayer player)
        {
            var serialized = new SerializedObject(player);
            serialized.FindProperty("conversationDatabase").objectReferenceValue = FindAsset<ConversationGraphDatabase>();
            serialized.ApplyModifiedProperties();
        }

        private static void WireDirector(GurukulVisitDirector director, GurukulInteractionDriver driver,
            ConversationPlayer conversations, GurukulLocationTransition transitions,
            GurukulEventRunner events, GurukulDepartureController departures)
        {
            var serialized = new SerializedObject(director);
            serialized.FindProperty("interactionDriver").objectReferenceValue = driver;
            serialized.FindProperty("conversations").objectReferenceValue = conversations;
            serialized.FindProperty("transitions").objectReferenceValue = transitions;
            serialized.FindProperty("events").objectReferenceValue = events;
            serialized.FindProperty("departures").objectReferenceValue = departures;
            serialized.ApplyModifiedProperties();
        }

        private static void WireDeparture(GurukulDepartureController departures, GurukulInputRouter router)
        {
            var serialized = new SerializedObject(departures);
            serialized.FindProperty("router").objectReferenceValue = router;
            serialized.ApplyModifiedProperties();
        }

        private static void WireEvents(GurukulEventRunner events, GurukulAreaTriggerWatcher areaTriggers,
            GurukulInputRouter router, ConversationPlayer conversations, GurukulCameraRig cameraRig,
            GurukulLocationLoader loader)
        {
            var eventDatabase = FindAsset<GurukulEventDatabase>();

            var runner = new SerializedObject(events);
            runner.FindProperty("router").objectReferenceValue = router;
            runner.FindProperty("conversations").objectReferenceValue = conversations;
            runner.FindProperty("eventDatabase").objectReferenceValue = eventDatabase;
            runner.FindProperty("cameraRig").objectReferenceValue = cameraRig;
            runner.ApplyModifiedProperties();

            var watcher = new SerializedObject(areaTriggers);
            watcher.FindProperty("router").objectReferenceValue = router;
            watcher.FindProperty("events").objectReferenceValue = events;
            watcher.FindProperty("eventDatabase").objectReferenceValue = eventDatabase;
            watcher.FindProperty("loader").objectReferenceValue = loader;
            watcher.ApplyModifiedProperties();
        }

        private static void WireMarkers(GurukulMarkerManager markers, GurukulInputRouter router,
            GurukulCameraRig cameraRig)
        {
            var serialized = new SerializedObject(markers);
            serialized.FindProperty("router").objectReferenceValue = router;
            serialized.FindProperty("cameraRig").objectReferenceValue = cameraRig;
            serialized.ApplyModifiedProperties();
        }

        private static void WireHud(GurukulInteractionDriver driver)
        {
            GurukulHUDController hud = GurukulHudBuilder.Build(out ChoiceMenuView choiceMenu,
                out EdgeIndicatorView edgeIndicators);

            var serialized = new SerializedObject(hud);
            serialized.FindProperty("interactionDriver").objectReferenceValue = driver;
            serialized.FindProperty("router").objectReferenceValue = Object.FindFirstObjectByType<GurukulInputRouter>();
            serialized.ApplyModifiedProperties();

            var player = Object.FindFirstObjectByType<ConversationPlayer>();
            var playerSerialized = new SerializedObject(player);
            playerSerialized.FindProperty("choiceView").objectReferenceValue = choiceMenu;
            playerSerialized.ApplyModifiedProperties();

            var markers = Object.FindFirstObjectByType<GurukulMarkerManager>();
            var markerSerialized = new SerializedObject(markers);
            markerSerialized.FindProperty("edgeIndicators").objectReferenceValue = edgeIndicators;
            markerSerialized.ApplyModifiedProperties();
        }

        private static T FindAsset<T>() where T : Object
        {
            string[] guids = AssetDatabase.FindAssets($"t:{typeof(T).Name}");
            if (guids.Length == 0)
            {
                Debug.LogWarning($"[GurukulSceneBuilder] No {typeof(T).Name} found — wire it on the bootstrapper by hand.");
                return null;
            }
            return AssetDatabase.LoadAssetAtPath<T>(AssetDatabase.GUIDToAssetPath(guids[0]));
        }
    }
}
