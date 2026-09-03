using UnityEngine;

namespace ProjectAstra.Core.Hub
{
    // One walkable space in the hub. The place itself is authored in the Hub scene; this is the
    // record of it that visits, doors and the camera refer to.
    [CreateAssetMenu(fileName = "HubLocationData", menuName = "Project Astra/Hub/Location Data")]
    public class HubLocationData : ScriptableObject
    {
        [SerializeField] private string locationId;
        [SerializeField] private string displayName;

        [Header("Size")]
        [Tooltip("In tiles. The camera never shows past this, and she cannot walk out of it.")]
        [SerializeField] private int tileWidth = 15;
        [SerializeField] private int tileHeight = 9;

        public string LocationId => locationId;
        public string DisplayName => displayName;
        public int TileWidth => tileWidth;
        public int TileHeight => tileHeight;

        // The camera never shows past the location's own edges, so its bounds are simply the room.
        public Rect Bounds => new(0f, 0f, tileWidth, tileHeight);

        private void OnValidate()
        {
            tileWidth = Mathf.Max(1, tileWidth);
            tileHeight = Mathf.Max(1, tileHeight);
        }

        internal static HubLocationData CreateForTest(string locationId, int tileWidth, int tileHeight)
        {
            var location = CreateInstance<HubLocationData>();
            location.locationId = locationId;
            location.tileWidth = tileWidth;
            location.tileHeight = tileHeight;
            return location;
        }
    }
}
