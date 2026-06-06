using UnityEngine;
using UnityEngine.UI;
using VContainer;

namespace BattleSystem
{
    public class BattleRetreatView : MonoBehaviour
    {
        [SerializeField] private Button m_retreatButton;
        
        private BattleRetreatController m_retreatController;

        [Inject]
        public void Construct(BattleRetreatController retreatController)
        {
            m_retreatController = retreatController;
        }

        private void Start()
        {
            if (m_retreatButton != null)
            {
                m_retreatButton.onClick.AddListener(OnRetreatButtonClicked);
            }
            else
            {
                Debug.LogWarning("[BattleRetreatView] Retreat Button is not assigned in the inspector.");
            }
        }

        private void OnDestroy()
        {
            if (m_retreatButton != null)
            {
                m_retreatButton.onClick.RemoveListener(OnRetreatButtonClicked);
            }
        }

        private void OnRetreatButtonClicked()
        {
            if (m_retreatButton != null)
            {
                m_retreatButton.interactable = false; // 중복 클릭 차단
            }

            if (m_retreatController != null)
            {
                m_retreatController.ShowRetreatConfirm();
            }
            else
            {
                Debug.LogError("[BattleRetreatView] BattleRetreatController is not injected.");
            }
        }
    }
}