using System.Text;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace ProjectAstra.Core.UI
{
    // UI-02 page 2 detail view. Read-only item inspector opened on top of the Unit
    // Info Panel's inventory page. Close with Cancel.
    public class UnitInfoItemDetailUI : MonoBehaviour
    {
        public static bool HasInputFocus { get; private set; }

        [SerializeField] TextMeshProUGUI _nameText;
        [SerializeField] TextMeshProUGUI _typeText;
        [SerializeField] TextMeshProUGUI _mightText;
        [SerializeField] TextMeshProUGUI _hitText;
        [SerializeField] TextMeshProUGUI _critText;
        [SerializeField] TextMeshProUGUI _weightText;
        [SerializeField] TextMeshProUGUI _rangeText;
        [SerializeField] TextMeshProUGUI _rankReqText;
        [SerializeField] TextMeshProUGUI _effectivenessText;
        [SerializeField] TextMeshProUGUI _specialText;
        [SerializeField] TextMeshProUGUI _descriptionText;

        System.Action _onClose;

        public void Show(InventoryItem item, System.Action onClose)
        {
            _onClose = onClose;
            Populate(item);
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

        void Populate(InventoryItem item)
        {
            bool isWeapon = item.kind == ItemKind.Weapon;
            bool isConsumable = item.kind == ItemKind.Consumable;

            if (_nameText != null) _nameText.text = string.IsNullOrEmpty(item.DisplayName) ? "—" : item.DisplayName;
            if (_typeText != null)
            {
                string typeStr = isWeapon ? item.weapon.weaponType.ToString()
                    : isConsumable ? "Consumable"
                    : "Item";
                _typeText.text = typeStr.ToUpper();
            }

            // Weapon / staff stats (staves are weapons with weaponType==Staff)
            if (isWeapon)
            {
                var w = item.weapon;
                SetText(_mightText,  "Mt " + w.might);
                SetText(_hitText,    "Hit " + w.hit);
                SetText(_critText,   "Crit " + w.crit);
                SetText(_weightText, "Wt " + w.weight);
                string range = w.minRange == w.maxRange ? w.minRange.ToString() : (w.minRange + "\u2013" + w.maxRange);
                SetText(_rangeText,  "Rng " + range);
                SetText(_rankReqText, "Rank " + w.minRank);
            }
            else
            {
                SetText(_mightText, "\u2014"); SetText(_hitText, "\u2014"); SetText(_critText, "\u2014");
                SetText(_weightText, "\u2014"); SetText(_rangeText, "\u2014"); SetText(_rankReqText, "\u2014");
            }

            // Effectiveness tags
            if (_effectivenessText != null)
            {
                if (isWeapon && item.weapon.effectivenessTargets != null && item.weapon.effectivenessTargets.Length > 0)
                {
                    var sb = new StringBuilder();
                    foreach (var t in item.weapon.effectivenessTargets)
                    {
                        if (sb.Length > 0) sb.Append(", ");
                        sb.Append("Effective vs. ").Append(t);
                    }
                    _effectivenessText.text = sb.ToString();
                    _effectivenessText.gameObject.SetActive(true);
                }
                else _effectivenessText.gameObject.SetActive(false);
            }

            // Special props
            if (_specialText != null)
            {
                var sb = new StringBuilder();
                if (isWeapon && item.weapon.brave) sb.AppendLine("Brave — attacks twice");
                if (isWeapon && item.weapon.minRange != item.weapon.maxRange)
                    sb.Append(item.weapon.minRange).Append("\u2013").Append(item.weapon.maxRange).AppendLine(" Range");
                _specialText.text = sb.ToString().TrimEnd();
                _specialText.gameObject.SetActive(sb.Length > 0);
            }

            // Consumable description
            if (_descriptionText != null)
            {
                if (isConsumable)
                {
                    var c = item.consumable;
                    string desc = c.type switch
                    {
                        ConsumableType.Vulnerary   => "Restores " + c.magnitude + " HP",
                        ConsumableType.StatBooster => "+" + c.magnitude + " " + c.targetStat + " permanently",
                        _ => ""
                    };
                    _descriptionText.text = desc;
                    _descriptionText.gameObject.SetActive(!string.IsNullOrEmpty(desc));
                }
                else _descriptionText.gameObject.SetActive(false);
            }
        }

        static void SetText(TextMeshProUGUI t, string s) { if (t != null) t.text = s; }
    }
}
