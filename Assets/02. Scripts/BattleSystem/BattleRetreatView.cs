using System;
using UnityEngine;
using UnityEngine.UI;
using VContainer;
using Bond.WT.Journal;

namespace BattleSystem
{
    public class BattleRetreatView : MonoBehaviour
    {
        [SerializeField] private Button m_retreatButton;

        private BattleRetreatController m_retreatController;
        private JournalModel m_journalModel;

        // ObservableValue가 IObserver<T>만 받으므로 래핑용 (JournalBinder와 동일한 패턴)
        private class ObserverWrapper<T> : IObserver<T>
        {
            public Action<T> EventHandler { get; set; }
        }

        private readonly ObserverWrapper<bool> m_journalCompleteObserver = new ObserverWrapper<bool>();

        /// <summary>
        /// VContainer 의존성 주입. 퇴각 컨트롤러와 일지창 상태 모델을 주입받는다.
        /// </summary>
        [Inject]
        public void Construct(BattleRetreatController retreatController, JournalModel journalModel)
        {
            m_retreatController = retreatController;
            m_journalModel = journalModel;
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

            // 확인 팝업(일지창)이 닫히면 비활성화했던 퇴각 버튼을 다시 켜기 위해 구독
            m_journalCompleteObserver.EventHandler = OnJournalCompleteChanged;
            m_journalModel?.IsJournalComplete.Subscribe(m_journalCompleteObserver);
        }

        private void OnDestroy()
        {
            if (m_retreatButton != null)
            {
                m_retreatButton.onClick.RemoveListener(OnRetreatButtonClicked);
            }

            m_journalModel?.IsJournalComplete.Unsubscribe(m_journalCompleteObserver);
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

        /// <summary>
        /// 일지창 상태 변화 콜백. 일지창이 닫히면(isComplete == true) 중복 클릭 차단으로 꺼둔 퇴각 버튼을 복구한다.
        /// '취소' 선택 시에도 이 콜백으로 버튼이 다시 활성화된다.
        /// </summary>
        private void OnJournalCompleteChanged(bool isComplete)
        {
            if (isComplete && m_retreatButton != null)
            {
                m_retreatButton.interactable = true;
            }
        }
    }
}