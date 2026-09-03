using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;
using ProjectAstra.Core.Camera;
using ProjectAstra.Core.Dialogue;
using ProjectAstra.Core.Hub;
using ProjectAstra.Core.Hub.Events;
using ProjectAstra.Core.Quests;
using ProjectAstra.Core.UI.Hub;
using ProjectAstra.Core.UI.Dialogue.Choice;
using ProjectAstra.Core.UI.Hub.Marker;
using ProjectAstra.Core.Units;

namespace ProjectAstra.Core.Editor
{
    // Builds the Hub scene from scratch. Rebuilding replaces the file, so keep hand-placed work out.
    public static class HubSceneBuilder
    {
        private const string ScenePath = "Assets/Scenes/HubExploration.unity";

        [MenuItem("Project Astra/Hub/Build Hub Scene")]
        public static void BuildScene()
        {
            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

            HubCameraController cameraRig = CreateCamera();
            HubLocationHost host = CreateLocationHost();
            CreateHubRoot(host, cameraRig);
            WireHud();

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

        private static void CreateHubRoot(HubLocationHost host, HubCameraController cameraRig)
        {
            var go = new GameObject("Hub");
            var loader = go.AddComponent<HubLocationLoader>();
            var transition = go.AddComponent<HubLocationTransition>();
            var markers = go.AddComponent<HubMarkerManager>();
            var events = go.AddComponent<HubEventRunner>();
            var areaTriggers = go.AddComponent<HubAreaTriggerWatcher>();
            var departures = go.AddComponent<HubDepartureController>();
            var questWorld = go.AddComponent<HubQuestWorld>();
            var quests = go.AddComponent<QuestManager>();
            var bootstrapper = go.AddComponent<HubBootstrapper>();

            var cast = new GameObject("Cast").transform;
            var playerRoot = new GameObject("Player").transform;

            WireLoader(loader, host, cast);
            WireTransition(transition, loader);
            WireBootstrapper(bootstrapper, loader, cameraRig, playerRoot);
            WireDeparture(departures, events);
            WireMarkers(markers, cameraRig);
            WireEvents(events, areaTriggers, cameraRig, loader);
            WireQuests(quests, questWorld, events);
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

        private static void WireTransition(HubLocationTransition transition, HubLocationLoader loader)
        {
            var serialized = new SerializedObject(transition);
            serialized.FindProperty("loader").objectReferenceValue = loader;
            serialized.ApplyModifiedProperties();
        }

        private static void WireBootstrapper(HubBootstrapper bootstrapper, HubLocationLoader loader,
            HubCameraController cameraRig, Transform playerRoot)
        {
            var serialized = new SerializedObject(bootstrapper);
            serialized.FindProperty("loader").objectReferenceValue = loader;
            serialized.FindProperty("cameraRig").objectReferenceValue = cameraRig;
            serialized.FindProperty("playerRoot").objectReferenceValue = playerRoot;
            serialized.FindProperty("events").objectReferenceValue = Object.FindFirstObjectByType<HubEventRunner>();
            serialized.FindProperty("scriptCatalog").objectReferenceValue = FindAsset<DialogueScriptCatalog>();
            serialized.FindProperty("fallbackVisit").objectReferenceValue = FindAsset<HubVisitData>();
            serialized.ApplyModifiedProperties();
        }

        private static void WireDeparture(HubDepartureController departures, HubEventRunner events)
        {
            var serialized = new SerializedObject(departures);
            serialized.FindProperty("events").objectReferenceValue = events;
            serialized.ApplyModifiedProperties();
        }

        private static void WireEvents(HubEventRunner events, HubAreaTriggerWatcher areaTriggers,
            HubCameraController cameraRig, HubLocationLoader loader)
        {
            var eventDatabase = FindAsset<HubEventDatabase>();

            var runner = new SerializedObject(events);
            runner.FindProperty("scriptCatalog").objectReferenceValue = FindAsset<DialogueScriptCatalog>();
            runner.FindProperty("eventDatabase").objectReferenceValue = eventDatabase;
            runner.FindProperty("cameraRig").objectReferenceValue = cameraRig;
            runner.ApplyModifiedProperties();

            var watcher = new SerializedObject(areaTriggers);
            watcher.FindProperty("events").objectReferenceValue = events;
            watcher.FindProperty("eventDatabase").objectReferenceValue = eventDatabase;
            watcher.FindProperty("loader").objectReferenceValue = loader;
            watcher.ApplyModifiedProperties();
        }

        private static void WireQuests(QuestManager quests, HubQuestWorld world, HubEventRunner events)
        {
            var manager = new SerializedObject(quests);
            manager.FindProperty("catalog").objectReferenceValue = FindAsset<QuestCatalog>();
            manager.FindProperty("worldBehaviour").objectReferenceValue = world;
            manager.ApplyModifiedProperties();

            var questWorld = new SerializedObject(world);
            questWorld.FindProperty("events").objectReferenceValue = events;
            questWorld.FindProperty("scriptCatalog").objectReferenceValue = FindAsset<DialogueScriptCatalog>();
            questWorld.ApplyModifiedProperties();
        }

        private static void WireMarkers(HubMarkerManager markers, HubCameraController cameraRig)
        {
            var serialized = new SerializedObject(markers);
            serialized.FindProperty("cameraRig").objectReferenceValue = cameraRig;
            serialized.ApplyModifiedProperties();
        }

        private static void WireHud()
        {
            HubHUDController hud = HubHudBuilder.Build(out ChoiceMenuView choiceMenu,
                out EdgeIndicatorView edgeIndicators);

            var serialized = new SerializedObject(hud);
            serialized.FindProperty("player").objectReferenceValue = null;
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
