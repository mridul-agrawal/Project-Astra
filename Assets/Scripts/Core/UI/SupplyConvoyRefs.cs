using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace ProjectAstra.Core.UI
{
    // Ref-holder attached to the SupplyConvoy prefab root by SupplyConvoyBuilder.
    // ConvoyUI reads these fields to drive live text + state sprite swaps.
    public class SupplyConvoyRefs : MonoBehaviour
    {
        [Serializable]
        public class RowRefs
        {
            public GameObject root;
            public Image background;
            public Image sigil;
            public TextMeshProUGUI nameText;
            public TextMeshProUGUI subText;
            public TextMeshProUGUI rankText;
            public Image rankChipBg;
            public TextMeshProUGUI usesText;
            public Image durabilityTrack;
            public Image durabilityFill;

            public Sprite sprDefault;
            public Sprite sprFocused;
            public Sprite sprDepleted;
            public Sprite sprDisabled;
        }

        [Serializable]
        public class SlotRefs
        {
            public GameObject root;
            public Image background;
            public Image sigil;
            public TextMeshProUGUI nameText;
            public TextMeshProUGUI rankText;
            public TextMeshProUGUI usesText;

            public Sprite sprDefault;
            public Sprite sprFocused;
            public Sprite sprEquipped;
            public Sprite sprDepleted;
            public Sprite sprEmpty;
        }

        [Serializable]
        public class TabRefs
        {
            public GameObject root;
            public Image background;
            public TextMeshProUGUI label;     // may hold single letter or count
            public TextMeshProUGUI countText;

            public Sprite sprDefault;
            public Sprite sprHover;
            public Sprite sprFocused;
            public Sprite sprActive;
        }

        [Header("Header")]
        public TextMeshProUGUI cartoucheTitle;
        public TextMeshProUGUI cartoucheSub;

        [Header("Portrait")]
        public TextMeshProUGUI portraitName;
        public TextMeshProUGUI portraitLabel;
        public Image portraitSilhouette; // optional

        [Header("Dialogue bubble")]
        public TextMeshProUGUI bubbleSpeaker;
        public TextMeshProUGUI bubbleLine;

        [Header("Submenu")]
        public Image giveButtonBg;
        public TextMeshProUGUI giveLabel;
        public Image takeButtonBg;
        public TextMeshProUGUI takeLabel;
        public Sprite submenuDefault;
        public Sprite submenuActive;
        public Sprite submenuHover;
        public Sprite submenuPressed;

        [Header("Lord inventory")]
        public TextMeshProUGUI lordInvHeader;
        public TextMeshProUGUI lordInvCap;
        public SlotRefs[] slots = new SlotRefs[5];

        [Header("Convoy head")]
        public TextMeshProUGUI convoyTitle;
        public TextMeshProUGUI stockNum;

        [Header("Tabstrip")]
        public TabRefs[] tabs = new TabRefs[10];

        [Header("Convoy list")]
        public RectTransform rowsContainer;
        public RowRefs[] rows = new RowRefs[10];
        public RectTransform scrollThumb;

        [Header("Footer")]
        public TextMeshProUGUI hintsText;

        [Header("Sigils (shared)")]
        public Sprite sigilSword;
        public Sprite sigilLance;
        public Sprite sigilAxe;
        public Sprite sigilBow;
        public Sprite sigilStaff;
        public Sprite sigilConsumable;
    }
}
