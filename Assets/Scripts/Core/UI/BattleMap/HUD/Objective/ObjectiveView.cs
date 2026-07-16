using TMPro;
using UnityEngine;

namespace ProjectAstra.Core.UI.BattleMap.HUD
{
    // Presentation for the Objective panel. Always visible; just shows the mission
    // line and the turn counter. Public fields are wired by BattleMapHUDBuilder.
    public sealed class ObjectiveView : MonoBehaviour
    {
        [Header("Widgets")]
        public TextMeshProUGUI ObjText;
        public TextMeshProUGUI TurnNum;

        public void Render(ObjectiveModel model)
        {
            if (model == null) return;
            if (ObjText != null) ObjText.text = model.ObjectiveText;
            if (TurnNum != null) TurnNum.text = model.Turn.ToString("00");
        }
    }
}
