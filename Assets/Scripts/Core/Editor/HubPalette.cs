using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;

namespace ProjectAstra.Core.Editor
{
    // The set of things a designer can place in a room, and how each behaves once placed.
    public sealed class HubPalette : ScriptableObject
    {
        public const string AssetPath = "Assets/Gurukul/Hub Palette.asset";

        // Ground is the floor she walks over; everything else stands on it and can hide her.
        public enum Kind { Ground, Object }

        [System.Serializable]
        public sealed class Entry
        {
            public string label;
            public string category = "Props";
            public Kind kind = Kind.Object;

            [Tooltip("Set for anything reused. The prefab decides its own colliders and sorting.")]
            public GameObject prefab;

            [Tooltip("Set for a one-off piece of art. Placing it builds the object.")]
            public Sprite sprite;

            [Tooltip("Whether she is stopped by it.")]
            public bool blocks;

            [Tooltip("The part of it she cannot walk through, in tiles, measured from its base.")]
            public Vector2 footprint = new(1f, 0.5f);

            public Object Source => prefab != null ? prefab : (Object)sprite;
            public bool IsUsable => Source != null;
        }

        [SerializeField] private List<Entry> entries = new();

        public IReadOnlyList<Entry> Entries => entries;

        public IEnumerable<string> Categories =>
            entries.Select(e => e.category).Where(c => !string.IsNullOrEmpty(c)).Distinct().OrderBy(c => c);

        public IEnumerable<Entry> InCategory(string category) =>
            entries.Where(e => e.IsUsable && (category == null || e.category == category));

        // Art dropped into the hub folder becomes placeable without anyone adding it by hand. The
        // folder it was put in becomes its category, so an artist's own filing is the palette's.
        public bool Adopt(Sprite sprite, string category, Kind kind)
        {
            if (sprite == null || entries.Any(e => e.sprite == sprite)) return false;

            entries.Add(new Entry
            {
                label = Readable(sprite.name),
                category = category,
                kind = kind,
                sprite = sprite,
                blocks = kind == Kind.Object
            });
            return true;
        }

        public bool Adopt(GameObject prefab, string category)
        {
            if (prefab == null || entries.Any(e => e.prefab == prefab)) return false;

            entries.Add(new Entry
            {
                label = Readable(prefab.name),
                category = category,
                kind = Kind.Object,
                prefab = prefab
            });
            return true;
        }

        // Art is filed as stone_well or stone-well; the palette shows it the way it would be said.
        private static string Readable(string fileName)
        {
            string spaced = fileName.Replace('_', ' ').Replace('-', ' ').Trim();
            return spaced.Length == 0 ? fileName : char.ToUpperInvariant(spaced[0]) + spaced.Substring(1);
        }

        public void Forget(Entry entry)
        {
            entries.Remove(entry);
            Save();
        }

        public void Save()
        {
            EditorUtility.SetDirty(this);
            AssetDatabase.SaveAssetIfDirty(this);
        }

        // There is one palette and it is made the first time anything asks for it, so a designer
        // never has to know it is an asset at all.
        public static HubPalette Load()
        {
            var existing = AssetDatabase.LoadAssetAtPath<HubPalette>(AssetPath);
            if (existing != null) return existing;

            var created = CreateInstance<HubPalette>();
            System.IO.Directory.CreateDirectory(System.IO.Path.GetDirectoryName(AssetPath));
            AssetDatabase.CreateAsset(created, AssetPath);
            AssetDatabase.SaveAssets();
            return created;
        }
    }
}
