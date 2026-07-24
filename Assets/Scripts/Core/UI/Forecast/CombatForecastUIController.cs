using UnityEngine;
using ProjectAstra.Core.Audio;
using ProjectAstra.Core.Units;

namespace ProjectAstra.Core.UI.Forecast
{
    public class CombatForecastUIController : MonoBehaviour
    {
        [Header("Screen")]
        [SerializeField] private GameObject panelRoot;

        [Header("Sides")]
        [SerializeField] private CombatForecastSideView leftSide;   // attacker
        [SerializeField] private CombatForecastSideView rightSide;  // defender

        private readonly CombatForecastCalculator calculator = new CombatForecastCalculator();

        public void Show(ForecastKind kind, TestUnit actor, TestUnit target)
        {
            if (actor == null || target == null) 
            { 
                Hide(); 
                return; 
            }

            CombatForecastModel model = calculator.Build(kind, actor, target);
            if (model == null) 
            { 
                Hide(); 
                return; 
            }

            if (leftSide != null)  
                leftSide.Render(model.AttackerSideModel);
            
            if (rightSide != null) 
                rightSide.Render(model.DefenderSideModel);
            
            Activate();
        }

        public void Hide()
        {
            if (panelRoot == null) return;
            bool wasVisible = panelRoot.activeSelf;
            panelRoot.SetActive(false);
            if (wasVisible) AudioManager.Instance?.Play(SoundId.UiPanelClose);
        }

        private void Activate()
        {
            if (panelRoot == null) return;
            bool wasActive = panelRoot.activeSelf;
            panelRoot.SetActive(true);
            panelRoot.transform.SetAsLastSibling();
            if (!wasActive) 
                AudioManager.Instance?.Play(SoundId.UiPanelOpen);
        }
    }
}
