using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace ProjectAstra.Core.UI
{
    // UI-02 page 3 detail view. Modal inspector for a single support bond.
    // Shows combined SP-02 affinity bonuses, Bandhan promise text, Shapath icon.
    public class UnitInfoSupportDetailUI : MonoBehaviour
    {
        public static bool HasInputFocus { get; private set; }

        [SerializeField] Image _portraitImage;
        [SerializeField] TextMeshProUGUI _nameText;
        [SerializeField] TextMeshProUGUI _atkText;
        [SerializeField] TextMeshProUGUI _defText;
        [SerializeField] TextMeshProUGUI _hitText;
        [SerializeField] TextMeshProUGUI _avoText;
        [SerializeField] TextMeshProUGUI _critText;
        [SerializeField] TextMeshProUGUI _critAvoText;
        [SerializeField] TextMeshProUGUI _promiseText;
        [SerializeField] GameObject _promiseContainer;
        [SerializeField] GameObject _shapathIcon;

        System.Action _onClose;

        public void Show(UnitInstance unit, SupportBond bond, ISupportBonusProvider provider, System.Action onClose)
        {
            _onClose = onClose;
            Populate(unit, bond, provider);
            gameObject.SetActive(true);
            HasInputFocus = true;
            Subscribe();
        }

        public void Hide()
        {
            HasInputFocus = false;
            Unsubscribe();
            gameObject.SetActive(false);
            var cb = _onClose; _onClose = null;
            cb?.Invoke();
        }

        void Subscribe()
        {
            if (InputManager.Instance == null) return;
            InputManager.Instance.OnCancel += Hide;
        }

        void Unsubscribe()
        {
            if (InputManager.Instance == null) return;
            InputManager.Instance.OnCancel -= Hide;
        }

        void Populate(UnitInstance unit, SupportBond bond, ISupportBonusProvider provider)
        {
            if (_portraitImage != null && bond.Partner != null && bond.Partner.Portrait != null)
                _portraitImage.sprite = bond.Partner.Portrait;
            if (_nameText != null) _nameText.text = bond.Partner != null ? bond.Partner.UnitName : "";

            var bonus = provider != null ? provider.GetPairBonus(unit, bond.Partner, bond.Stage) : default;
            SetBonus(_atkText,     "Atk",     bonus.Atk);
            SetBonus(_defText,     "Def",     bonus.Def);
            SetBonus(_hitText,     "Hit",     bonus.Hit);
            SetBonus(_avoText,     "Avo",     bonus.Avo);
            SetBonus(_critText,    "Crit",    bonus.Crit);
            SetBonus(_critAvoText, "CritAvo", bonus.CritAvo);

            bool showPromise = !string.IsNullOrEmpty(bond.PromiseText);
            if (_promiseContainer != null) _promiseContainer.SetActive(showPromise);
            if (_promiseText != null && showPromise) _promiseText.text = bond.PromiseText;

            if (_shapathIcon != null) _shapathIcon.SetActive(bond.ShapathWitnessed);
        }

        static void SetBonus(TextMeshProUGUI t, string label, int value)
        {
            if (t == null) return;
            t.text = value != 0 ? label + " +" + value : label + " \u2014";
        }
    }
}
