using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using ProjectAstra.Core.Grid;

namespace ProjectAstra.Core.Editor
{
    // The Environment Builder: author a map's animated environment as a normal
    // prefab, in a dedicated scene, against a WYSIWYG backdrop of the map's real
    // base art + grid + unit markers. Full Unity scene freedom (any component,
    // particle, volume, or custom script; place anything anywhere, even off-map);
    // press Play to preview. Save writes the Environment root to a per-map prefab
    // and wires MapData.environmentPrefab. Steps 1-6 (Map Editor) stay data-driven;
    // this is the open, creative half.
    public sealed class MapEnvironmentBuilderWindow : EditorWindow
    {
        private const string BuilderScenePath = EnvironmentBackdropGuard.BuilderScenePath;
        private const string PrefabFolder = "Assets/Prefabs/Environment";
        private const string LastMapKey = "ProjectAstra.EnvBuilder.LastMap";
        private const string BackdropName = EnvironmentBackdropGuard.BackdropName;
        private const string EnvironmentName = "Environment";
        private const string CameraName = "Builder Camera";
        private const int MapPPU = 32;
        private const int RefResY = 270;   // matches the game's pixel-perfect camera (480×270 @ 32 PPU)

        private MapData target;

        [MenuItem("Project Astra/Map/Environment Builder")]
        public static void Open()
        {
            var window = GetWindow<MapEnvironmentBuilderWindow>("Environment Builder");
            window.minSize = new Vector2(360, 420);
        }

        private void OnEnable()
        {
            target = LoadLastMap();
            SceneView.duringSceneGui += DrawSceneOverlay;
        }

        private void OnDisable() => SceneView.duringSceneGui -= DrawSceneOverlay;

        // --- GUI ---

        private void OnGUI()
        {
            EditorGUILayout.LabelField("Environment Builder", EditorStyles.boldLabel);
            EditorGUILayout.HelpBox(
                "Author a map's animated environment as a prefab. Build under 'Environment', press Play to preview, then Save.",
                MessageType.None);

            var picked = (MapData)EditorGUILayout.ObjectField("Map", target, typeof(MapData), false);
            if (picked != target) { target = picked; RememberMap(); }

            if (!InBuilderScene())
            {
                EditorGUILayout.Space();
                if (GUILayout.Button("Open Builder Scene")) OpenBuilderScene();
                EditorGUILayout.HelpBox("Open the builder scene to author against the map backdrop.", MessageType.Info);
                return;
            }

            using (new EditorGUI.DisabledScope(target == null))
            {
                EditorGUILayout.Space();
                if (GUILayout.Button("Load Backdrop (base art + grid + units)")) LoadBackdrop();
                DrawPivotWarning();

                EditorGUILayout.Space();
                EditorGUILayout.LabelField("Environment", EditorStyles.boldLabel);
                EditorGUILayout.BeginHorizontal();
                if (GUILayout.Button("New")) NewEnvironment();
                using (new EditorGUI.DisabledScope(target != null && target.EnvironmentPrefab == null))
                    if (GUILayout.Button("Load Existing")) LoadExistingEnvironment();
                EditorGUILayout.EndHorizontal();
                if (GUILayout.Button("Add River Patch (auto-size to water band)")) AddRiverPatch();
                if (GUILayout.Button("Save → MapData.environmentPrefab")) SaveEnvironment();
            }

            EditorGUILayout.Space();
            EditorGUILayout.HelpBox(
                "Sorting: base-layer river → Ground (order 10); ground deco → Object; birds over units → Sky.",
                MessageType.None);
        }

        private void DrawPivotWarning()
        {
            if (target == null || target.BaseArt == null) return;
            Sprite s = target.BaseArt;
            Vector2 pivot = s.rect.width > 0 && s.rect.height > 0
                ? new Vector2(s.pivot.x / s.rect.width, s.pivot.y / s.rect.height)
                : new Vector2(0.5f, 0.5f);
            if (pivot == Vector2.zero) return;

            EditorGUILayout.HelpBox(
                $"Base art pivot is {pivot} (expected BottomLeft 0,0). Decorations authored here won't line up at runtime.",
                MessageType.Warning);
            if (GUILayout.Button("Fix base-art pivot to BottomLeft")) FixBaseArtPivot();
        }

        // --- Backdrop (editor-only, never saved) ---

        private void LoadBackdrop()
        {
            ClearBackdrops();

            var backdrop = new GameObject(BackdropName) { hideFlags = HideFlags.DontSave | HideFlags.NotEditable };
            // Bind it to the builder scene so it dies when that scene unloads (opening
            // another scene / booting the game). DontSave still keeps it out of the .unity
            // file and the prefab — but it can no longer go scene-less and leak into play.
            SceneManager.MoveGameObjectToScene(backdrop, SceneManager.GetActiveScene());

            var artGO = new GameObject("BaseArt");
            artGO.transform.SetParent(backdrop.transform, false);
            var sr = artGO.AddComponent<SpriteRenderer>();
            sr.sprite = target.BaseArt;                 // rendered exactly as runtime (honors its own pivot)
            sr.sortingLayerName = "Ground";
            sr.sortingOrder = 0;

            FrameGameCameraOnMap();
            FrameSceneOnMap();
            SceneView.RepaintAll();
        }

        // Clears any existing backdrop before painting a fresh one. The always-on
        // EnvironmentBackdropGuard owns the actual sweep (and also runs it on scene
        // change / play-enter, which is what stops the backdrop leaking).
        private static void ClearBackdrops() => EnvironmentBackdropGuard.ClearAllBackdrops();

        // Grid lines + unit markers, drawn in the scene view (no runtime component needed).
        private void DrawSceneOverlay(SceneView view)
        {
            if (target == null || GameObject.Find(BackdropName) == null) return;
            int w = target.Width, h = target.Height;

            Handles.color = new Color(1f, 1f, 1f, 0.15f);
            for (int x = 0; x <= w; x++) Handles.DrawLine(new Vector3(x, 0, 0), new Vector3(x, h, 0));
            for (int y = 0; y <= h; y++) Handles.DrawLine(new Vector3(0, y, 0), new Vector3(w, y, 0));

            foreach (UnitStartPosition u in target.UnitStartPositions)
            {
                Handles.color = TeamColor(u.team);
                Vector3 c = new(u.position.x + 0.5f, u.position.y + 0.5f, 0f);
                Handles.DrawWireCube(c, Vector3.one * 0.8f);
            }
        }

        private static Color TeamColor(int team) => team switch
        {
            1 => new Color(0.9f, 0.3f, 0.3f),
            2 => new Color(0.3f, 0.8f, 0.45f),
            _ => new Color(0.35f, 0.6f, 1f),
        };

        // --- Environment root ---

        private void NewEnvironment()
        {
            if (GameObject.Find(EnvironmentName) != null &&
                !EditorUtility.DisplayDialog("Replace Environment?",
                    "An Environment root already exists in the scene. Replace it with an empty one?", "Replace", "Cancel"))
                return;

            GameObject existing = GameObject.Find(EnvironmentName);
            if (existing != null) DestroyImmediate(existing);
            Selection.activeGameObject = new GameObject(EnvironmentName);
        }

        private void LoadExistingEnvironment()
        {
            GameObject existing = GameObject.Find(EnvironmentName);
            if (existing != null) DestroyImmediate(existing);

            var instance = (GameObject)PrefabUtility.InstantiatePrefab(target.EnvironmentPrefab);
            instance.name = EnvironmentName;
            instance.transform.position = Vector3.zero;
            Selection.activeGameObject = instance;
        }

        private void SaveEnvironment()
        {
            GameObject env = GameObject.Find(EnvironmentName);
            if (env == null) { EditorUtility.DisplayDialog("No Environment", "Create an Environment root first (New).", "OK"); return; }
            if (HasBackdropInside(env.transform))
            { EditorUtility.DisplayDialog("Backdrop inside Environment", "Move the backdrop out — it must not be saved into the prefab.", "OK"); return; }

            EnsureFolder(PrefabFolder);
            string path = $"{PrefabFolder}/{SafeId(target.MapId)}.prefab";
            GameObject prefab = PrefabUtility.SaveAsPrefabAsset(env, path, out bool ok);
            if (!ok || prefab == null) { Debug.LogError($"[EnvBuilder] Failed to save prefab at {path}."); return; }

            var so = new SerializedObject(target);
            so.FindProperty("environmentPrefab").objectReferenceValue = prefab;
            so.ApplyModifiedProperties();
            EditorUtility.SetDirty(target);
            AssetDatabase.SaveAssets();
            Debug.Log($"[EnvBuilder] Saved {path} and wired '{target.MapName}'.environmentPrefab.");
        }

        // Scaffolds a base-layer river: a tiled sprite on Ground (order 10, above the
        // base art, below everything) sized to the map's water band. The designer just
        // drops the river sprite + animator controller onto it.
        private void AddRiverPatch()
        {
            GameObject env = GameObject.Find(EnvironmentName);
            if (env == null) { EditorUtility.DisplayDialog("No Environment", "Create an Environment root first (New).", "OK"); return; }

            WaterBand band = WaterBand.Detect(target);
            var river = new GameObject("River");
            river.transform.SetParent(env.transform, false);
            river.transform.localPosition = new Vector3(target.Width / 2f, band.CenterY, 0f);

            var sr = river.AddComponent<SpriteRenderer>();
            sr.sortingLayerName = "Ground";
            sr.sortingOrder = 10;
            sr.drawMode = SpriteDrawMode.Tiled;
            sr.size = new Vector2(target.Width, band.Height);

            Selection.activeGameObject = river;
            Debug.Log($"[EnvBuilder] Added a River patch ({target.Width}×{band.Height}) over the water band — assign its river sprite + animator.");
        }

        private static bool HasBackdropInside(Transform root)
        {
            foreach (Transform t in root.GetComponentsInChildren<Transform>(true))
                if (t.name == BackdropName) return true;
            return false;
        }

        // --- Base-art pivot fix (mirrors the Map Editor importer rule) ---

        private void FixBaseArtPivot()
        {
            string path = AssetDatabase.GetAssetPath(target.BaseArt);
            if (AssetImporter.GetAtPath(path) is not TextureImporter importer) return;

            var settings = new TextureImporterSettings();
            importer.ReadTextureSettings(settings);
            settings.spriteAlignment = (int)SpriteAlignment.BottomLeft;
            importer.SetTextureSettings(settings);
            importer.SaveAndReimport();
            LoadBackdrop();
        }

        // --- Scene + helpers ---

        private void OpenBuilderScene()
        {
            if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;
            if (File.Exists(BuilderScenePath)) EditorSceneManager.OpenScene(BuilderScenePath);
            else CreateBuilderScene();
        }

        private void CreateBuilderScene()
        {
            Scene scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            var camGO = new GameObject(CameraName);
            var cam = camGO.AddComponent<UnityEngine.Camera>();
            camGO.tag = "MainCamera";
            ConfigureGameCamera(cam);
            new GameObject(EnvironmentName);

            Directory.CreateDirectory(Path.GetDirectoryName(Path.Combine(Application.dataPath, "..", BuilderScenePath)));
            EditorSceneManager.SaveScene(scene, BuilderScenePath);
        }

        // Makes the Game view frame the map exactly like the real battle: MapCamera
        // gives pixel-perfect 480×270 framing at Play, and the matching ortho size +
        // map-centred position make the edit-mode preview identical (no negative space).
        private void ConfigureGameCamera(UnityEngine.Camera cam)
        {
            if (cam == null) return;
            cam.orthographic = true;
            cam.orthographicSize = RefResY / (2f * MapPPU);   // 270 / 64 = 4.21875
            if (cam.GetComponent<ProjectAstra.Core.Camera.MapCamera>() == null)
                cam.gameObject.AddComponent<ProjectAstra.Core.Camera.MapCamera>();

            float x = target != null ? target.Width / 2f : 7.5f;
            float y = target != null ? target.Height / 2f : 4f;
            cam.transform.position = new Vector3(x, y, -10f);
        }

        private void FrameGameCameraOnMap()
        {
            GameObject camGO = GameObject.Find(CameraName);
            if (camGO != null) ConfigureGameCamera(camGO.GetComponent<UnityEngine.Camera>());
        }

        private void FrameSceneOnMap()
        {
            if (SceneView.lastActiveSceneView == null) return;
            var center = new Vector3(target.Width / 2f, target.Height / 2f, 0f);
            SceneView.lastActiveSceneView.LookAt(center, Quaternion.identity, Mathf.Max(target.Width, target.Height) * 0.6f);
        }

        private static bool InBuilderScene() =>
            SceneManager.GetActiveScene().path == BuilderScenePath;

        private MapData LoadLastMap()
        {
            string guid = EditorPrefs.GetString(LastMapKey, "");
            return string.IsNullOrEmpty(guid) ? null
                : AssetDatabase.LoadAssetAtPath<MapData>(AssetDatabase.GUIDToAssetPath(guid));
        }

        private void RememberMap()
        {
            if (target == null) { EditorPrefs.DeleteKey(LastMapKey); return; }
            EditorPrefs.SetString(LastMapKey, AssetDatabase.AssetPathToGUID(AssetDatabase.GetAssetPath(target)));
        }

        private static string SafeId(string mapId) =>
            string.IsNullOrEmpty(mapId) ? "map" : mapId.Replace(" ", "_");

        private static void EnsureFolder(string path)
        {
            if (AssetDatabase.IsValidFolder(path)) return;
            string parent = Path.GetDirectoryName(path).Replace("\\", "/");
            string leaf = Path.GetFileName(path);
            if (!AssetDatabase.IsValidFolder(parent)) EnsureFolder(parent);
            AssetDatabase.CreateFolder(parent, leaf);
        }
    }
}
