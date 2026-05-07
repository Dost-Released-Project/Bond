using System;
using BattleSystem.Interface;
using PipeLine;
using Cysharp.Threading.Tasks;
using juno_Test;
using UnityEngine;
using UnityEngine.InputSystem;
using VContainer;
using VContainer.Unity;

namespace BattleStage
{
    public class BattleFlowManager : MonoBehaviour, IBattleFlowManager
    {
        public event Action<BaseCharacter[], BaseCharacter[]> OnBattle;
        
        private BaseCharacter[] m_playerUnits;
        private BaseCharacter[] m_enemyUnits;
        
        // 이후에 매개변수가 정착지 쪽에서 만든 파티 세팅으로 바뀜
        public void PartySetting(BaseCharacter[] playerUnits)
        {
            m_playerUnits =  playerUnits;
        }

        public void EnemySetting(BaseCharacter[] enemyUnits)
        {
            m_enemyUnits =  enemyUnits;
        }
        
        public void BattleSwitch()
        {
            // 구독하고 있는 매니저들 한테 전투 시작 신호(플레이어 파티, 적 파티)
            OnBattle?.Invoke(m_playerUnits, m_enemyUnits);
        }
        
        
        private void Update()
        {
            if (Keyboard.current != null && Keyboard.current.digit1Key.wasPressedThisFrame)
            {
                Debug.Log("1번 키 눌림");
                BattleSwitch();
            }

            if (Keyboard.current != null && Keyboard.current.digit2Key.wasPressedThisFrame)
            {
                Debug.Log("2번 키 눌림");
            }
        }
    }
}
