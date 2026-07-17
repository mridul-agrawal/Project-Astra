using TMPro;
using UnityEngine;

namespace ProjectAstra.Core.UI.BattleMap.HUD
{
    // Presentation for the Tile Info panel. Maps terrain bonuses onto the widgets
    // and flips the panel between bottom-left and bottom-right (FE GBA: the panel
    // sits opposite the cursor). Public fields are wired by BattleMapHUDBuilder.
    public sealed class TileInfoView : MonoBehaviour
    {
        [Header("Widgets")]
        public GameObject Root;
        public TextMeshProUGUI TileName;
        public TextMeshProUGUI StatValueDef;
        public TextMeshProUGUI StatValueAvo;
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

            if (TileName != null)     TileName.text     = model.TerrainName;
            if (StatValueDef != null) StatValueDef.text = FormatStat(model.Defense);
            if (StatValueAvo != null) StatValueAvo.text = FormatStat(model.Avoid);

            if (HealValue != null)
            {
                bool heals = model.Heal > 0;
                HealValue.gameObject.SetActive(heals);
                if (heals) HealValue.text = "Heal " + FormatStat(model.Heal) + " / turn";
            }

            ApplySide(model.PanelOnLeft);
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
