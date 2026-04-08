using UnityEngine;

namespace ProjectAstra.Core
{
    [CreateAssetMenu(menuName = "Project Astra/Units/Unit Definition")]
    public class UnitDefinition : ScriptableObject
    {
        [Header("Identity")]
        [SerializeField] private string _unitName;
        [SerializeField] private string _unitId;

        [Header("Class")]
        [SerializeField] private ClassDefinition _defaultClass;

        [Header("Base Stats (Level 1)")]
        [SerializeField] private StatArray _baseStats;
        [SerializeField] private int _baseLevel = 1;

        [Header("Growth Rates (0-100)")]
        [SerializeField] private StatArray _personalGrowths;

        public string UnitName => _unitName;
        public string UnitId => _unitId;
        public ClassDefinition DefaultClass => _defaultClass;
        public StatArray BaseStats => _baseStats;
        public int BaseLevel => _baseLevel;
        public StatArray PersonalGrowths => _personalGrowths;
    }
}
