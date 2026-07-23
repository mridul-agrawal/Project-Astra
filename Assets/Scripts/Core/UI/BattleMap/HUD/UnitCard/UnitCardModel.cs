using Codice.Client.BaseCommands;
using ProjectAstra.Core.Units;
using UnityEngine;
using static UnityEngine.UI.CanvasScaler;

namespace ProjectAstra.Core.UI.BattleMap.HUD
{
    public sealed class UnitCardModel
    {
        public string UnitName;
        public Sprite unitCardPortriat;
        public int UnitLevel;
        public int UnitExp;
        public int CurrentHP;
        public int MaxHP;
        public float HpFraction => MaxHP > 0 ? (float)CurrentHP / MaxHP : 0f;
        public Faction UnitFaction;
        public HudCorner Corner;
    }
}
