using System;
using _03._PipeLine;
using Cysharp.Threading.Tasks;
using juno_Test;
using UnityEngine;
using UnityEngine.InputSystem;
using VContainer;
using VContainer.Unity;

namespace _02._Scripts.BattleSystem
{
    public class ExpeditionFlowManager : MonoBehaviour, IExpeditionFlowManager
    {
        public event Action<BaseCharacter[], BaseCharacter[]> OnBattleStart;
        
        private BaseCharacter[] _PlayerUnits;
        private BaseCharacter[] _EnemyUnits;
        
        // 이후에 매개변수가 정착지 쪽에서 만든 파티 세팅으로 바뀜
        public void PartySetting(BaseCharacter[] playerUnits)
        {
            _PlayerUnits =  playerUnits;
        }
        
        // 매개변수 : 적 파티
        public void StartBattle(BaseCharacter[] enemyUnits)
        {
            _EnemyUnits = enemyUnits;
            // 구독하고 있는 매니저들 한테 전투 시작 신호(플레이어 파티, 적 파티)
            OnBattleStart?.Invoke(_PlayerUnits, _EnemyUnits);
        }
        
        private void Update()
        {
            if (Keyboard.current != null && Keyboard.current.digit1Key.wasPressedThisFrame)
            {
                Debug.Log("1번 키 눌림");
            }

            if (Keyboard.current != null && Keyboard.current.digit2Key.wasPressedThisFrame)
            {
                Debug.Log("2번 키 눌림");
            }
        }
    }
}
