using System;
using System.Linq;
using BattleSystem.Interface;
using Cysharp.Threading.Tasks;
using UnityEngine;
using UnityEngine.InputSystem;

using VContainer;

namespace BattleStage
{
    public class BattleFlowManager : MonoBehaviour, IBattleFlowManager
    {
        public event Action<BaseCharacter[], BaseCharacter[]> OnBattle;
        public event Action<BattleEndStatus> OnBattleEnd;
        
        private Bond.Expedition.ExpeditionPayload _payload;

        [Inject]
        public void Construct(Bond.Expedition.ExpeditionPayload payload)
        {
            _payload = payload;
        }

        private BaseCharacter[] m_playerUnits;
        private BaseCharacter[] m_enemyUnits;
        
        private bool m_isBattleEnding = false;

        public void PartySetting(BaseCharacter[] playerUnits)
        {
            if (m_playerUnits != null)
            {
                foreach (var unit in m_playerUnits)
                {
                    if (unit != null) unit.OnDead -= CheckBattleEnd;
                }
            }
            
            m_playerUnits = playerUnits;
            
            if (m_playerUnits != null)
            {
                foreach (var unit in m_playerUnits)
                {
                    if (unit != null) unit.OnDead += CheckBattleEnd;
                }
            }
        }

        public void EnemySetting(BaseCharacter[] enemyUnits)
        {
            if (m_enemyUnits != null)
            {
                foreach (var unit in m_enemyUnits)
                {
                    if (unit != null) unit.OnDead -= CheckBattleEnd;
                }
            }
            
            m_enemyUnits = enemyUnits;
            
            if (m_enemyUnits != null)
            {
                foreach (var unit in m_enemyUnits)
                {
                    if (unit != null) unit.OnDead += CheckBattleEnd;
                }
            }
        }
        
        private bool m_isBattleActive = false;

        public void BattleSwitch()
        {
            m_isBattleActive = !m_isBattleActive;

            // 전투 시작 시 종료 플래그 초기화
            if (m_isBattleActive)
            {
                m_isBattleEnding = false;
            }
            
            // 구독하고 있는 매니저들 한테 전투 신호 토글(플레이어 파티, 적 파티)
            OnBattle?.Invoke(m_playerUnits, m_enemyUnits);
        }

#if UNITY_EDITOR || DEVELOPMENT_BUILD
        private void Update()
        {
            if (Keyboard.current.spaceKey.wasPressedThisFrame)
            {
                if (m_isBattleEnding) return;
                
                Debug.Log("<color=yellow>[DEBUG] Space 키 입력으로 전투를 스킵합니다.</color>");
                m_isBattleEnding = true;
                ProcessBattleEndAsync(true).Forget();
            }
        }
#endif

        private void CheckBattleEnd(BaseCharacter deadCharacter)
        {
            if (m_isBattleEnding) return;

            bool isPlayerWipedOut = m_playerUnits != null && m_playerUnits.Where(u => u != null).All(u => u.IsDead);
            bool isEnemyWipedOut = m_enemyUnits != null && m_enemyUnits.Where(u => u != null).All(u => u.IsDead);

            if (isPlayerWipedOut)
            {
                m_isBattleEnding = true;
                ProcessBattleEndAsync(false).Forget();
            }
            else if (isEnemyWipedOut)
            {
                m_isBattleEnding = true;
                ProcessBattleEndAsync(true).Forget();
            }
        }

        public void HandleRetreat()
        {
            if (m_isBattleEnding) return;
            
            Debug.Log("<color=yellow>[BattleFlowManager] 전투 퇴각(도주) 요청 수신.</color>");
            m_isBattleEnding = true;
            ProcessBattleEndAsync(false, true).Forget();
        }

        private async UniTask ProcessBattleEndAsync(bool isPlayerWin, bool isRetreat = false)
        {
            Debug.Log($"<color=green>[BattleFlowManager] 전투 종료 처리 시작. (플레이어 승리: {isPlayerWin}, 퇴각 여부: {isRetreat})</color>");
            
            // 승리 보상 가상 계좌(Payload) 적립
            if (isPlayerWin && !isRetreat && _payload != null)
            {
                _payload.AddReward(200, 20, 20);
            }

            // 1. 진행 중인 턴 루프 및 전투 로직 중지 신호 발송 (토글 오프)
            BattleSwitch();

            // 2. 승리/패배/퇴각 연출 대기 시간
            await UniTask.Delay(2000);

            // 3. 전투 결과 상태 결정
            BattleEndStatus status = isRetreat ? BattleEndStatus.Retreat : (isPlayerWin ? BattleEndStatus.Victory : BattleEndStatus.Defeat);

            // 4. 외부 구독자에게 전투 종료 상태 전달
            if (OnBattleEnd != null)
            {
                OnBattleEnd.Invoke(status);
            }
            else
            {
                InvokeStageComplete(isPlayerWin);
            }
        }

        private void InvokeStageComplete(bool isPlayerWin)
        {
            StageResult result = new StageResult
            {
                IsSuccess = isPlayerWin,
                IsGameOver = !isPlayerWin,
                IsBattleTriggered = false,
                RewardIds = new System.Collections.Generic.List<string>()
            };
            StageCompletionChannel.Invoke(result);
        }
    }
}
