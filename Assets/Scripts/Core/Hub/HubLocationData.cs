using System;
using UnityEngine;

namespace ProjectAstra.Core.Hub
{
    // A prop whose blocking area is smaller than its picture. Kept as a rect rather than baked into
    // the mask so an interactable that opens or is cleared away can un-stamp exactly what it added.
    [Serializable]
    public struct HubPropFootprint
    {
        [Tooltip("Matches the interactable's id when the prop can change state. Leave empty for permanent scenery.")]
        public string propId;

        [Tooltip("Blocking area in tiles, measured from the location's bottom-left. For a tall sprite this is the base it stands on, not the whole picture.")]
        public Rect footprint;

        [Tooltip("Off means the prop starts already cleared away.")]
        public bool startsSolid;
    }

    // One walkable space in the Hub: the courtyard, the Library, a student's house. Art is one
    // painted PNG; where you can walk is separate data painted over it in the location editor.
    //
    // Its own type rather than a battle MapData because the two want different things — MapData
    // caps at 64x64, routes passability through a per-movement-type cost table the hub is not
    // allowed to use, and has nowhere to put sub-tile footprints.
    [CreateAssetMenu(fileName = "HubLocationData", menuName = "Project Astra/Hub/Location Data")]
    public class HubLocationData : ScriptableObject
    {
        // Half a tile, matching HubCollisionMap.
        public const int CellsPerTile = 2;

        [SerializeField] private string locationId;
        [SerializeField] private string displayName;

        [Header("Art")]
        [SerializeField] private Sprite baseArt;

        [Tooltip("Animated dressing — trees, cloth, smoke. Instantiated under the location when it loads.")]
        [SerializeField] private GameObject propsPrefab;

        [Header("Size")]
        [SerializeField] private int tileWidth = 15;
        [SerializeField] private int tileHeight = 9;

        [Header("Walkability")]
        [Tooltip("Half-tile cells, row-major from the bottom-left. True blocks. Painted in the location editor.")]
        [SerializeField] private bool[] blockedCells = Array.Empty<bool>();

        [SerializeField] private HubPropFootprint[] propFootprints = Array.Empty<HubPropFootprint>();

        [Header("Ways in and out")]
        [SerializeField] private HubDoor[] doors = Array.Empty<HubDoor>();

        public string LocationId => locationId;
        public string DisplayName => displayName;
        public Sprite BaseArt => baseArt;
        public GameObject PropsPrefab => propsPrefab;
        public int TileWidth => tileWidth;
        public int TileHeight => tileHeight;
        public int CellsWide => tileWidth * CellsPerTile;
        public int CellsHigh => tileHeight * CellsPerTile;
        public bool[] BlockedCells => blockedCells;
        public HubPropFootprint[] PropFootprints => propFootprints;
        public HubDoor[] Doors => doors;

        public bool TryGetDoor(string doorId, out HubDoor found)
        {
            foreach (HubDoor door in doors)
            {
                if (door.doorId != doorId) continue;
                found = door;
                return true;
            }
            found = default;
            return false;
        }

        // The camera never shows past the location's own edges, so its bounds are simply the room.
        public Rect Bounds => new(0f, 0f, tileWidth, tileHeight);

        public HubCollisionMap BuildCollisionMap()
        {
            var map = new HubCollisionMap(CellsWide, CellsHigh, blockedCells);
            foreach (HubPropFootprint prop in propFootprints)
                if (prop.startsSolid) map.Stamp(prop.footprint);
            return map;
        }

        // Keeps the painted mask the right length when a designer resizes the room, preserving the
        // overlapping corner so a small nudge doesn't wipe the work.
        public void ResizeMask(int newTileWidth, int newTileHeight)
        {
            newTileWidth = Mathf.Max(1, newTileWidth);
            newTileHeight = Mathf.Max(1, newTileHeight);

            // Reads still use the old dimensions — tileWidth is only reassigned once copying is done.
            var resized = new bool[newTileWidth * CellsPerTile * newTileHeight * CellsPerTile];
            int copyWide = Mathf.Min(CellsWide, newTileWidth * CellsPerTile);
            int copyHigh = Mathf.Min(CellsHigh, newTileHeight * CellsPerTile);

            for (int y = 0; y < copyHigh; y++)
                for (int x = 0; x < copyWide; x++)
                {
                    int source = y * CellsWide + x;
                    if (blockedCells == null || source >= blockedCells.Length) continue;
                    resized[y * newTileWidth * CellsPerTile + x] = blockedCells[source];
                }

            tileWidth = newTileWidth;
            tileHeight = newTileHeight;
            blockedCells = resized;
        }

        private void OnValidate()
        {
            tileWidth = Mathf.Max(1, tileWidth);
            tileHeight = Mathf.Max(1, tileHeight);

            int expected = CellsWide * CellsHigh;
            if (blockedCells == null || blockedCells.Length != expected)
                Array.Resize(ref blockedCells, expected);
        }

        internal static HubLocationData CreateForTest(string locationId, int tileWidth, int tileHeight)
        {
            var location = CreateInstance<HubLocationData>();
            location.locationId = locationId;
            location.tileWidth = tileWidth;
            location.tileHeight = tileHeight;
            location.blockedCells = new bool[tileWidth * CellsPerTile * tileHeight * CellsPerTile];
            return location;
        }
    }
}
