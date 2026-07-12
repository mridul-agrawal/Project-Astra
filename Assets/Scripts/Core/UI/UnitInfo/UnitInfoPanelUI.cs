using TMPro;
using UnityEngine;
using UnityEngine.UI;
using ProjectAstra.Core.Audio;
using ProjectAstra.Core.Combat;
using ProjectAstra.Core.Input;
using ProjectAstra.Core.Stats;
using ProjectAstra.Core.Support;
using ProjectAstra.Core.UI.Interfaces;
using ProjectAstra.Core.UI.Inventory;
using ProjectAstra.Core.Units;

namespace ProjectAstra.Core.UI.UnitInfo
{
    // UI-02 Unit Info Panel. Three-page modal: Stats / Inventory / Supports.
    // Pages 0–1 always visible; page 2 only for Player-faction units. Cursor
    // left/right flips pages; up/down moves row selection within
    // Inventory/Supports. Confirm opens a detail sub-panel (item or bond).
    // Cancel closes the whole modal.
    public class UnitInfoPanelUI : MonoBehaviour
    {
        public static bool HasInputFocus { get; private set; }

        static readonly Color HpGreen  = new Color32(0x60, 0xc8, 0x70, 0xff);
        static readonly Color HpYellow = new Color32(0xc8, 0xb0, 0x40, 0xff);
        static readonly Color HpRed    = new Color32(0xc8, 0x40, 0x40, 0xff);
        static readonly Color ColGreen = new Color32(0x60, 0xc8, 0x70, 0xff);
        static readonly Color ColRed   = new Color32(0xc8, 0x40, 0x40, 0xff);
        static readonly Color ColIvory = new Color32(0xe8, 0xe0, 0xd0, 0xff);
        static readonly Color ColGold  = new Color32(0xc8, 0xa0, 0x40, 0xff);
        static readonly Color ColSilver = new Color32(0xa0, 0xa8, 0xb8, 0xff);

        [SerializeField] Sprite hpFillGreen;
        [SerializeField] Sprite hpFillYellow;
        [SerializeField] Sprite hpFillRed;
        [SerializeField] Sprite pageDotActive;
        [SerializeField] Sprite pageDotInactive;

        [Header("Bond pip / support row chrome")]
        [SerializeField] Sprite bondPipLit;
        [SerializeField] Sprite bondPipUnlit;
        [SerializeField] Sprite bondPipEncounter;
        [SerializeField] Sprite notifBadge;
        [SerializeField] Sprite diyaMemorial;
        [SerializeField] Sprite shapathIcon;
        [SerializeField] Sprite thresholdMark;

        [Header("Affinity icons (index matches PanchaBhuta enum, 0=None unused)")]
        [SerializeField] Sprite[] affinityIcons;

        [Header("Material for deceased portrait")]
        [SerializeField] Material desaturatedMaterial;

        [Header("Detail sub-panels")]
        [SerializeField] UnitInfoItemDetailUI itemDetail;
        [SerializeField] UnitInfoSupportDetailUI supportDetail;

        TestUnit unit;
        UnitInfoContext ctx = UnitInfoContext.BattleMap;
        int currentPage;
        int maxPage;
        GameObject[] pages;
        Image[] dots;
        GameObject dimOverlay;

        // Shared section refs
        TextMeshProUGUI unitNameText;
        TextMeshProUGUI classNameText;
        TextMeshProUGUI lvValueText;
        TextMeshProUGUI expLabelText;
        TextMeshProUGUI expValueText;
        TextMeshProUGUI hpCurrentText;
        TextMeshProUGUI hpSepText;
        TextMeshProUGUI hpMaxText;
        RectTransform hpBarFill;
        Image hpBarFillImage;
        RectTransform hpBarOuter;
        Image portraitImage;
        Image stressOverlayImage;
        Image affinityIconImage;
        TextMeshProUGUI affinityLabelText;
        TextMeshProUGUI personalityLabelText;

        // Stats page extra refs
        TextMeshProUGUI movValueText;
        GameObject dharmicThresholdBanner;

        // Supports page
        RectTransform supportsList;
        GameObject supportRowTemplate;
        TextMeshProUGUI supportsEmptyText;

        // Intra-page row selection
        int selectedItemRow;
        int selectedBondRow;
        readonly System.Collections.Generic.List<TextMeshProUGUI> visibleItemNameTMPs = new();
        readonly System.Collections.Generic.List<int> visibleItemSlotIndices = new();
        readonly System.Collections.Generic.List<TextMeshProUGUI> visibleBondNameTMPs = new();
        readonly System.Collections.Generic.List<SupportBond> visibleBondData = new();

        // Providers (scene-level, optional)
        ISupportProvider supportProvider;
        ITemporaryModifierProvider modifierProvider;
        ISupportBonusProvider supportBonusProvider;
        IFogOfWarProvider fogProvider;

        Material portraitOriginalMaterial;
        float hpBarReferenceWidth;

        bool refsDiscovered;
        bool providersDiscovered;

        public void Show(TestUnit unit) => Show(unit, UnitInfoContext.BattleMap);

        public void Show(TestUnit unit, UnitInfoContext ctx)
        {
            if (unit == null) return;

            if (!providersDiscovered) DiscoverProviders();

            // HM-08 / MT-04. Unrevealed enemies in fog of war can't be inspected.
            if (fogProvider != null && unit.UnitInstance != null && !fogProvider.IsRevealed(unit.UnitInstance))
                return;

            this.unit = unit;
            this.ctx = ctx;
            currentPage = 0;
            maxPage = unit.faction == Faction.Player ? 2 : 1;

            if (!refsDiscovered) DiscoverReferences();

            BindSharedSection();
            SetActivePage(0);
            UpdatePageIndicator();

            if (dimOverlay != null) dimOverlay.SetActive(true);
            gameObject.SetActive(true);

            HasInputFocus = true;
            SubscribeInput();
            AudioManager.Instance?.Play(SoundId.UiPanelOpen);
        }

        void DiscoverProviders()
        {
            providersDiscovered = true;
            // Scene-level optional providers; any missing → safe defaults.
            supportProvider      = FindInterface<ISupportProvider>();
            modifierProvider     = FindInterface<ITemporaryModifierProvider>();
            supportBonusProvider = FindInterface<ISupportBonusProvider>();
            fogProvider          = FindInterface<IFogOfWarProvider>();
        }

        static T FindInterface<T>() where T : class
        {
            foreach (var mb in Object.FindObjectsByType<MonoBehaviour>(FindObjectsInactive.Include, FindObjectsSortMode.None))
                if (mb is T t) return t;
            return null;
        }

        public void Hide()
        {
            bool wasOpen = HasInputFocus;
            HasInputFocus = false;
            UnsubscribeInput();

            gameObject.SetActive(false);
            if (dimOverlay != null) dimOverlay.SetActive(false);

            unit = null;
            if (wasOpen) AudioManager.Instance?.Play(SoundId.UiPanelClose);
        }

        void SubscribeInput()
        {
            if (InputManager.Instance == null) return;
            InputManager.Instance.OnCursorMove += NavigatePage;
            InputManager.Instance.OnCancel += HandleCancel;
            InputManager.Instance.OnConfirm += HandleConfirm;
            InputManager.Instance.OnNextUnit += NextPage;
            InputManager.Instance.OnPrevUnit += PrevPage;
        }

        void UnsubscribeInput()
        {
            if (InputManager.Instance == null) return;
            InputManager.Instance.OnCursorMove -= NavigatePage;
            InputManager.Instance.OnCancel -= HandleCancel;
            InputManager.Instance.OnConfirm -= HandleConfirm;
            InputManager.Instance.OnNextUnit -= NextPage;
            InputManager.Instance.OnPrevUnit -= PrevPage;
        }

        void HandleConfirm()
        {
            // Only act when the main panel has focus (i.e. no detail sub-panel is open).
            if (UnitInfoItemDetailUI.HasInputFocus || UnitInfoSupportDetailUI.HasInputFocus) return;
            if (currentPage == 1) OpenSelectedItemDetail();
            else if (currentPage == 2) OpenSelectedBondDetail();
        }

        void OpenSelectedItemDetail()
        {
            if (itemDetail == null || unit == null) return;
            if (visibleItemSlotIndices.Count == 0) return;
            int row = Mathf.Clamp(selectedItemRow, 0, visibleItemSlotIndices.Count - 1);
            int slotIdx = visibleItemSlotIndices[row];
            var slot = unit.Inventory?.GetSlot(slotIdx) ?? InventoryItem.None;
            if (slot.kind == ItemKind.None) return;
            UnsubscribeInput();
            AudioManager.Instance?.Play(SoundId.UiConfirm);
            itemDetail.Show(slot, () => { SubscribeInput(); });
        }

        void OpenSelectedBondDetail()
        {
            if (supportDetail == null || unit == null || unit.UnitInstance == null) return;
            if (visibleBondData.Count == 0) return;
            int row = Mathf.Clamp(selectedBondRow, 0, visibleBondData.Count - 1);
            UnsubscribeInput();
            AudioManager.Instance?.Play(SoundId.UiConfirm);
            supportDetail.Show(unit.UnitInstance, visibleBondData[row], supportBonusProvider, () => { SubscribeInput(); });
        }

        void NavigatePage(Vector2Int dir)
        {
            // Horizontal → flip pages. Vertical → move row selection on pages 1/2.
            if (dir.x > 0) NextPage();
            else if (dir.x < 0) PrevPage();
            else if (dir.y != 0)
            {
                int delta = dir.y < 0 ? 1 : -1; // Unity's +y is up-on-screen; rows flow downward.
                if (currentPage == 1) MoveItemSelection(delta);
                else if (currentPage == 2) MoveBondSelection(delta);
            }
        }

        void MoveItemSelection(int delta)
        {
            if (visibleItemNameTMPs.Count == 0) return;
            int n = visibleItemNameTMPs.Count;
            selectedItemRow = (selectedItemRow + delta + n) % n;
            UpdateItemHighlight();
            AudioManager.Instance?.Play(SoundId.UiMove);
        }

        void MoveBondSelection(int delta)
        {
            if (visibleBondNameTMPs.Count == 0) return;
            int n = visibleBondNameTMPs.Count;
            selectedBondRow = (selectedBondRow + delta + n) % n;
            UpdateBondHighlight();
            AudioManager.Instance?.Play(SoundId.UiMove);
        }

        void UpdateItemHighlight()
        {
            for (int i = 0; i < visibleItemNameTMPs.Count; i++)
            {
                var t = visibleItemNameTMPs[i];
                if (t == null) continue;
                t.color = i == selectedItemRow ? ColGold : ColIvory;
            }
        }

        void UpdateBondHighlight()
        {
            for (int i = 0; i < visibleBondNameTMPs.Count; i++)
            {
                var t = visibleBondNameTMPs[i];
                if (t == null) continue;
                if (i == visibleBondData.Count) break;
                // Deceased rows stay silver/struck-through regardless of selection.
                if (visibleBondData[i].IsDeceased)
                {
                    t.color = new Color(ColSilver.r, ColSilver.g, ColSilver.b, 0.4f);
                }
                else
                {
                    t.color = i == selectedBondRow ? ColGold : ColIvory;
                }
            }
        }

        void NextPage()
        {
            int next = currentPage >= maxPage ? 0 : currentPage + 1;
            SetActivePage(next);
            AudioManager.Instance?.Play(SoundId.UiTab);
        }

        void PrevPage()
        {
            int prev = currentPage <= 0 ? maxPage : currentPage - 1;
            SetActivePage(prev);
            AudioManager.Instance?.Play(SoundId.UiTab);
        }

        void HandleCancel() => Hide();

        void SetActivePage(int page)
        {
            if (pages == null) return;

            for (int i = 0; i < pages.Length; i++)
                if (pages[i] != null)
                    pages[i].SetActive(i == page);

            // Reset row selection when entering a selection-aware page.
            if (page == 1) selectedItemRow = 0;
            else if (page == 2) selectedBondRow = 0;

            currentPage = page;
            UpdatePageIndicator();
            BindCurrentPage();
        }

        void UpdatePageIndicator()
        {
            if (dots == null) return;
            for (int i = 0; i < dots.Length; i++)
            {
                if (dots[i] == null) continue;
                bool isActive = i == currentPage;
                if (pageDotActive != null && pageDotInactive != null)
                {
                    dots[i].sprite = isActive ? pageDotActive : pageDotInactive;
                    dots[i].color = Color.white;
                }
                else
                {
                    dots[i].color = isActive
                        ? ColGold
                        : new Color(ColGold.r, ColGold.g, ColGold.b, 0.3f);
                }
            }
        }

        void BindCurrentPage()
        {
            switch (currentPage)
            {
                case 0: BindStatsPage(); break;
                case 1: BindInventoryPage(); break;
                case 2: BindSupportsPage(); break;
            }
        }

        // ================================================================
        // Reference discovery
        // ================================================================

        void DiscoverReferences()
        {
            refsDiscovered = true;

            // Dim overlay is a sibling
            var canvasT = transform.parent;
            if (canvasT != null)
            {
                var overlayT = canvasT.Find("UnitInfoDimOverlay");
                if (overlayT != null) dimOverlay = overlayT.gameObject;
            }

            // Pages
            var container = FindDeep(transform, "PageContainer");
            if (container != null)
            {
                var stats = container.Find("StatsPage");
                var inv = container.Find("InventoryPage");
                var sup = container.Find("SupportsPage");
                pages = new[]
                {
                    stats != null ? stats.gameObject : null,
                    inv != null ? inv.gameObject : null,
                    sup != null ? sup.gameObject : null
                };
            }

            // Page indicator dots
            var dotsParent = FindDeep(transform, "PageIndicator");
            if (dotsParent != null)
            {
                dots = new Image[3];
                for (int i = 0; i < 3 && i < dotsParent.childCount; i++)
                    dots[i] = dotsParent.GetChild(i).GetComponent<Image>();
            }

            // Shared section
            unitNameText = FindTMP(transform, "UnitName");
            classNameText = FindTMP(transform, "ClassName");
            lvValueText = FindTMP(transform, "LvValue");
            expLabelText = FindTMP(transform, "ExpLabel");
            expValueText = FindTMP(transform, "ExpValue");
            hpCurrentText = FindTMP(transform, "HpCurrent");
            hpSepText = FindTMP(transform, "HpSep");
            hpMaxText = FindTMP(transform, "HpMax");

            var hpFillT = FindDeep(transform, "HpBarFill");
            if (hpFillT != null)
            {
                hpBarFill = hpFillT.GetComponent<RectTransform>();
                hpBarFillImage = hpFillT.GetComponent<Image>();
            }
            var hpOuterT = FindDeep(transform, "HpBarOuter");
            if (hpOuterT != null)
            {
                hpBarOuter = hpOuterT.GetComponent<RectTransform>();
                hpBarReferenceWidth = hpBarOuter.rect.width;
            }

            var portraitT = FindDeep(transform, "PortraitPlaceholder");
            if (portraitT != null)
            {
                portraitImage = portraitT.GetComponent<Image>();
                if (portraitImage != null) portraitOriginalMaterial = portraitImage.material;
            }

            // New shared-section nodes
            var stressT = FindDeep(transform, "StressOverlay");
            if (stressT != null) stressOverlayImage = stressT.GetComponent<Image>();

            var affT = FindDeep(transform, "AffinIcon");
            if (affT != null) affinityIconImage = affT.GetComponent<Image>();
            affinityLabelText = FindTMP(transform, "AffinLabel");
            personalityLabelText = FindTMP(transform, "PersonalityLabel");

            // Stats page extras
            movValueText = FindTMP(transform, "StatR_Mov/Value");
            var dtbT = FindDeep(transform, "DharmicThresholdBanner");
            if (dtbT != null) dharmicThresholdBanner = dtbT.gameObject;

            // Supports list
            var supListT = FindDeep(transform, "SupportList");
            if (supListT != null) supportsList = supListT.GetComponent<RectTransform>();
            var supTemplateT = FindDeep(transform, "SupportRowTemplate");
            if (supTemplateT != null)
            {
                supportRowTemplate = supTemplateT.gameObject;
                supportRowTemplate.SetActive(false);
            }
            supportsEmptyText = FindTMP(transform, "SupportsEmptyText");
        }

        // ================================================================
        // Shared section binding
        // ================================================================

        void BindSharedSection()
        {
            var inst = unit.UnitInstance;
            var def = inst?.Definition;

            // Name
            if (unitNameText != null)
            {
                string name = def != null && !string.IsNullOrEmpty(def.UnitName)
                    ? def.UnitName : unit.gameObject.name;
                unitNameText.text = name.ToUpper();
            }

            // Class
            if (classNameText != null)
            {
                var cls = inst?.CurrentClass;
                classNameText.text = cls != null ? cls.ClassName : "Unknown";
            }

            // Level
            if (lvValueText != null)
            {
                int level = inst?.Level ?? 1;
                lvValueText.text = level.ToString();
            }

            // EXP — "MAX" for promoted cap; "--" for unpromoted Lv 20 (still accumulates silently); hidden for enemy.
            bool showExp = unit.faction != Faction.Enemy;
            bool atCap = inst != null && inst.IsAtLevelCap;
            bool atUnpromotedCap = inst != null && inst.CurrentClass != null
                && !inst.CurrentClass.IsPromoted && inst.Level >= UnitInstance.PromotedLevelCap;
            if (expValueText != null)
            {
                expValueText.gameObject.SetActive(showExp);
                if (atCap) expValueText.text = "MAX";
                else if (atUnpromotedCap) expValueText.text = "--";
                else expValueText.text = (inst?.CurrentEXP ?? 0) + " / " + UnitInstance.ExpPerLevel;
            }
            if (expLabelText != null) expLabelText.gameObject.SetActive(showExp);

            // HP
            int currentHP = inst != null ? inst.CurrentHP : unit.currentHP;
            int maxHP = inst != null ? inst.MaxHP : unit.maxHP;

            if (hpCurrentText != null) hpCurrentText.text = currentHP.ToString();
            if (hpMaxText != null) hpMaxText.text = maxHP.ToString();

            if (hpBarFillImage != null)
            {
                float frac = maxHP > 0 ? Mathf.Clamp01((float)currentHP / maxHP) : 0;
                var sprite = HpSpriteForFraction(frac);
                if (sprite != null)
                {
                    hpBarFillImage.sprite = sprite;
                    hpBarFillImage.color = Color.white;
                }
                else
                {
                    hpBarFillImage.color = HpColorForFraction(frac);
                }

                if (hpBarFillImage.type == Image.Type.Filled)
                {
                    hpBarFillImage.fillAmount = frac;
                }
                else if (hpBarFill != null)
                {
                    hpBarFill.anchorMax = new Vector2(frac, 1);
                }
            }

            // HP bar width scaling — proportional to MaxHP vs. a 60-HP reference, clamped to 1.5×.
            if (hpBarOuter != null && hpBarReferenceWidth > 0f)
            {
                float scale = Mathf.Clamp(maxHP / 60f, 0.35f, 1.5f);
                var size = hpBarOuter.sizeDelta;
                size.x = hpBarReferenceWidth * scale - hpBarReferenceWidth; // delta over the baseline
                hpBarOuter.sizeDelta = size;
            }

            // Portrait variant pick — dead > critical > wounded > full.
            if (portraitImage != null && def != null)
            {
                bool isDead = (inst != null && inst.IsDead) || ctx == UnitInfoContext.ObituaryFromSupportList;
                Sprite portrait = PickPortrait(def, inst?.HPThreshold ?? HPThreshold.Normal, isDead);
                portraitImage.sprite = portrait;

                // Desaturation (dead state)
                if (isDead && desaturatedMaterial != null)
                    portraitImage.material = desaturatedMaterial;
                else
                    portraitImage.material = portraitOriginalMaterial;
            }

            // Stress overlay — USE-04 Tier 1+ layers an extra sprite atop the portrait.
            if (stressOverlayImage != null)
            {
                bool showStress = inst != null && inst.StressTier >= 1 && def != null && def.StressedOverlay != null;
                stressOverlayImage.gameObject.SetActive(showStress);
                if (showStress) stressOverlayImage.sprite = def.StressedOverlay;
            }

            // Affinity icon — SP-03.
            if (affinityIconImage != null)
            {
                Sprite icon = GetAffinityIcon(def?.Affinity ?? PanchaBhuta.None);
                affinityIconImage.gameObject.SetActive(icon != null);
                if (icon != null) affinityIconImage.sprite = icon;
            }
            if (affinityLabelText != null && def != null)
            {
                affinityLabelText.text = AffinityLabel(def.Affinity);
            }

            // Personality label — Allied NPC only (CC-01).
            if (personalityLabelText != null)
            {
                bool showPersonality = unit.faction == Faction.Allied && def != null && def.Personality != Personality.None;
                personalityLabelText.gameObject.SetActive(showPersonality);
                if (showPersonality) personalityLabelText.text = "PERSONALITY: " + def.Personality.ToString().ToUpper();
            }
        }

        static Sprite PickPortrait(UnitDefinition def, HPThreshold hpT, bool isDead)
        {
            if (isDead && def.DeceasedPortrait != null) return def.DeceasedPortrait;
            if (hpT == HPThreshold.Critical && def.CriticalPortrait != null) return def.CriticalPortrait;
            if (hpT == HPThreshold.Injured && def.WoundedPortrait != null) return def.WoundedPortrait;
            return def.Portrait;
        }

        Sprite GetAffinityIcon(PanchaBhuta a)
        {
            if (a == PanchaBhuta.None || affinityIcons == null) return null;
            int idx = (int)a - 1; // skip None=0
            return (idx >= 0 && idx < affinityIcons.Length) ? affinityIcons[idx] : null;
        }

        static string AffinityLabel(PanchaBhuta a) => a switch
        {
            PanchaBhuta.Agni => "Agni",
            PanchaBhuta.Jal => "Jal",
            PanchaBhuta.Vayu => "Vayu",
            PanchaBhuta.Prithvi => "Prithvi",
            PanchaBhuta.Akasha => "Akasha",
            _ => ""
        };

        // ================================================================
        // Stats page binding
        // ================================================================

        void BindStatsPage()
        {
            if (pages == null || pages[0] == null) return;
            var page = pages[0].transform;
            var inst = unit.UnitInstance;
            if (inst == null) return;

            var modifiers = modifierProvider != null ? modifierProvider.GetModifiers(inst) : default;
            var caps = inst.CurrentClass != null ? inst.CurrentClass.StatCaps : default;
            bool hasClass = inst.CurrentClass != null;

            // Core stats (left column: Str, Mag, Skl, Spd, Niyati)
            string[] leftNames = { "Str", "Mag", "Skl", "Spd", "Niyati" };
            StatIndex[] leftIdx = { StatIndex.Str, StatIndex.Mag, StatIndex.Skl, StatIndex.Spd, StatIndex.Niyati };
            for (int i = 0; i < leftNames.Length; i++)
                BindStatRowWithModifier(page, "StatL_" + leftNames[i], inst.Stats[leftIdx[i]], modifiers[leftIdx[i]],
                    hasClass && inst.Stats[leftIdx[i]] >= caps[leftIdx[i]]);

            // Right column: Def, Res, Con, Mov
            BindStatRowWithModifier(page, "StatR_Def", inst.Stats[StatIndex.Def], modifiers[StatIndex.Def],
                hasClass && inst.Stats[StatIndex.Def] >= caps[StatIndex.Def]);
            BindStatRowWithModifier(page, "StatR_Res", inst.Stats[StatIndex.Res], modifiers[StatIndex.Res],
                hasClass && inst.Stats[StatIndex.Res] >= caps[StatIndex.Res]);
            BindStatRowWithModifier(page, "StatR_Con", inst.Stats[StatIndex.Con], modifiers[StatIndex.Con],
                hasClass && inst.Stats[StatIndex.Con] >= caps[StatIndex.Con]);
            // Mov is derived from class — no temp modifier hook, no cap display.
            BindStatRowWithModifier(page, "StatR_Mov", inst.EffectiveMovement, 0, false);

            // Dharmic Threshold banner — shown only for player when every non-HP stat is capped.
            bool allCapped = hasClass && unit.faction == Faction.Player
                && inst.Stats[StatIndex.Str] >= caps[StatIndex.Str]
                && inst.Stats[StatIndex.Mag] >= caps[StatIndex.Mag]
                && inst.Stats[StatIndex.Skl] >= caps[StatIndex.Skl]
                && inst.Stats[StatIndex.Spd] >= caps[StatIndex.Spd]
                && inst.Stats[StatIndex.Niyati] >= caps[StatIndex.Niyati]
                && inst.Stats[StatIndex.Def] >= caps[StatIndex.Def]
                && inst.Stats[StatIndex.Res] >= caps[StatIndex.Res]
                && inst.Stats[StatIndex.Con] >= caps[StatIndex.Con];
            if (dharmicThresholdBanner != null) dharmicThresholdBanner.SetActive(allCapped);

            // Derived combat stats
            var wpn = unit.equippedWeapon;
            bool hasWeapon = !wpn.IsEmpty && !wpn.IsBroken;

            SetDerived(page, "Atk", hasWeapon
                ? ComputeAtk(inst, wpn).ToString() : "\u2014");
            SetDerived(page, "Hit", hasWeapon
                ? ComputeHit(inst, wpn).ToString() : "\u2014");
            SetDerived(page, "Crit", hasWeapon
                ? ComputeCrit(inst, wpn).ToString() : "\u2014");

            int attackSpeed = ComputeAS(inst, wpn, hasWeapon);
            SetDerived(page, "AS", attackSpeed.ToString());
            SetDerived(page, "Avo", ComputeAvo(inst, attackSpeed).ToString());

            // Weapon ranks (hide the WEXP detail for enemies)
            BindWeaponRanks(page);
        }

        void BindStatRowWithModifier(Transform page, string rowPrefix, int baseValue, int modifier, bool atCap)
        {
            var valT = FindTMP(page, rowPrefix + "/Value");
            if (valT == null) return;

            int final = baseValue + modifier;
            valT.text = final.ToString();
            if (modifier > 0) valT.color = ColGreen;
            else if (modifier < 0) valT.color = ColRed;
            else valT.color = ColIvory;

            var modT = FindTMP(page, rowPrefix + "/Mod");
            if (modT != null)
            {
                if (modifier != 0)
                {
                    string arrow = modifier > 0 ? "\u2191+" : "\u2193";
                    modT.text = arrow + Mathf.Abs(modifier);
                    modT.color = modifier > 0 ? ColGreen : ColRed;
                    modT.gameObject.SetActive(true);
                }
                else modT.gameObject.SetActive(false);
            }

            // Dharmic threshold per-stat marker (SS-12)
            var rowT = FindDeep(page, rowPrefix);
            if (rowT != null)
            {
                var markT = rowT.Find("ThresholdMark");
                if (markT != null) markT.gameObject.SetActive(atCap);
            }
        }

        void BindWeaponRanks(Transform page)
        {
            var tracker = unit.WeaponRankTracker;
            var cls = unit.UnitInstance?.CurrentClass;
            WeaponType[] whitelist = cls?.WeaponWhitelist ?? unit.AllowedWeaponTypes;
            if (whitelist == null || tracker == null) return;

            var allTypes = new[] {
                WeaponType.Sword, WeaponType.Lance, WeaponType.Bow, WeaponType.Axe,
                WeaponType.AnimaTome, WeaponType.LightTome, WeaponType.DarkTome, WeaponType.Staff
            };
            string[] names = { "Sword", "Lance", "Bow", "Axe", "Anima", "Light", "Dark", "Staff" };

            for (int i = 0; i < allTypes.Length; i++)
            {
                var rankT = FindTMP(page, "Wep_" + names[i] + "/Rank");
                if (rankT == null) continue;

                bool hasAccess = tracker.HasAccess(allTypes[i]);
                if (hasAccess)
                {
                    var rank = tracker.GetRank(allTypes[i]);
                    rankT.text = rank.ToString();

                    // Update WEXP bar
                    var barFillT = FindDeep(page, "Wep_" + names[i] + "/WexpFill");
                    if (barFillT != null)
                    {
                        int wexp = tracker.GetWexp(allTypes[i]);
                        int threshold = WeaponRankTracker.GetThreshold(rank);
                        float fill = threshold > 0 ? (float)wexp / threshold : 0;
                        barFillT.GetComponent<RectTransform>().anchorMax = new Vector2(Mathf.Clamp01(fill), 1);
                    }
                }
                else
                {
                    rankT.text = "--";
                }
            }
        }

        // ================================================================
        // Inventory page binding
        // ================================================================

        void BindInventoryPage()
        {
            if (pages == null || pages[1] == null) return;
            var page = pages[1].transform;
            var inv = unit.Inventory;

            visibleItemNameTMPs.Clear();
            visibleItemSlotIndices.Clear();

            // Bind item slots by finding child items in ItemsList
            var listT = FindDeep(page, "ItemsList");
            if (listT == null || inv == null) return;

            int slotIdx = 0;
            for (int i = 0; i < listT.childCount && slotIdx < UnitInventory.Capacity; i++)
            {
                var child = listT.GetChild(i);
                var nameT = FindTMP(child, "Name");
                var usesT = FindTMP(child, "Uses");
                if (nameT == null && usesT == null) continue;

                var item = inv.GetSlot(slotIdx);
                if (item.kind != ItemKind.None)
                {
                    if (nameT != null) nameT.text = item.DisplayName;
                    if (usesT != null) usesT.text = item.CurrentUses + " / " + item.MaxUses;
                    if (nameT != null)
                    {
                        visibleItemNameTMPs.Add(nameT);
                        visibleItemSlotIndices.Add(slotIdx);
                    }
                }
                else
                {
                    if (nameT != null) nameT.text = "";
                    if (usesT != null) usesT.text = "";
                }
                slotIdx++;
            }

            selectedItemRow = Mathf.Clamp(selectedItemRow, 0, Mathf.Max(0, visibleItemNameTMPs.Count - 1));
            UpdateItemHighlight();

            // Bind equipment derived stats
            var wpn = unit.equippedWeapon;
            var inst = unit.UnitInstance;
            bool hasWeapon = !wpn.IsEmpty && !wpn.IsBroken && inst != null;

            var eBox = FindDeep(page, "EquipmentBox");
            if (eBox != null && inst != null)
            {
                SetDerived(eBox, "Rng", hasWeapon ? wpn.minRange + (wpn.maxRange > wpn.minRange ? "-" + wpn.maxRange : "") : "\u2014");
                SetDerived(eBox, "Atk", hasWeapon ? ComputeAtk(inst, wpn).ToString() : "\u2014");
                SetDerived(eBox, "Hit", hasWeapon ? ComputeHit(inst, wpn).ToString() : "\u2014");
                SetDerived(eBox, "Crit", hasWeapon ? ComputeCrit(inst, wpn).ToString() : "\u2014");
                int as_ = ComputeAS(inst, wpn, hasWeapon);
                SetDerived(eBox, "Avo", ComputeAvo(inst, as_).ToString());
            }
        }

        // ================================================================
        // Supports page binding
        // ================================================================

        void BindSupportsPage()
        {
            if (pages == null || pages.Length < 3 || pages[2] == null) return;
            if (supportsList == null || supportRowTemplate == null) return;
            var inst = unit.UnitInstance;

            visibleBondNameTMPs.Clear();
            visibleBondData.Clear();

            // Clear existing rows (every cloned row except the template itself).
            for (int i = supportsList.childCount - 1; i >= 0; i--)
            {
                var c = supportsList.GetChild(i);
                if (c.gameObject == supportRowTemplate) continue;
                Object.Destroy(c.gameObject);
            }

            var bonds = (supportProvider != null && inst != null)
                ? supportProvider.GetBonds(inst)
                : null;

            bool anyBonds = bonds != null && bonds.Count > 0;
            if (supportsEmptyText != null) supportsEmptyText.gameObject.SetActive(!anyBonds);
            if (!anyBonds) return;

            // Sort: higher stage first, alphabetical within.
            var sorted = new System.Collections.Generic.List<SupportBond>(bonds);
            sorted.Sort((a, b) =>
            {
                int cmp = ((int)b.Stage).CompareTo((int)a.Stage);
                if (cmp != 0) return cmp;
                string na = a.Partner != null ? a.Partner.UnitName : "";
                string nb = b.Partner != null ? b.Partner.UnitName : "";
                return string.Compare(na, nb, System.StringComparison.OrdinalIgnoreCase);
            });

            foreach (var bond in sorted)
            {
                var row = Object.Instantiate(supportRowTemplate, supportsList);
                row.SetActive(true);
                row.name = "Bond_" + (bond.Partner != null ? bond.Partner.UnitName : "?");
                PopulateSupportRow(row.transform, bond);

                var nameT = FindTMP(row.transform, "Name");
                if (nameT != null)
                {
                    visibleBondNameTMPs.Add(nameT);
                    visibleBondData.Add(bond);
                }
            }

            selectedBondRow = Mathf.Clamp(selectedBondRow, 0, Mathf.Max(0, visibleBondNameTMPs.Count - 1));
            UpdateBondHighlight();
        }

        void PopulateSupportRow(Transform row, SupportBond bond)
        {
            float rowAlpha = bond.IsDeceased ? 0.4f : 1f;

            // Portrait
            var portraitT = FindDeep(row, "Portrait");
            if (portraitT != null)
            {
                var img = portraitT.GetComponent<Image>();
                if (img != null && bond.Partner != null && bond.Partner.Portrait != null)
                {
                    img.sprite = bond.Partner.Portrait;
                    img.color = bond.IsDeceased ? new Color(0.55f, 0.55f, 0.55f, rowAlpha) : Color.white;
                    img.material = bond.IsDeceased ? desaturatedMaterial : null;
                }
            }

            // Memorial diya (deceased)
            var diyaT = FindDeep(row, "DiyaMemorial");
            if (diyaT != null)
            {
                diyaT.gameObject.SetActive(bond.IsDeceased);
                var img = diyaT.GetComponent<Image>();
                if (img != null && diyaMemorial != null) img.sprite = diyaMemorial;
            }

            // Notif (conversation available)
            var notifT = FindDeep(row, "Notif");
            if (notifT != null)
            {
                notifT.gameObject.SetActive(bond.ConversationAvailable && !bond.IsDeceased);
                var img = notifT.GetComponent<Image>();
                if (img != null && notifBadge != null) img.sprite = notifBadge;
            }

            // Name (strike-through when deceased)
            var nameT = FindTMP(row, "Name");
            if (nameT != null && bond.Partner != null)
            {
                nameT.text = bond.Partner.UnitName;
                nameT.color = bond.IsDeceased
                    ? new Color(ColSilver.r, ColSilver.g, ColSilver.b, rowAlpha)
                    : ColIvory;
                nameT.fontStyle = bond.IsDeceased ? TMPro.FontStyles.Bold | TMPro.FontStyles.Strikethrough : TMPro.FontStyles.Bold;
            }

            // Pips — 3 slots; encounter pip for level 0, unlit for 0-lit, lit for progressive.
            var pipsT = FindDeep(row, "BondPips");
            if (pipsT != null)
            {
                int stageInt = (int)bond.Stage;
                for (int i = 0; i < 3 && i < pipsT.childCount; i++)
                {
                    var pip = pipsT.GetChild(i).GetComponent<Image>();
                    if (pip == null) continue;
                    Sprite sprite = i < stageInt
                        ? bondPipLit
                        : (stageInt == 0 && i == 0 ? bondPipEncounter : bondPipUnlit);
                    if (sprite != null) pip.sprite = sprite;
                    pip.color = bond.IsDeceased ? new Color(1, 1, 1, 0.35f) : Color.white;
                }
            }

            // Shapath icon (oath witnessed)
            var shapathT = FindDeep(row, "ShapathIcon");
            if (shapathT != null)
            {
                shapathT.gameObject.SetActive(bond.ShapathWitnessed);
                var img = shapathT.GetComponent<Image>();
                if (img != null && shapathIcon != null) img.sprite = shapathIcon;
            }

            // Bonus teaser text — defer detail numbers to UnitInfoSupportDetailUI.
            var bonusT = FindTMP(row, "Bonus");
            if (bonusT != null)
            {
                bonusT.text = bond.IsDeceased ? "Saathi \u2014 frozen"
                    : bond.Stage == BondStage.Encounter ? "\u2014"
                    : "View";
                bonusT.color = bond.IsDeceased
                    ? new Color(ColSilver.r, ColSilver.g, ColSilver.b, 0.6f)
                    : ColGreen;
            }
        }

        // ================================================================
        // Combat formula helpers
        // ================================================================

        static int ComputeAtk(UnitInstance inst, WeaponData wpn)
        {
            int baseStat = wpn.damageType == DamageType.Physical
                ? inst.Stats[StatIndex.Str]
                : inst.Stats[StatIndex.Mag];
            return baseStat + wpn.might;
        }

        static int ComputeHit(UnitInstance inst, WeaponData wpn)
        {
            return inst.Stats[StatIndex.Skl] * 2 + inst.Stats[StatIndex.Niyati] + wpn.hit;
        }

        static int ComputeCrit(UnitInstance inst, WeaponData wpn)
        {
            int classCrit = inst.CurrentClass != null ? inst.CurrentClass.CritBonus : 0;
            return inst.Stats[StatIndex.Skl] / 2 + wpn.crit + classCrit;
        }

        static int ComputeAS(UnitInstance inst, WeaponData wpn, bool hasWeapon)
        {
            int spd = inst.Stats[StatIndex.Spd];
            if (!hasWeapon) return spd;
            int penalty = Mathf.Max(0, wpn.weight - inst.Stats[StatIndex.Con]);
            return spd - penalty;
        }

        static int ComputeAvo(UnitInstance inst, int attackSpeed)
        {
            return attackSpeed * 2 + inst.Stats[StatIndex.Niyati];
        }

        // ================================================================
        // Utility
        // ================================================================

        static Color HpColorForFraction(float frac)
        {
            if (frac > 0.50f) return HpGreen;
            if (frac > 0.25f) return HpYellow;
            return HpRed;
        }

        Sprite HpSpriteForFraction(float frac)
        {
            if (frac > 0.50f) return hpFillGreen;
            if (frac > 0.25f) return hpFillYellow;
            return hpFillRed;
        }

        void SetDerived(Transform root, string statName, string value)
        {
            var t = FindTMP(root, "Derived_" + statName + "/Value");
            if (t != null) t.text = value;
        }

        static TextMeshProUGUI FindTMP(Transform root, string path)
        {
            var t = FindDeep(root, path);
            return t != null ? t.GetComponent<TextMeshProUGUI>() : null;
        }

        static Transform FindDeep(Transform parent, string path)
        {
            // Support slash-separated paths like "StatL_Str/Value"
            string[] parts = path.Split('/');
            Transform current = null;

            // Find the first part
            current = FindChildRecursive(parent, parts[0]);
            if (current == null) return null;

            // Navigate remaining parts as direct children
            for (int i = 1; i < parts.Length; i++)
            {
                current = current.Find(parts[i]);
                if (current == null) return null;
            }
            return current;
        }

        static Transform FindChildRecursive(Transform parent, string name)
        {
            for (int i = 0; i < parent.childCount; i++)
            {
                var child = parent.GetChild(i);
                if (child.name == name) return child;
                var found = FindChildRecursive(child, name);
                if (found != null) return found;
            }
            return null;
        }
    }
}
