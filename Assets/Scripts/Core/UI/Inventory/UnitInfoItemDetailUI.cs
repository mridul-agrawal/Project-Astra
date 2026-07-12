using System.Text;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using ProjectAstra.Core.Input;

namespace ProjectAstra.Core.UI.Inventory
{
    // UI-02 page 2 detail view. Read-only item inspector opened on top of the Unit
    // Info Panel's inventory page. Close with Cancel.
    public class UnitInfoItemDetailUI : MonoBehaviour
    {
        public static bool HasInputFocus { get; private set; }

        [SerializeField] TextMeshProUGUI nameText;
        [SerializeField] TextMeshProUGUI typeText;
        [SerializeField] TextMeshProUGUI mightText;
        [SerializeField] TextMeshProUGUI hitText;
        [SerializeField] TextMeshProUGUI critText;
        [SerializeField] TextMeshProUGUI weightText;
        [SerializeField] TextMeshProUGUI rangeText;
        [SerializeField] TextMeshProUGUI rankReqText;
        [SerializeField] TextMeshProUGUI effectivenessText;
        [SerializeField] TextMeshProUGUI specialText;
        [SerializeField] TextMeshProUGUI descriptionText;

        System.Action onClose;

        public void Show(InventoryItem item, System.Action onClose)
        {
            this.onClose = onClose;
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

        void Populate(InventoryItem item)
        {
            bool isWeapon = item.kind == ItemKind.Weapon;
            bool isConsumable = item.kind == ItemKind.Consumable;

            if (nameText != null) nameText.text = string.IsNullOrEmpty(item.DisplayName) ? "—" : item.DisplayName;
            if (typeText != null)
            {
                string typeStr = isWeapon ? item.weapon.weaponType.ToString()
                    : isConsumable ? "Consumable"
                    : "Item";
                typeText.text = typeStr.ToUpper();
            }

            // Weapon / staff stats (staves are weapons with weaponType==Staff)
            if (isWeapon)
            {
                var w = item.weapon;
                SetText(mightText,  "Mt " + w.might);
                SetText(hitText,    "Hit " + w.hit);
                SetText(critText,   "Crit " + w.crit);
                SetText(weightText, "Wt " + w.weight);
                string range = w.minRange == w.maxRange ? w.minRange.ToString() : (w.minRange + "\u2013" + w.maxRange);
                SetText(rangeText,  "Rng " + range);
                SetText(rankReqText, "Rank " + w.minRank);
            }
            else
            {
                SetText(mightText, "\u2014"); SetText(hitText, "\u2014"); SetText(critText, "\u2014");
                SetText(weightText, "\u2014"); SetText(rangeText, "\u2014"); SetText(rankReqText, "\u2014");
            }

            // Effectiveness tags
            if (effectivenessText != null)
            {
                if (isWeapon && item.weapon.effectivenessTargets != null && item.weapon.effectivenessTargets.Length > 0)
                {
                    var sb = new StringBuilder();
                    foreach (var t in item.weapon.effectivenessTargets)
                    {
                        if (sb.Length > 0) sb.Append(", ");
                        sb.Append("Effective vs. ").Append(t);
                    }
                    effectivenessText.text = sb.ToString();
                    effectivenessText.gameObject.SetActive(true);
                }
                else effectivenessText.gameObject.SetActive(false);
            }

            // Special props
            if (specialText != null)
            {
                var sb = new StringBuilder();
                if (isWeapon && item.weapon.brave) sb.AppendLine("Brave — attacks twice");
                if (isWeapon && item.weapon.minRange != item.weapon.maxRange)
                    sb.Append(item.weapon.minRange).Append("\u2013").Append(item.weapon.maxRange).AppendLine(" Range");
                specialText.text = sb.ToString().TrimEnd();
                specialText.gameObject.SetActive(sb.Length > 0);
            }

            // Consumable description
            if (descriptionText != null)
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
                    descriptionText.text = desc;
                    descriptionText.gameObject.SetActive(!string.IsNullOrEmpty(desc));
                }
                else descriptionText.gameObject.SetActive(false);
            }
        }

        static void SetText(TextMeshProUGUI t, string s) { if (t != null) t.text = s; }
    }
}
