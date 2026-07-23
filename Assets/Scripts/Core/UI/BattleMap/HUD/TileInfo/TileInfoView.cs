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

        private RectTransform rect;
        private HudCorner corner;
        private bool cornerInit;

        private void Awake()
        {
            rect = Root != null ? Root.GetComponent<RectTransform>() : GetComponent<RectTransform>();
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

            ApplyCorner(model.Corner);
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

        // Tile info always sits on the bottom row; the composition root picks which
        // bottom corner. Docking is the shared corner-layout used by every HUD panel.
        private void ApplyCorner(HudCorner target)
        {
            if (rect == null || (cornerInit && target == corner)) return;
            corner = target;
            cornerInit = true;
            HudCornerLayout.Apply(rect, target, EdgePad);
        }

        private static string FormatStat(int v) => v >= 0 ? "+" + v : v.ToString();
    }
}
