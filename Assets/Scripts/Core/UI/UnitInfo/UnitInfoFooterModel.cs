using UnityEngine;

namespace ProjectAstra.Core.UI.UnitInfo
{
    // Footer description line — the icon/name/description of the currently selected
    // stat row (STATS tab) or gear slot (GEAR tab).
    public sealed class UnitInfoFooterModel
    {
        public Sprite Icon;
        public string Title;
        public string Description;
        public string Detail;           // §7 line two — the longer explanation under the summary
    }
}
