using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using ProjectAstra.Core.Audio;

namespace ProjectAstra.Core.Editor
{
    // Creates the empty SoundSO assets for the cursor events and registers them in the
    // AudioLibrary, so a designer only has to drop a clip into a slot rather than build the
    // enum-to-asset chain by hand.
    //
    // Every asset ships clipless on purpose: silence is the correct default until the sounds
    // are chosen, and a clipless SoundSO plays nothing without complaining.
    public static class CursorSoundSeeder
    {
        private const string Folder = "Assets/ScriptableObjects/Audio";

        private static readonly SoundId[] CursorSounds =
        {
            SoundId.CursorStepped,
            SoundId.CursorHoverSelectable,
            SoundId.CursorHoverEnemy,
            SoundId.CursorUnitSelected,
            SoundId.CursorMoveConfirmed,
            SoundId.CursorMoveCancelled,
            SoundId.CursorSelectionCancelled,
            SoundId.CursorUnitSpentTurn,
            SoundId.CursorError,
        };

        [MenuItem("Project Astra/Audio/Seed Cursor Sound Slots")]
        public static void Seed()
        {
            var library = AssetDatabase.LoadAssetAtPath<AudioLibrary>($"{Folder}/AudioLibrary.asset");
            if (library == null)
            {
                Debug.LogError("[CursorSoundSeeder] AudioLibrary.asset not found — nothing to register into.");
                return;
            }

            var created = new Dictionary<SoundId, SoundSO>();
            foreach (var id in CursorSounds)
            {
                var sound = GetOrCreateSound(id);
                if (sound != null) created[id] = sound;
            }

            AppendMissingLibraryRows(library, created);
            PointProfilesAtSlots();

            AssetDatabase.SaveAssets();
            Debug.Log($"[CursorSoundSeeder] {created.Count} cursor sound slots ready in {Folder}. " +
                      "Drop a clip into any of them to hear it — no profile edit needed.");
        }

        // Wires each profile's event slots to the matching id, but only where the designer has
        // left it at None. The assets are clipless, so this changes nothing audible — it just
        // means dropping a clip into CursorStepped.asset is the only step required.
        private static void PointProfilesAtSlots()
        {
            foreach (string guid in AssetDatabase.FindAssets("t:CursorVariantProfile"))
            {
                var profile = AssetDatabase.LoadAssetAtPath<ProjectAstra.Core.Cursor.CursorVariantProfile>(
                    AssetDatabase.GUIDToAssetPath(guid));
                if (profile == null) continue;

                var so = new SerializedObject(profile);
                AssignIfUnset(so, "steppedSound", SoundId.CursorStepped);
                AssignIfUnset(so, "hoverSelectableSound", SoundId.CursorHoverSelectable);
                AssignIfUnset(so, "hoverEnemySound", SoundId.CursorHoverEnemy);
                AssignIfUnset(so, "unitSelectedSound", SoundId.CursorUnitSelected);
                AssignIfUnset(so, "moveConfirmedSound", SoundId.CursorMoveConfirmed);
                AssignIfUnset(so, "moveCancelledSound", SoundId.CursorMoveCancelled);
                AssignIfUnset(so, "selectionCancelledSound", SoundId.CursorSelectionCancelled);
                AssignIfUnset(so, "unitSpentTurnSound", SoundId.CursorUnitSpentTurn);
                AssignIfUnset(so, "errorSound", SoundId.CursorError);
                so.ApplyModifiedPropertiesWithoutUndo();

                EditorUtility.SetDirty(profile);
            }
        }

        private static void AssignIfUnset(SerializedObject so, string field, SoundId id)
        {
            var property = so.FindProperty(field);
            if (property == null || property.enumValueIndex != (int)SoundId.None) return;
            property.enumValueIndex = (int)id;
        }

        private static SoundSO GetOrCreateSound(SoundId id)
        {
            string path = $"{Folder}/{id}.asset";
            var existing = AssetDatabase.LoadAssetAtPath<SoundSO>(path);
            if (existing != null) return existing;

            var sound = ScriptableObject.CreateInstance<SoundSO>();
            AssetDatabase.CreateAsset(sound, path);
            return sound;
        }

        // Strictly additive — the library is a shared asset, and rewriting it from a code seed
        // would silently drop every row a designer added by hand.
        private static void AppendMissingLibraryRows(AudioLibrary library, Dictionary<SoundId, SoundSO> sounds)
        {
            var so = new SerializedObject(library);
            var nodes = so.FindProperty("soundNodes");

            var alreadyPresent = new HashSet<int>();
            for (int i = 0; i < nodes.arraySize; i++)
                alreadyPresent.Add(nodes.GetArrayElementAtIndex(i).FindPropertyRelative("id").enumValueIndex);

            foreach (var pair in sounds)
            {
                if (alreadyPresent.Contains((int)pair.Key)) continue;

                nodes.InsertArrayElementAtIndex(nodes.arraySize);
                var node = nodes.GetArrayElementAtIndex(nodes.arraySize - 1);
                node.FindPropertyRelative("id").enumValueIndex = (int)pair.Key;
                node.FindPropertyRelative("sound").objectReferenceValue = pair.Value;
            }

            so.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(library);
        }
    }
}
