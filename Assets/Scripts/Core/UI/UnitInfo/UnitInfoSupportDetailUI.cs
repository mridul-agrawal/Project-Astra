using TMPro;
using UnityEngine;
using UnityEngine.UI;
using ProjectAstra.Core.Input;
using ProjectAstra.Core.Support;
using ProjectAstra.Core.UI.Interfaces;
using ProjectAstra.Core.Units;

namespace ProjectAstra.Core.UI.UnitInfo
{
    // UI-02 page 3 detail view. Modal inspector for a single support bond.
    // Shows combined SP-02 affinity bonuses, Bandhan promise text, Shapath icon.
    public class UnitInfoSupportDetailUI : MonoBehaviour
    {
        public static bool HasInputFocus { get; private set; }

        [SerializeField] Image portraitImage;
        [SerializeField] TextMeshProUGUI nameText;
        [SerializeField] TextMeshProUGUI atkText;
        [SerializeField] TextMeshProUGUI defText;
        [SerializeField] TextMeshProUGUI hitText;
        [SerializeField] TextMeshProUGUI avoText;
        [SerializeField] TextMeshProUGUI critText;
        [SerializeField] TextMeshProUGUI critAvoText;
        [SerializeField] TextMeshProUGUI promiseText;
        [SerializeField] GameObject promiseContainer;
        [SerializeField] GameObject shapathIcon;

        System.Action onClose;

        public void Show(UnitInstance unit, SupportBond bond, ISupportBonusProvider provider, System.Action onClose)
        {
            this.onClose = onClose;
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
            var cb = onClose; onClose = null;
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
            if (portraitImage != null && bond.Partner != null && bond.Partner.Portrait != null)
                portraitImage.sprite = bond.Partner.Portrait;
            if (nameText != null) nameText.text = bond.Partner != null ? bond.Partner.UnitName : "";

            var bonus = provider != null ? provider.GetPairBonus(unit, bond.Partner, bond.Stage) : default;
            SetBonus(atkText,     "Atk",     bonus.Atk);
            SetBonus(defText,     "Def",     bonus.Def);
            SetBonus(hitText,     "Hit",     bonus.Hit);
            SetBonus(avoText,     "Avo",     bonus.Avo);
            SetBonus(critText,    "Crit",    bonus.Crit);
            SetBonus(critAvoText, "CritAvo", bonus.CritAvo);

            bool showPromise = !string.IsNullOrEmpty(bond.PromiseText);
            if (promiseContainer != null) promiseContainer.SetActive(showPromise);
            if (promiseText != null && showPromise) promiseText.text = bond.PromiseText;

            if (shapathIcon != null) shapathIcon.SetActive(bond.ShapathWitnessed);
        }

        static void SetBonus(TextMeshProUGUI t, string label, int value)
        {
            if (t == null) return;
            t.text = value != 0 ? label + " +" + value : label + " \u2014";
        }
    }
}
