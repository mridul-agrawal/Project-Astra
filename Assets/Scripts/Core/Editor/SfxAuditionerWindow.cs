using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;
using ProjectAstra.Core.Audio;

namespace ProjectAstra.EditorTools
{
    // SFX Auditioner — find + assign clips for each SoundSO without surfing the
    // Project window. Pick a sound on the left; the right shows a curated shortlist
    // of candidate clips (filtered by keywords I picked per sound). ▶ auditions a
    // clip, + assigns it. "Harvest" copies any clips referenced from the gitignored
    // External Assets into Assets/Audio/UI and repoints them, so the SoundSOs commit
    // cleanly. Edit the keyword box to widen/narrow the candidate list at any time.
    public class SfxAuditionerWindow : EditorWindow
    {
        const string SoundSODir = "Assets/ScriptableObjects/Audio";
        const string SearchRoot = "Assets/External Assets";
        const string HarvestDir = "Assets/Audio/UI";

        // Curated default keywords per SoundSO name. Comma-separated; each term is a
        // filename substring searched under External Assets. Picked from the pack
        // catalogue — CelerisLab's named files map cleanly to most of these.
        static readonly Dictionary<string, string> DefaultKeywords = new()
        {
            { "UiConfirm",         "glass_crystal_button, metallic_button, soft_bubbly_button" },
            { "UiCancel",          "click_out, dropdown_menu, wooden_button" },
            { "CancelGrid",        "click_out, wooden_button, heavy_mechanical_button" },
            { "UiMove",            "hover, checkbox_tick" },
            { "CursorMove",        "Cursor, tick, Minimalist, hover, blip" },
            { "UiInvalid",         "access_denied, action_failed, error_message, warning, insufficient" },
            { "UiPanelOpen",       "inventory_open, dropdown_menu, notification" },
            { "UiPanelClose",      "inventory_close" },
            { "UiTab",             "shop_category, dropdown_menu" },
            { "UiHover",           "hover" },
            { "ConfirmStartGame",  "dramatic_button, game_state_change, achievement_unlocked, reward_crate_open" },
            { "ConfirmStartBattle","dramatic_button, game_state_change, objective_reached, mission_success" },
            { "ConfirmUnitSelect", "glass_crystal_button, metallic_button, checkbox_tick" },
            { "ConfirmMove",       "soft_bubbly_button, wooden_button, liquid_button" },
            { "ConfirmAction",     "metallic_button, glass_crystal_button, heavy_mechanical_button" },
            { "ConfirmEngage",     "dramatic_button, upgrade_applied, level_up, MELEE" },
            { "ConfirmItem",       "item_equip, item_pick, item_acquired, item_drop" },
            { "HitCrit",           "gore, punch, MELEE, HIT-, crit, impact" },
            { "HitPhysical",       "HIT-, MELEE, punch, slap, impact, smack" },
            { "Miss",              "whoosh, swish, dodge, air, MissWhoosh" },
            { "AttackSwing",       "WHSH, SWSH, swing, slash, Blade Swing, whoosh" },
            { "MagicCast",         "MAGSpel, CAST, magic, spell, Arcane, Aura" },
            { "UnitDeath",         "death, Die, gore, body, fall, DSGNImpt" },
            { "Heal",              "Heal, Healing, MAGAngl, restore, sparkle" },
            { "Footstep",          "Footstep, FEETMisc, STEP" },
            { "RakshasaRoar",      "roar, beast, creature, growl, monster, vocalisation" },
            { "VillagerScream",    "scream, shout, death, pain, grunt" },
            { "ExpTick",           "Minimalist, tick, coin, currency, blip" },
            { "LevelUp",           "level_up, upgrade_applied, Powerup, Buff Pickup, achievement" },
            { "BuffApplied",       "buff_applied, BUFF, Buff Pickup, Aura Up" },
            { "DebuffApplied",     "debuff_applied, Debuff, Speed Debuff" },
            { "ItemEquip",         "item_equip, equip, Mecha Upgrade Equip, Generic Item" },
            { "ItemMove",          "item_drag, item_stack, Swipe, Magic Swipe" },
            { "GoldGain",          "Coin, currency_gained, Coin Toss, purchase, transaction" },
            { "PhasePlayer",       "DSGNTonl, Achievement, objective_reached, Aura Rise" },
            { "PhaseEnemy",        "Deep Lurker, Minor, tense, Dark Energy" },
            { "PhaseAllied",       "DSGNTonl, Aura, neutral, Sphere Up" },
            { "Fanfare",           "Achievement, mission_success, quest_completed, fanfare" },
            { "DialogueBlip",      "Minimalist, Wood Block, tick, blip, type" },
            { "MusicTitle",        "MUSCMisc_Major, Cosmic Star, Piano, Tranquility" },
            { "MusicMap",          "Ambiences, Tiger Temple, Dark Dark Woods, Tranquility" },
            { "MusicBattle",       "Ambiences, Bloodrat, DSGNDron, Tranquility" },
            { "MusicVictory",      "Achievement, mission_success, quest_completed" },
            { "MusicGameOver",     "DSGNDron_Minor, Angelic Sleep, Resonating, minor" },
            { "AmbientWind",       "AMBIENCE, Tundra, Tunnel, wind, drone" },
            { "TransitionWhoosh",  "WHSH, Fly By, whoosh, Swipe, transition" },
            { "SplashSting",       "Breathy Startup, achievement_unlocked, dramatic, Achievement" },
        };

        SoundSO[] _sounds;
        string[] _names;
        int _selected;
        string _search = "";
        readonly List<AudioClip> _results = new();
        Vector2 _scrollList, _scrollResults;

        [MenuItem("Tools/Audio/SFX Auditioner")]
        static void Open()
        {
            var w = GetWindow<SfxAuditionerWindow>("SFX Auditioner");
            w.minSize = new Vector2(760, 440);
        }

        void OnEnable() => Reload();

        void Reload()
        {
            _sounds = AssetDatabase.FindAssets("t:SoundSO", new[] { SoundSODir })
                .Select(g => AssetDatabase.LoadAssetAtPath<SoundSO>(AssetDatabase.GUIDToAssetPath(g)))
                .Where(s => s != null)
                .OrderBy(s => s.name)
                .ToArray();
            _names = _sounds.Select(s => s.name).ToArray();
            if (_sounds.Length > 0) Select(Mathf.Clamp(_selected, 0, _sounds.Length - 1));
        }

        void Select(int i)
        {
            _selected = i;
            _search = DefaultKeywords.TryGetValue(_names[i], out var kw) ? kw : "";
            RunSearch();
        }

        void RunSearch()
        {
            _results.Clear();
            var seen = new HashSet<string>();
            foreach (var raw in _search.Split(','))
            {
                var term = raw.Trim();
                if (term.Length == 0) continue;
                foreach (var guid in AssetDatabase.FindAssets($"t:AudioClip {term}", new[] { SearchRoot }))
                {
                    var path = AssetDatabase.GUIDToAssetPath(guid);
                    if (!seen.Add(path)) continue;
                    var clip = AssetDatabase.LoadAssetAtPath<AudioClip>(path);
                    if (clip != null) _results.Add(clip);
                }
            }
        }

        void OnGUI()
        {
            if (_sounds == null || _sounds.Length == 0)
            {
                EditorGUILayout.HelpBox($"No SoundSO assets found under {SoundSODir}.", MessageType.Info);
                if (GUILayout.Button("Reload")) Reload();
                return;
            }

            EditorGUILayout.BeginHorizontal();
            DrawSoundList();
            DrawDetailPane();
            EditorGUILayout.EndHorizontal();
        }

        void DrawSoundList()
        {
            EditorGUILayout.BeginVertical(GUILayout.Width(210));
            EditorGUILayout.LabelField("Sounds  (assigned)", EditorStyles.boldLabel);
            _scrollList = EditorGUILayout.BeginScrollView(_scrollList);
            for (int i = 0; i < _sounds.Length; i++)
            {
                int count = ClipCount(_sounds[i]);
                var prev = GUI.backgroundColor;
                if (i == _selected) GUI.backgroundColor = new Color(0.45f, 0.65f, 1f);
                else if (count == 0) GUI.backgroundColor = new Color(1f, 0.85f, 0.6f);
                if (GUILayout.Button($"{_names[i]}  ({count})", EditorStyles.miniButton)) Select(i);
                GUI.backgroundColor = prev;
            }
            EditorGUILayout.EndScrollView();
            EditorGUILayout.Space();
            if (GUILayout.Button("⟳ Reload")) Reload();
            if (GUILayout.Button("⬇ Harvest → Assets/Audio\n(make commit-safe)", GUILayout.Height(36))) Harvest();
            EditorGUILayout.EndVertical();
        }

        void DrawDetailPane()
        {
            var so = _sounds[_selected];
            EditorGUILayout.BeginVertical();

            EditorGUILayout.LabelField($"Assigned to  {so.name}:", EditorStyles.boldLabel);
            DrawAssigned(so);
            EditorGUILayout.Space();

            EditorGUILayout.LabelField("Search keywords (comma-separated filename matches):", EditorStyles.miniBoldLabel);
            EditorGUILayout.BeginHorizontal();
            var newSearch = EditorGUILayout.TextField(_search);
            if (newSearch != _search) { _search = newSearch; }
            if (GUILayout.Button("Search", GUILayout.Width(70))) RunSearch();
            if (GUILayout.Button("◼ Stop", GUILayout.Width(60))) AudioClipPreview.StopAll();
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.LabelField($"{_results.Count} candidates — ▶ hear, + add to {so.name}:", EditorStyles.miniLabel);
            _scrollResults = EditorGUILayout.BeginScrollView(_scrollResults);
            foreach (var clip in _results)
            {
                EditorGUILayout.BeginHorizontal();
                if (GUILayout.Button("▶", GUILayout.Width(26))) AudioClipPreview.Preview(clip);
                if (GUILayout.Button("+", GUILayout.Width(26))) { AddClip(so, clip); GUIUtility.ExitGUI(); }
                EditorGUILayout.LabelField(clip.name, GUILayout.MinWidth(140));
                GUILayout.FlexibleSpace();
                EditorGUILayout.LabelField(FolderOf(clip), EditorStyles.miniLabel, GUILayout.Width(240));
                EditorGUILayout.EndHorizontal();
            }
            EditorGUILayout.EndScrollView();

            EditorGUILayout.EndVertical();
        }

        void DrawAssigned(SoundSO so)
        {
            var s = new SerializedObject(so);
            var clips = s.FindProperty("clips");
            if (clips.arraySize == 0)
            {
                EditorGUILayout.LabelField("   (none — this sound is silent until you add a clip)", EditorStyles.miniLabel);
                return;
            }
            for (int i = 0; i < clips.arraySize; i++)
            {
                var clip = clips.GetArrayElementAtIndex(i).objectReferenceValue as AudioClip;
                EditorGUILayout.BeginHorizontal();
                if (GUILayout.Button("▶", GUILayout.Width(26))) AudioClipPreview.Preview(clip);
                if (GUILayout.Button("✕", GUILayout.Width(26))) { RemoveClip(so, i); GUIUtility.ExitGUI(); }
                EditorGUILayout.LabelField(clip != null ? clip.name : "(missing)");
                EditorGUILayout.EndHorizontal();
            }
        }

        // --- SoundSO clip editing ---

        static int ClipCount(SoundSO so) => new SerializedObject(so).FindProperty("clips").arraySize;

        static void AddClip(SoundSO so, AudioClip clip)
        {
            var s = new SerializedObject(so);
            var clips = s.FindProperty("clips");
            for (int i = 0; i < clips.arraySize; i++)
                if (clips.GetArrayElementAtIndex(i).objectReferenceValue == clip) return;
            clips.arraySize++;
            clips.GetArrayElementAtIndex(clips.arraySize - 1).objectReferenceValue = clip;
            s.ApplyModifiedProperties();
            EditorUtility.SetDirty(so);
            AssetDatabase.SaveAssets();
        }

        static void RemoveClip(SoundSO so, int index)
        {
            var s = new SerializedObject(so);
            var clips = s.FindProperty("clips");
            if (index < 0 || index >= clips.arraySize) return;
            clips.GetArrayElementAtIndex(index).objectReferenceValue = null; // clear ref first so DeleteArrayElementAtIndex removes the slot
            clips.DeleteArrayElementAtIndex(index);
            s.ApplyModifiedProperties();
            EditorUtility.SetDirty(so);
            AssetDatabase.SaveAssets();
        }

        // --- Harvest: copy External-Assets clips into Assets/Audio/UI + repoint ---

        void Harvest()
        {
            EnsureFolder(HarvestDir);
            int copied = 0;
            foreach (var so in _sounds)
            {
                var s = new SerializedObject(so);
                var clips = s.FindProperty("clips");
                bool changed = false;
                for (int i = 0; i < clips.arraySize; i++)
                {
                    var clip = clips.GetArrayElementAtIndex(i).objectReferenceValue as AudioClip;
                    if (clip == null) continue;
                    var src = AssetDatabase.GetAssetPath(clip);
                    if (string.IsNullOrEmpty(src) || !src.StartsWith(SearchRoot)) continue; // already tracked
                    var dest = AssetDatabase.GenerateUniqueAssetPath($"{HarvestDir}/{Path.GetFileName(src)}");
                    if (AssetDatabase.CopyAsset(src, dest))
                    {
                        var copy = AssetDatabase.LoadAssetAtPath<AudioClip>(dest);
                        clips.GetArrayElementAtIndex(i).objectReferenceValue = copy;
                        changed = true;
                        copied++;
                    }
                }
                if (changed) { s.ApplyModifiedProperties(); EditorUtility.SetDirty(so); }
            }
            AssetDatabase.SaveAssets();
            Debug.Log($"[SFX Auditioner] Harvested {copied} clip(s) into {HarvestDir} and repointed SoundSOs. These now commit cleanly.");
        }

        static void EnsureFolder(string path)
        {
            if (AssetDatabase.IsValidFolder(path)) return;
            var parent = Path.GetDirectoryName(path).Replace('\\', '/');
            var leaf = Path.GetFileName(path);
            EnsureFolder(parent);
            AssetDatabase.CreateFolder(parent, leaf);
        }

        static string FolderOf(AudioClip clip)
        {
            var dir = Path.GetDirectoryName(AssetDatabase.GetAssetPath(clip))?.Replace('\\', '/') ?? "";
            return Path.GetFileName(dir);
        }
    }
}
