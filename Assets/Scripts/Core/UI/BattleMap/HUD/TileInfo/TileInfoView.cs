using TMPro;
using UnityEngine;

namespace ProjectAstra.Core.UI.BattleMap.HUD
{
    public sealed class TileInfoView : MonoBehaviour
    {
        [Header("Widgets")]
        public GameObject Root;
        public TextMeshProUGUI TileName;
        public GameObject StatDef;            // row: "Def" label + value together
        public TextMeshProUGUI StatValueDef;
        public GameObject StatAvo;            // row: "Avo" label + value together
        public TextMeshProUGUI StatValueAvo;
        public GameObject StatDivider;
        public TextMeshProUGUI HealValue;

        private const float EdgePad = 56f;
        private static readonly Vector2 LeftPos  = new Vector2( EdgePad, EdgePad);
        private static readonly Vector2 RightPos = new Vector2(-EdgePad, EdgePad);

        private RectTransform rect;
        private bool onLeft;

        private void Awake()
        {
            rect = Root != null ? Root.GetComponent<RectTransform>() : GetComponent<RectTransform>();
            onLeft = false; // build-time default sits bottom-right
        }

        public void SetVisible(bool visible)
        {
            if (Root != null) Root.SetActive(visible);
        }

        public void Render(TileInfoModel model)
        {
            if (model == null)
            {
                if (Root != null) Root.SetActive(false);
                return;
            }
            if (Root != null) Root.SetActive(true);

            if (TileName != null) TileName.text = model.TerrainName;

            ShowStat(StatDef, StatValueDef, model.Defense);
            ShowStat(StatAvo, StatValueAvo, model.Avoid);
            ShowHeal(model.Heal);
            ShowDivider(model);

            ApplySide(model.PanelOnLeft);
        }

        // A terrain bonus only earns a line when it actually does something.
        private static void ShowStat(GameObject row, TextMeshProUGUI value, int amount)
        {
            bool applies = amount != 0;
            if (row != null) row.SetActive(applies);
            if (applies && value != null) value.text = FormatStat(amount);
        }

        private void ShowHeal(int heal)
        {
            if (HealValue == null) return;
            bool heals = heal > 0;
            HealValue.gameObject.SetActive(heals);
            if (heals) HealValue.text = "Heal " + FormatStat(heal) + " / turn";
        }

        // The divider only belongs there when a bonus sits under it.
        private void ShowDivider(TileInfoModel model)
        {
            if (StatDivider == null) return;
            bool anyBonus = model.Defense != 0 || model.Avoid != 0 || model.Heal > 0;
            StatDivider.SetActive(anyBonus);
        }

        private void ApplySide(bool panelOnLeft)
        {
            if (rect == null || panelOnLeft == onLeft) return;
            onLeft = panelOnLeft;
            if (panelOnLeft)
            {
                rect.anchorMin = rect.anchorMax = new Vector2(0, 0);
                rect.pivot = new Vector2(0, 0);
                rect.anchoredPosition = LeftPos;
            }
            else
            {
                rect.anchorMin = rect.anchorMax = new Vector2(1, 0);
                rect.pivot = new Vector2(1, 0);
                rect.anchoredPosition = RightPos;
            }
        }

        private static string FormatStat(int v) => v >= 0 ? "+" + v : v.ToString();
    }
}
