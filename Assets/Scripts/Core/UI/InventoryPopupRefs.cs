using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace ProjectAstra.Core.UI
{
    // Ref-holder attached to the prefab root by InventoryPopupBuilder. InventoryMenuUI
    // reads these fields instead of walking the hierarchy by name, so renames in the
    // builder don't silently break the runtime wiring.
    public class InventoryPopupRefs : MonoBehaviour
    {
        [Serializable]
        public class RowRefs
        {
            public GameObject root;
            public Image background;
            public Image sigil;
            public TextMeshProUGUI nameText;
            public TextMeshProUGUI kindText;
            public TextMeshProUGUI usesText;
            public RectTransform durabilityTrack;
            public Image durabilityFill;
            public Image selectionCaret;

            public Sprite sprDefault;
            public Sprite sprSelected;
            public Sprite sprEmpty;
            public Sprite sprDepleted;
        }

        [Header("Header")]
        public TextMeshProUGUI headerLabel;

        [Header("Portrait panel")]
        public TextMeshProUGUI portraitPlaceholder;
        public TextMeshProUGUI unitName;
        public TextMeshProUGUI unitClass;
        public TextMeshProUGUI hpNumbers;
        public Image hpFill;

        [Header("Inventory panel")]
        public TextMeshProUGUI inventoryTitle;
        public TextMeshProUGUI inventoryCount;
        public TextMeshProUGUI hintsText;
        public RowRefs[] rows = new RowRefs[5];

        [Header("Stats panel")]
        public CanvasGroup statsGroup;
        public TextMeshProUGUI selectedItemName;
        public TextMeshProUGUI selectedItemKind;
        public TextMeshProUGUI statAtk;
        public TextMeshProUGUI statHit;
        public TextMeshProUGUI statRng;
        public TextMeshProUGUI statWt;
        public TextMeshProUGUI itemDescription;
        public TextMeshProUGUI provText;

        [Header("Sigils (by InventoryItem kind / weapon type)")]
        public Sprite sigilSword;
        public Sprite sigilLance;
        public Sprite sigilAxe;
        public Sprite sigilBow;
        public Sprite sigilStaff;
        public Sprite sigilConsumable;
    }
}
