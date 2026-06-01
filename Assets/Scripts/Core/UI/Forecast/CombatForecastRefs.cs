using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace ProjectAstra.Core.UI.Forecast
{
    // Ref-holder on the CombatForecast prefab root. Matches the two-panel layout
    // (Left = attacker, Right = defender): each side has a name, portrait, equipped
    // weapon (name + icon), the Atk/Hit/Crit values, and an HP number.
    // CombatForecastUI fills these every cursor move from the computed forecast.
    public class CombatForecastRefs : MonoBehaviour
    {
        [Serializable]
        public class UnitSide
        {
            public TextMeshProUGUI unitName;
            public Image portrait;
            public TextMeshProUGUI weaponName;
            public Image weaponIcon;
            public TextMeshProUGUI atkValue;   // attacker/defender damage per hit
            public TextMeshProUGUI hitValue;   // "%"
            public TextMeshProUGUI critValue;  // "%"
            public TextMeshProUGUI hpValue;    // current HP
        }

        [Header("Unit sides")]
        public UnitSide left  = new UnitSide();
        public UnitSide right = new UnitSide();

        [Header("Weapon-icon sigils (by weapon type)")]
        public Sprite sigilSword;
        public Sprite sigilLance;
        public Sprite sigilAxe;
        public Sprite sigilBow;
        public Sprite sigilStaff;
        public Sprite sigilAnima;
        public Sprite sigilLight;
        public Sprite sigilDark;
    }
}
