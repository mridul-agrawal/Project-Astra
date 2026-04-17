using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace ProjectAstra.Core.UI
{
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

        [SerializeField] Sprite _hpFillGreen;
        [SerializeField] Sprite _hpFillYellow;
        [SerializeField] Sprite _hpFillRed;
        [SerializeField] Sprite _pageDotActive;
        [SerializeField] Sprite _pageDotInactive;

        [Header("Bond pip / support row chrome")]
        [SerializeField] Sprite _bondPipLit;
        [SerializeField] Sprite _bondPipUnlit;
        [SerializeField] Sprite _bondPipEncounter;
        [SerializeField] Sprite _notifBadge;
        [SerializeField] Sprite _diyaMemorial;
        [SerializeField] Sprite _shapathIcon;
        [SerializeField] Sprite _thresholdMark;

        [Header("Affinity icons (index matches PanchaBhuta enum, 0=None unused)")]
        [SerializeField] Sprite[] _affinityIcons;

        [Header("Material for deceased portrait")]
        [SerializeField] Material _desaturatedMaterial;

        [Header("Detail sub-panels")]
        [SerializeField] UnitInfoItemDetailUI _itemDetail;
        [SerializeField] UnitInfoSupportDetailUI _supportDetail;

        TestUnit _unit;
        UnitInfoContext _ctx = UnitInfoContext.BattleMap;
        int _currentPage;
        int _maxPage;
        GameObject[] _pages;
        Image[] _dots;
        GameObject _dimOverlay;

        // Shared section refs
        TextMeshProUGUI _unitNameText;
        TextMeshProUGUI _classNameText;
        TextMeshProUGUI _lvValueText;
        TextMeshProUGUI _expLabelText;
        TextMeshProUGUI _expValueText;
        TextMeshProUGUI _hpCurrentText;
        TextMeshProUGUI _hpSepText;
        TextMeshProUGUI _hpMaxText;
        RectTransform _hpBarFill;
        Image _hpBarFillImage;
        RectTransform _hpBarOuter;
        Image _portraitImage;
        Image _stressOverlayImage;
        Image _affinityIconImage;
        TextMeshProUGUI _affinityLabelText;
        TextMeshProUGUI _personalityLabelText;

        // Stats page extra refs
        TextMeshProUGUI _movValueText;
        GameObject _dharmicThresholdBanner;

        // Supports page
        RectTransform _supportsList;
        GameObject _supportRowTemplate;
        TextMeshProUGUI _supportsEmptyText;

        // Intra-page row selection
        int _selectedItemRow;
        int _selectedBondRow;
        readonly System.Collections.Generic.List<TextMeshProUGUI> _visibleItemNameTMPs = new();
        readonly System.Collections.Generic.List<int> _visibleItemSlotIndices = new();
        readonly System.Collections.Generic.List<TextMeshProUGUI> _visibleBondNameTMPs = new();
        readonly System.Collections.Generic.List<SupportBond> _visibleBondData = new();

        // Providers (scene-level, optional)
        ISupportProvider _supportProvider;
        ITemporaryModifierProvider _modifierProvider;
        ISupportBonusProvider _supportBonusProvider;
        IFogOfWarProvider _fogProvider;

        Material _portraitOriginalMaterial;
        float _hpBarReferenceWidth;

        bool _refsDiscovered;
        bool _providersDiscovered;

        public void Show(TestUnit unit) => Show(unit, UnitInfoContext.BattleMap);

        public void Show(TestUnit unit, UnitInfoContext ctx)
        {
            if (unit == null) return;

            if (!_providersDiscovered) DiscoverProviders();

            // HM-08 / MT-04. Unrevealed enemies in fog of war can't be inspected.
            if (_fogProvider != null && unit.UnitInstance != null && !_fogProvider.IsRevealed(unit.UnitInstance))
                return;

            _unit = unit;
            _ctx = ctx;
            _currentPage = 0;
            _maxPage = unit.faction == Faction.Player ? 2 : 1;

            if (!_refsDiscovered) DiscoverReferences();

            BindSharedSection();
            SetActivePage(0);
            UpdatePageIndicator();

            if (_dimOverlay != null) _dimOverlay.SetActive(true);
            gameObject.SetActive(true);

            HasInputFocus = true;
            SubscribeInput();
        }

        void DiscoverProviders()
        {
            _providersDiscovered = true;
            // Scene-level optional providers; any missing → safe defaults.
            _supportProvider      = FindInterface<ISupportProvider>();
            _modifierProvider     = FindInterface<ITemporaryModifierProvider>();
            _supportBonusProvider = FindInterface<ISupportBonusProvider>();
            _fogProvider          = FindInterface<IFogOfWarProvider>();
        }

        static T FindInterface<T>() where T : class
        {
            foreach (var mb in Object.FindObjectsByType<MonoBehaviour>(FindObjectsInactive.Include, FindObjectsSortMode.None))
                if (mb is T t) return t;
            return null;
        }

        public void Hide()
        {
            HasInputFocus = false;
            UnsubscribeInput();

            gameObject.SetActive(false);
            if (_dimOverlay != null) _dimOverlay.SetActive(false);

            _unit = null;
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
            if (_currentPage == 1) OpenSelectedItemDetail();
            else if (_currentPage == 2) OpenSelectedBondDetail();
        }

        void OpenSelectedItemDetail()
        {
            if (_itemDetail == null || _unit == null) return;
            if (_visibleItemSlotIndices.Count == 0) return;
            int row = Mathf.Clamp(_selectedItemRow, 0, _visibleItemSlotIndices.Count - 1);
            int slotIdx = _visibleItemSlotIndices[row];
            var slot = _unit.Inventory?.GetSlot(slotIdx) ?? InventoryItem.None;
            if (slot.kind == ItemKind.None) return;
            UnsubscribeInput();
            _itemDetail.Show(slot, () => { SubscribeInput(); });
        }

        void OpenSelectedBondDetail()
        {
            if (_supportDetail == null || _unit == null || _unit.UnitInstance == null) return;
            if (_visibleBondData.Count == 0) return;
            int row = Mathf.Clamp(_selectedBondRow, 0, _visibleBondData.Count - 1);
            UnsubscribeInput();
            _supportDetail.Show(_unit.UnitInstance, _visibleBondData[row], _supportBonusProvider, () => { SubscribeInput(); });
        }

        void NavigatePage(Vector2Int dir)
        {
            // Horizontal → flip pages. Vertical → move row selection on pages 1/2.
            if (dir.x > 0) NextPage();
            else if (dir.x < 0) PrevPage();
            else if (dir.y != 0)
            {
                int delta = dir.y < 0 ? 1 : -1; // Unity's +y is up-on-screen; rows flow downward.
                if (_currentPage == 1) MoveItemSelection(delta);
                else if (_currentPage == 2) MoveBondSelection(delta);
            }
        }

        void MoveItemSelection(int delta)
        {
            if (_visibleItemNameTMPs.Count == 0) return;
            int n = _visibleItemNameTMPs.Count;
            _selectedItemRow = (_selectedItemRow + delta + n) % n;
            UpdateItemHighlight();
        }

        void MoveBondSelection(int delta)
        {
            if (_visibleBondNameTMPs.Count == 0) return;
            int n = _visibleBondNameTMPs.Count;
            _selectedBondRow = (_selectedBondRow + delta + n) % n;
            UpdateBondHighlight();
        }

        void UpdateItemHighlight()
        {
            for (int i = 0; i < _visibleItemNameTMPs.Count; i++)
            {
                var t = _visibleItemNameTMPs[i];
                if (t == null) continue;
                t.color = i == _selectedItemRow ? ColGold : ColIvory;
            }
        }

        void UpdateBondHighlight()
        {
            for (int i = 0; i < _visibleBondNameTMPs.Count; i++)
            {
                var t = _visibleBondNameTMPs[i];
                if (t == null) continue;
                if (i == _visibleBondData.Count) break;
                // Deceased rows stay silver/struck-through regardless of selection.
                if (_visibleBondData[i].IsDeceased)
                {
                    t.color = new Color(ColSilver.r, ColSilver.g, ColSilver.b, 0.4f);
                }
                else
                {
                    t.color = i == _selectedBondRow ? ColGold : ColIvory;
                }
            }
        }

        void NextPage()
        {
            int next = _currentPage >= _maxPage ? 0 : _currentPage + 1;
            SetActivePage(next);
        }

        void PrevPage()
        {
            int prev = _currentPage <= 0 ? _maxPage : _currentPage - 1;
            SetActivePage(prev);
        }

        void HandleCancel() => Hide();

        void SetActivePage(int page)
        {
            if (_pages == null) return;

            for (int i = 0; i < _pages.Length; i++)
                if (_pages[i] != null)
                    _pages[i].SetActive(i == page);

            // Reset row selection when entering a selection-aware page.
            if (page == 1) _selectedItemRow = 0;
            else if (page == 2) _selectedBondRow = 0;

            _currentPage = page;
            UpdatePageIndicator();
            BindCurrentPage();
        }

        void UpdatePageIndicator()
        {
            if (_dots == null) return;
            for (int i = 0; i < _dots.Length; i++)
            {
                if (_dots[i] == null) continue;
                bool isActive = i == _currentPage;
                if (_pageDotActive != null && _pageDotInactive != null)
                {
                    _dots[i].sprite = isActive ? _pageDotActive : _pageDotInactive;
                    _dots[i].color = Color.white;
                }
                else
                {
                    _dots[i].color = isActive
                        ? ColGold
                        : new Color(ColGold.r, ColGold.g, ColGold.b, 0.3f);
                }
            }
        }

        void BindCurrentPage()
        {
            switch (_currentPage)
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
            _refsDiscovered = true;

            // Dim overlay is a sibling
            var canvasT = transform.parent;
            if (canvasT != null)
            {
                var overlayT = canvasT.Find("UnitInfoDimOverlay");
                if (overlayT != null) _dimOverlay = overlayT.gameObject;
            }

            // Pages
            var container = FindDeep(transform, "PageContainer");
            if (container != null)
            {
                var stats = container.Find("StatsPage");
                var inv = container.Find("InventoryPage");
                var sup = container.Find("SupportsPage");
                _pages = new[]
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
                _dots = new Image[3];
                for (int i = 0; i < 3 && i < dotsParent.childCount; i++)
                    _dots[i] = dotsParent.GetChild(i).GetComponent<Image>();
            }

            // Shared section
            _unitNameText = FindTMP(transform, "UnitName");
            _classNameText = FindTMP(transform, "ClassName");
            _lvValueText = FindTMP(transform, "LvValue");
            _expLabelText = FindTMP(transform, "ExpLabel");
            _expValueText = FindTMP(transform, "ExpValue");
            _hpCurrentText = FindTMP(transform, "HpCurrent");
            _hpSepText = FindTMP(transform, "HpSep");
            _hpMaxText = FindTMP(transform, "HpMax");

            var hpFillT = FindDeep(transform, "HpBarFill");
            if (hpFillT != null)
            {
                _hpBarFill = hpFillT.GetComponent<RectTransform>();
                _hpBarFillImage = hpFillT.GetComponent<Image>();
            }
            var hpOuterT = FindDeep(transform, "HpBarOuter");
            if (hpOuterT != null)
            {
                _hpBarOuter = hpOuterT.GetComponent<RectTransform>();
                _hpBarReferenceWidth = _hpBarOuter.rect.width;
            }

            var portraitT = FindDeep(transform, "PortraitPlaceholder");
            if (portraitT != null)
            {
                _portraitImage = portraitT.GetComponent<Image>();
                if (_portraitImage != null) _portraitOriginalMaterial = _portraitImage.material;
            }

            // New shared-section nodes
            var stressT = FindDeep(transform, "StressOverlay");
            if (stressT != null) _stressOverlayImage = stressT.GetComponent<Image>();

            var affT = FindDeep(transform, "AffinIcon");
            if (affT != null) _affinityIconImage = affT.GetComponent<Image>();
            _affinityLabelText = FindTMP(transform, "AffinLabel");
            _personalityLabelText = FindTMP(transform, "PersonalityLabel");

            // Stats page extras
            _movValueText = FindTMP(transform, "StatR_Mov/Value");
            var dtbT = FindDeep(transform, "DharmicThresholdBanner");
            if (dtbT != null) _dharmicThresholdBanner = dtbT.gameObject;

            // Supports list
            var supListT = FindDeep(transform, "SupportList");
            if (supListT != null) _supportsList = supListT.GetComponent<RectTransform>();
            var supTemplateT = FindDeep(transform, "SupportRowTemplate");
            if (supTemplateT != null)
            {
                _supportRowTemplate = supTemplateT.gameObject;
                _supportRowTemplate.SetActive(false);
            }
            _supportsEmptyText = FindTMP(transform, "SupportsEmptyText");
        }

        // ================================================================
        // Shared section binding
        // ================================================================

        void BindSharedSection()
        {
            var inst = _unit.UnitInstance;
            var def = inst?.Definition;

            // Name
            if (_unitNameText != null)
            {
                string name = def != null && !string.IsNullOrEmpty(def.UnitName)
                    ? def.UnitName : _unit.gameObject.name;
                _unitNameText.text = name.ToUpper();
            }

            // Class
            if (_classNameText != null)
            {
                var cls = inst?.CurrentClass;
                _classNameText.text = cls != null ? cls.ClassName : "Unknown";
            }

            // Level
            if (_lvValueText != null)
            {
                int level = inst?.Level ?? 1;
                _lvValueText.text = level.ToString();
            }

            // EXP — "Lv. 20 (MAX)" when promoted level cap reached; hidden for enemy.
            bool showExp = _unit.faction != Faction.Enemy;
            bool atCap = inst != null && inst.IsAtLevelCap;
            if (_expValueText != null)
            {
                _expValueText.gameObject.SetActive(showExp);
                if (atCap) _expValueText.text = "MAX";
                else _expValueText.text = (inst?.CurrentEXP ?? 0) + " / " + UnitInstance.ExpPerLevel;
            }
            if (_expLabelText != null) _expLabelText.gameObject.SetActive(showExp);

            // HP
            int currentHP = inst != null ? inst.CurrentHP : _unit.currentHP;
            int maxHP = inst != null ? inst.MaxHP : _unit.maxHP;

            if (_hpCurrentText != null) _hpCurrentText.text = currentHP.ToString();
            if (_hpMaxText != null) _hpMaxText.text = maxHP.ToString();

            if (_hpBarFillImage != null)
            {
                float frac = maxHP > 0 ? Mathf.Clamp01((float)currentHP / maxHP) : 0;
                var sprite = HpSpriteForFraction(frac);
                if (sprite != null)
                {
                    _hpBarFillImage.sprite = sprite;
                    _hpBarFillImage.color = Color.white;
                }
                else
                {
                    _hpBarFillImage.color = HpColorForFraction(frac);
                }

                if (_hpBarFillImage.type == Image.Type.Filled)
                {
                    _hpBarFillImage.fillAmount = frac;
                }
                else if (_hpBarFill != null)
                {
                    _hpBarFill.anchorMax = new Vector2(frac, 1);
                }
            }

            // HP bar width scaling — proportional to MaxHP vs. a 60-HP reference, clamped to 1.5×.
            if (_hpBarOuter != null && _hpBarReferenceWidth > 0f)
            {
                float scale = Mathf.Clamp(maxHP / 60f, 0.35f, 1.5f);
                var size = _hpBarOuter.sizeDelta;
                size.x = _hpBarReferenceWidth * scale - _hpBarReferenceWidth; // delta over the baseline
                _hpBarOuter.sizeDelta = size;
            }

            // Portrait variant pick — dead > critical > wounded > full.
            if (_portraitImage != null && def != null)
            {
                bool isDead = (inst != null && inst.IsDead) || _ctx == UnitInfoContext.ObituaryFromSupportList;
                Sprite portrait = PickPortrait(def, inst?.HPThreshold ?? HPThreshold.Normal, isDead);
                _portraitImage.sprite = portrait;

                // Desaturation (dead state)
                if (isDead && _desaturatedMaterial != null)
                    _portraitImage.material = _desaturatedMaterial;
                else
                    _portraitImage.material = _portraitOriginalMaterial;
            }

            // Stress overlay — USE-04 Tier 1+ layers an extra sprite atop the portrait.
            if (_stressOverlayImage != null)
            {
                bool showStress = inst != null && inst.StressTier >= 1 && def != null && def.StressedOverlay != null;
                _stressOverlayImage.gameObject.SetActive(showStress);
                if (showStress) _stressOverlayImage.sprite = def.StressedOverlay;
            }

            // Affinity icon — SP-03.
            if (_affinityIconImage != null)
            {
                Sprite icon = GetAffinityIcon(def?.Affinity ?? PanchaBhuta.None);
                _affinityIconImage.gameObject.SetActive(icon != null);
                if (icon != null) _affinityIconImage.sprite = icon;
            }
            if (_affinityLabelText != null && def != null)
            {
                _affinityLabelText.text = AffinityLabel(def.Affinity);
            }

            // Personality label — Allied NPC only (CC-01).
            if (_personalityLabelText != null)
            {
                bool showPersonality = _unit.faction == Faction.Allied && def != null && def.Personality != Personality.None;
                _personalityLabelText.gameObject.SetActive(showPersonality);
                if (showPersonality) _personalityLabelText.text = "PERSONALITY: " + def.Personality.ToString().ToUpper();
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
            if (a == PanchaBhuta.None || _affinityIcons == null) return null;
            int idx = (int)a - 1; // skip None=0
            return (idx >= 0 && idx < _affinityIcons.Length) ? _affinityIcons[idx] : null;
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
            if (_pages == null || _pages[0] == null) return;
            var page = _pages[0].transform;
            var inst = _unit.UnitInstance;
            if (inst == null) return;

            var modifiers = _modifierProvider != null ? _modifierProvider.GetModifiers(inst) : default;
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
            bool allCapped = hasClass && _unit.faction == Faction.Player
                && inst.Stats[StatIndex.Str] >= caps[StatIndex.Str]
                && inst.Stats[StatIndex.Mag] >= caps[StatIndex.Mag]
                && inst.Stats[StatIndex.Skl] >= caps[StatIndex.Skl]
                && inst.Stats[StatIndex.Spd] >= caps[StatIndex.Spd]
                && inst.Stats[StatIndex.Niyati] >= caps[StatIndex.Niyati]
                && inst.Stats[StatIndex.Def] >= caps[StatIndex.Def]
                && inst.Stats[StatIndex.Res] >= caps[StatIndex.Res]
                && inst.Stats[StatIndex.Con] >= caps[StatIndex.Con];
            if (_dharmicThresholdBanner != null) _dharmicThresholdBanner.SetActive(allCapped);

            // Derived combat stats
            var wpn = _unit.equippedWeapon;
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
            var tracker = _unit.WeaponRankTracker;
            var cls = _unit.UnitInstance?.CurrentClass;
            WeaponType[] whitelist = cls?.WeaponWhitelist ?? _unit.AllowedWeaponTypes;
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
            if (_pages == null || _pages[1] == null) return;
            var page = _pages[1].transform;
            var inv = _unit.Inventory;

            _visibleItemNameTMPs.Clear();
            _visibleItemSlotIndices.Clear();

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
                        _visibleItemNameTMPs.Add(nameT);
                        _visibleItemSlotIndices.Add(slotIdx);
                    }
                }
                else
                {
                    if (nameT != null) nameT.text = "";
                    if (usesT != null) usesT.text = "";
                }
                slotIdx++;
            }

            _selectedItemRow = Mathf.Clamp(_selectedItemRow, 0, Mathf.Max(0, _visibleItemNameTMPs.Count - 1));
            UpdateItemHighlight();

            // Bind equipment derived stats
            var wpn = _unit.equippedWeapon;
            var inst = _unit.UnitInstance;
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
            if (_pages == null || _pages.Length < 3 || _pages[2] == null) return;
            if (_supportsList == null || _supportRowTemplate == null) return;
            var inst = _unit.UnitInstance;

            _visibleBondNameTMPs.Clear();
            _visibleBondData.Clear();

            // Clear existing rows (every cloned row except the template itself).
            for (int i = _supportsList.childCount - 1; i >= 0; i--)
            {
                var c = _supportsList.GetChild(i);
                if (c.gameObject == _supportRowTemplate) continue;
                Object.Destroy(c.gameObject);
            }

            var bonds = (_supportProvider != null && inst != null)
                ? _supportProvider.GetBonds(inst)
                : null;

            bool anyBonds = bonds != null && bonds.Count > 0;
            if (_supportsEmptyText != null) _supportsEmptyText.gameObject.SetActive(!anyBonds);
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
                var row = Object.Instantiate(_supportRowTemplate, _supportsList);
                row.SetActive(true);
                row.name = "Bond_" + (bond.Partner != null ? bond.Partner.UnitName : "?");
                PopulateSupportRow(row.transform, bond);

                var nameT = FindTMP(row.transform, "Name");
                if (nameT != null)
                {
                    _visibleBondNameTMPs.Add(nameT);
                    _visibleBondData.Add(bond);
                }
            }

            _selectedBondRow = Mathf.Clamp(_selectedBondRow, 0, Mathf.Max(0, _visibleBondNameTMPs.Count - 1));
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
                    img.material = bond.IsDeceased ? _desaturatedMaterial : null;
                }
            }

            // Memorial diya (deceased)
            var diyaT = FindDeep(row, "DiyaMemorial");
            if (diyaT != null)
            {
                diyaT.gameObject.SetActive(bond.IsDeceased);
                var img = diyaT.GetComponent<Image>();
                if (img != null && _diyaMemorial != null) img.sprite = _diyaMemorial;
            }

            // Notif (conversation available)
            var notifT = FindDeep(row, "Notif");
            if (notifT != null)
            {
                notifT.gameObject.SetActive(bond.ConversationAvailable && !bond.IsDeceased);
                var img = notifT.GetComponent<Image>();
                if (img != null && _notifBadge != null) img.sprite = _notifBadge;
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
                        ? _bondPipLit
                        : (stageInt == 0 && i == 0 ? _bondPipEncounter : _bondPipUnlit);
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
                if (img != null && _shapathIcon != null) img.sprite = _shapathIcon;
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
            if (frac > 0.50f) return _hpFillGreen;
            if (frac > 0.25f) return _hpFillYellow;
            return _hpFillRed;
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
