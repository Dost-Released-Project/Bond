using System.Collections.Generic;
using UnityEngine;
using VContainer;
using VContainer.Unity;

namespace _02._Scripts.BattleSystem_KWT
{
    public class BattleFlowManager : IStartable
    {
        [Inject]
        private readonly BattleManager battleManager;

        [Inject] 
        private readonly TM tm;
        
        private List<GameObject> m_Units;
        private int m_CurrentActCharacter;

        public void Start()
        {
            m_Units = new List<GameObject>();
            // 플레이어 캐릭터 4명 목록 받아옴
            m_CurrentActCharacter = 0;
        }

        public void StartBattle()
        {
            // 적 캐릭터 목록 받아옴
            // m_Units = TurnManager.Turn(m_Units);
        }
        private void NextUnit()
        {
             ExecuteUnitAction(m_CurrentActCharacter);
             m_CurrentActCharacter++;
        }

        private void ExecuteUnitAction(int unitId)
        {
            Debug.Log($"<color=yellow>[BattleFlowManager]</color> 유닛({unitId}) 행동 시작");
            Debug.Log("<color=yellow>[BattleFlowManager]</color> 다음 턴 대기 중...");
        }
    }
}
