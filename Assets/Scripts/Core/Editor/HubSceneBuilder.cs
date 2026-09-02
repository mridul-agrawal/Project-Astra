using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;
using ProjectAstra.Core.Camera;
using ProjectAstra.Core.Dialogue;
using ProjectAstra.Core.Hub;
using ProjectAstra.Core.Hub.Events;
using ProjectAstra.Core.UI.Hub;
using ProjectAstra.Core.UI.Dialogue.Choice;
using ProjectAstra.Core.UI.Hub.Marker;
using ProjectAstra.Core.Units;

namespace ProjectAstra.Core.Editor
{
    // Builds the Hub scene: the pixel-perfect camera the hub shares with the battle map, the
    // transform rooms get instantiated under, and the two components that drive a visit.
    //
    // No 2D light, deliberately — the battle map has none either; its map and unit sprites are
    // unlit, so adding one would only make the hub read differently from every other scene.
    //
    // Rebuilding replaces the scene file, so keep anything hand-placed out of it.
    public static class HubSceneBuilder
    {
        private const string ScenePath = "Assets/Scenes/HubExploration.unity";

        [MenuItem("Project Astra/Hub/Build Hub Scene")]
        public static void BuildScene()
        {
            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

            HubCameraController cameraRig = CreateCamera();
            HubLocationHost host = CreateLocationHost();
            HubInteractionDriver driver = CreateHubRoot(host, cameraRig);
            WireHud(driver);

            EditorSceneManager.SaveScene(scene, ScenePath);
            HubSetupTool.RunSetup();
            Debug.Log($"[HubSceneBuilder] Built {ScenePath}.");
        }

        // Greybox art and data first, then the scene that points at them.
        [MenuItem("Project Astra/Hub/Build Everything (Greybox)")]
        public static void BuildEverything()
        {
            HubGreyboxBuilder.Build();
            BuildScene();
        }

        // MapCamera owns the 480x270 / 32 PPU pixel-perfect setup and adds the PixelPerfectCamera
        // itself on Awake, so the hub reads at the same scale without restating any of it.
        private static HubCameraController CreateCamera()
        {
            var go = new GameObject("Main Camera") { tag = "MainCamera" };
            var camera = go.AddComponent<UnityEngine.Camera>();
            camera.orthographic = true;
            camera.clearFlags = CameraClearFlags.SolidColor;
            camera.backgroundColor = Color.black;
            go.transform.position = new Vector3(0f, 0f, -10f);

            go.AddComponent<MapCamera>();
            return go.AddComponent<HubCameraController>();
        }

        private static HubLocationHost CreateLocationHost()
        {
            var go = new GameObject("LocationHost");
            return go.AddComponent<HubLocationHost>();
        }

        private static HubInteractionDriver CreateHubRoot(HubLocationHost host, HubCameraController cameraRig)
        {
            var go = new GameObject("Hub");
            var router = go.AddComponent<HubInputRouter>();
            var driver = go.AddComponent<HubInteractionDriver>();
            var director = go.AddComponent<HubVisitDirector>();
            var loader = go.AddComponent<HubLocationLoader>();
            var transition = go.AddComponent<HubLocationTransition>();
            var markers = go.AddComponent<HubMarkerManager>();
            var events = go.AddComponent<HubEventRunner>();
            var areaTriggers = go.AddComponent<HubAreaTriggerWatcher>();
            var departures = go.AddComponent<HubDepartureController>();
            var bootstrapper = go.AddComponent<HubBootstrapper>();

            var cast = new GameObject("Cast").transform;
            var playerRoot = new GameObject("Player").transform;

            WireLoader(loader, host, cast);
            WireTransition(transition, router, loader);
            WireBootstrapper(bootstrapper, loader, cameraRig, router, driver, playerRoot);
            WireDeparture(departures, router);
            WireMarkers(markers, router, cameraRig);
            WireDirector(director, driver, transition, events, departures);
            WireEvents(events, areaTriggers, router, cameraRig, loader);
            return driver;
        }

        private static void WireLoader(HubLocationLoader loader, HubLocationHost host, Transform cast)
        {
            var serialized = new SerializedObject(loader);
            serialized.FindProperty("locationHost").objectReferenceValue = host;
            serialized.FindProperty("locationDatabase").objectReferenceValue = FindAsset<HubLocationDatabase>();
            serialized.FindProperty("unitDatabase").objectReferenceValue = FindAsset<UnitDatabase>();
            serialized.FindProperty("castRoot").objectReferenceValue = cast;
            serialized.ApplyModifiedProperties();
        }

        private static void WireTransition(HubLocationTransition transition, HubInputRouter router,
            HubLocationLoader loader)
        {
            var serialized = new SerializedObject(transition);
            serialized.FindProperty("router").objectReferenceValue = router;
            serialized.FindProperty("loader").objectReferenceValue = loader;
            serialized.ApplyModifiedProperties();
        }

        private static void WireBootstrapper(HubBootstrapper bootstrapper, HubLocationLoader loader,
            HubCameraController cameraRig, HubInputRouter router, HubInteractionDriver driver, Transform playerRoot)
        {
            var serialized = new SerializedObject(bootstrapper);
            serialized.FindProperty("loader").objectReferenceValue = loader;
            serialized.FindProperty("cameraRig").objectReferenceValue = cameraRig;
            serialized.FindProperty("router").objectReferenceValue = router;
            serialized.FindProperty("interactionDriver").objectReferenceValue = driver;
            serialized.FindProperty("playerRoot").objectReferenceValue = playerRoot;
            serialized.FindProperty("events").objectReferenceValue = Object.FindFirstObjectByType<HubEventRunner>();
            serialized.FindProperty("director").objectReferenceValue = Object.FindFirstObjectByType<HubVisitDirector>();
            serialized.FindProperty("fallbackVisit").objectReferenceValue = FindAsset<HubVisitData>();
            serialized.ApplyModifiedProperties();
        }

        private static void WireDirector(HubVisitDirector director, HubInteractionDriver driver,
            HubLocationTransition transitions,
            HubEventRunner events, HubDepartureController departures)
        {
            var serialized = new SerializedObject(director);
            serialized.FindProperty("interactionDriver").objectReferenceValue = driver;
            serialized.FindProperty("scriptCatalog").objectReferenceValue = FindAsset<DialogueScriptCatalog>();
            serialized.FindProperty("transitions").objectReferenceValue = transitions;
            serialized.FindProperty("events").objectReferenceValue = events;
            serialized.FindProperty("departures").objectReferenceValue = departures;
            serialized.ApplyModifiedProperties();
        }

        private static void WireDeparture(HubDepartureController departures, HubInputRouter router)
        {
            var serialized = new SerializedObject(departures);
            serialized.FindProperty("router").objectReferenceValue = router;
            serialized.ApplyModifiedProperties();
        }

        private static void WireEvents(HubEventRunner events, HubAreaTriggerWatcher areaTriggers,
            HubInputRouter router, HubCameraController cameraRig,
            HubLocationLoader loader)
        {
            var eventDatabase = FindAsset<HubEventDatabase>();

            var runner = new SerializedObject(events);
            runner.FindProperty("router").objectReferenceValue = router;
            runner.FindProperty("scriptCatalog").objectReferenceValue = FindAsset<DialogueScriptCatalog>();
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

        private static void WireMarkers(HubMarkerManager markers, HubInputRouter router,
            HubCameraController cameraRig)
        {
            var serialized = new SerializedObject(markers);
            serialized.FindProperty("router").objectReferenceValue = router;
            serialized.FindProperty("cameraRig").objectReferenceValue = cameraRig;
            serialized.ApplyModifiedProperties();
        }

        private static void WireHud(HubInteractionDriver driver)
        {
            HubHUDController hud = HubHudBuilder.Build(out ChoiceMenuView choiceMenu,
                out EdgeIndicatorView edgeIndicators);

            var serialized = new SerializedObject(hud);
            serialized.FindProperty("interactionDriver").objectReferenceValue = driver;
            serialized.FindProperty("router").objectReferenceValue = Object.FindFirstObjectByType<HubInputRouter>();
            serialized.ApplyModifiedProperties();


            var markers = Object.FindFirstObjectByType<HubMarkerManager>();
            var markerSerialized = new SerializedObject(markers);
            markerSerialized.FindProperty("edgeIndicators").objectReferenceValue = edgeIndicators;
            markerSerialized.ApplyModifiedProperties();
        }

        private static T FindAsset<T>() where T : Object
        {
            string[] guids = AssetDatabase.FindAssets($"t:{typeof(T).Name}");
            if (guids.Length == 0)
            {
                Debug.LogWarning($"[HubSceneBuilder] No {typeof(T).Name} found — wire it on the bootstrapper by hand.");
                return null;
            }
            return AssetDatabase.LoadAssetAtPath<T>(AssetDatabase.GUIDToAssetPath(guids[0]));
        }
    }
}
