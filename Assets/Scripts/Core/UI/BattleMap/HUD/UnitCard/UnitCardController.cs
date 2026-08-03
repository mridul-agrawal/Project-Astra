using ProjectAstra.Core.Cursor;
using ProjectAstra.Core.Events;
using ProjectAstra.Core.Turn;
using ProjectAstra.Core.Units;
using UnityEngine;

namespace ProjectAstra.Core.UI.BattleMap.HUD
{
    public sealed class UnitCardController
    {
        public UnitCardView unitCardView;
        public UnitCardModel unitCardModel;

        public UnitCardController(UnitCardView view)
        {
            unitCardView = view;
            unitCardModel = new UnitCardModel();
        }

        public void HandleCursorMoved(Vector2Int pos, HudCorner corner) => UpdateUnitCardView(pos, corner);

        public void HandlePhaseStarted(BattlePhase phase, Vector2Int pos, HudCorner corner) => UpdateUnitCardView(pos, corner);


        private void UpdateUnitCardView(Vector2Int pos, HudCorner corner)
        {
            if (!IsPlayerPhase)
                return;

            unitCardModel = BuildModel(pos);
            if (unitCardModel != null)
                unitCardModel.Corner = corner;
            if (unitCardView != null)
                unitCardView.Render(unitCardModel);
        }

        private UnitCardModel BuildModel(Vector2Int pos)
        {
            TestUnit unit = TurnManager.Instance.UnitRegistry.GetUnitAt(pos);
            UnitInstance unitInstance = unit?.UnitInstance;

            if (unit == null)
                return null;

            return new UnitCardModel
            {
                UnitName = unitInstance != null ? unitInstance.Definition.name : unit.name,
                unitCardPortriat = unitInstance != null ? unitInstance.Definition.Portrait : null,
                UnitLevel = unitInstance != null ? unitInstance.Level : 0,
                UnitExp = unitInstance != null ? unitInstance.CurrentEXP : 0,
                CurrentHP = unitInstance != null ? unitInstance.CurrentHP : unit.currentHP,
                MaxHP = unitInstance != null ? unitInstance.MaxHP : unit.maxHP,
                UnitFaction = unit.faction
            };
        }

        private bool IsPlayerPhase
        {
            get
            {
                return TurnManager.Instance.CurrentPhase == BattlePhase.PlayerPhase;
            }
        }
    }
}
