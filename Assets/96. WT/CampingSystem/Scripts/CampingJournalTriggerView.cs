using UnityEngine;
using UnityEngine.UI;
using VContainer;

namespace Bond.WT.Camping
{
    public class CampingJournalTriggerView : MonoBehaviour
    {
        [SerializeField] private Button m_journalButton;
        
        private CampingSystem m_campingSystem;

        [Inject]
        public void Construct(CampingSystem campingSystem)
        {
            m_campingSystem = campingSystem;
        }

        private void Start()
        {
            if (m_journalButton != null)
            {
                m_journalButton.onClick.AddListener(OnJournalButtonClicked);
            }
            else
            {
                Debug.LogWarning("[CampingJournalTriggerView] Journal Button is not assigned.");
            }
        }

        private void OnDestroy()
        {
            if (m_journalButton != null)
            {
                m_journalButton.onClick.RemoveListener(OnJournalButtonClicked);
            }
        }

        private void OnJournalButtonClicked()
        {
            if (m_campingSystem != null)
            {
                // 중복 클릭 방지 (버튼 비활성화)
                m_journalButton.interactable = false;
                
                m_campingSystem.GenerateCampingReport();
                
                // 일지 창이 닫히거나 상태가 변경될 때 다시 버튼을 켤 로직이 추가로 필요하다면,
                // JournalSystem의 OnJournalClosed 이벤트 등을 구독해야 합니다.
                // 일단은 중복 생성 방지를 위해 1회 클릭 후 막아둡니다. (캠프 시스템 특성 상)
            }
            else
            {
                Debug.LogError("[CampingJournalTriggerView] CampingSystem is not injected.");
            }
        }
    }
}