using System;
using System.Linq;
using BattleSystem.Interface;
using Cysharp.Threading.Tasks;
using UnityEngine;

namespace BattleStage
{
    public class BattleFlowManager : MonoBehaviour, IBattleFlowManager
    {
        public event Action<BaseCharacter[], BaseCharacter[]> OnBattle;
        public event Action<bool> OnBattleEnd;
        
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
        
        public void BattleSwitch()
        {
            // 전투 시작 시 종료 플래그 초기화
            if (m_playerUnits != null && m_enemyUnits != null)
            {
                m_isBattleEnding = false;
            }
            
            // 구독하고 있는 매니저들 한테 전투 신호 토글(플레이어 파티, 적 파티)
            OnBattle?.Invoke(m_playerUnits, m_enemyUnits);
        }

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

        private async UniTask ProcessBattleEndAsync(bool isPlayerWin)
        {
            Debug.Log($"<color=green>[BattleFlowManager] 전투 종료 처리 시작. (플레이어 승리: {isPlayerWin})</color>");
            
            // 1. 진행 중인 턴 루프 및 전투 로직 중지 신호 발송 (토글 오프)
            BattleSwitch();

            // 2. 승리/패배 연출 대기 시간
            await UniTask.Delay(2000);

            // 3. Provider(일지 시스템) 등 외부 구독자에게 전투 종료 및 승패 결과 알림
            if (OnBattleEnd != null)
            {
                OnBattleEnd.Invoke(isPlayerWin);
            }
            else
            {
                // 구독자가 없을 경우 즉시 맵 복귀 (안전 장치)
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
}
