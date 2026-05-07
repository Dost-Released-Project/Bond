using BattleSystem.Interface;
using Bond.Expedition;
using UnityEngine;
using UnityEngine.InputSystem;
using VContainer.Unity;

namespace BattleSystem
{
    public class BattleStageEntry : IBattleStageEntry, IPostStartable, ITickable
    {
        private readonly IBattleFlowManager m_battleFlowManager;
        private readonly ExpeditionPayload m_battlePayload;
        private readonly IFormationManager m_formationManager;

        public BattleStageEntry(IBattleFlowManager expeditionFlowManager,  ExpeditionPayload expeditionPayload
        , IFormationManager formationManager)
        {
            m_battleFlowManager = expeditionFlowManager;
            m_battlePayload = expeditionPayload;
            m_formationManager = formationManager;
        }

        void IPostStartable.PostStart()
        {
            CharacterSetting();
        }

        public void Tick()
        {
            if (Keyboard.current != null && Keyboard.current.digit1Key.wasPressedThisFrame)
            {
                Debug.Log("1번 키 눌림");
                BattleSwitch();
            }
            
            if (Keyboard.current != null && Keyboard.current.digit2Key.wasPressedThisFrame)
            {
                Debug.Log("2번 키 눌림");
                CharacterSetting();
            }
        }

        private void CharacterSetting()
        {
            BaseCharacter[] player = new BaseCharacter[4]; 
            
            int playerCnt = m_battlePayload.Party.Count;
            
            for (int i = 0; i < playerCnt; i++)
            {
                player[i] = m_battlePayload.Party[i];
                Debug.Log($"{i}번째 파티원 {m_battlePayload.Party[i]}");
                m_formationManager.SetCharacterToSlot(player[i],  E_BattleSide.Player, i);
            }
            
            // Root의 StageContext.enemyparty의 CharacterData랑 BaseCharacter결합
            // 아래는 임시 코드
            BaseCharacter[] enemy = new BaseCharacter[4];
            for (int i = 0; i < playerCnt; i++)
            {
                enemy[i] = m_battlePayload.EnemyParty[i];
                Debug.Log($"{i}번째 적 {m_battlePayload.EnemyParty[i]}");
                m_formationManager.SetCharacterToSlot(enemy[i],  E_BattleSide.Enemy, i);
            }
            CharacterRegister(player, enemy);
        }

        private void CharacterRegister(BaseCharacter[] playerCharacter, BaseCharacter[] enemyCharacter)
        {
            // ExpeditionFlowManager에 CharacterSetting에서 결합한 객체를 등록
            m_battleFlowManager.PartySetting(playerCharacter);
            m_battleFlowManager.EnemySetting(enemyCharacter);
            BattleSwitch();
        }

        private void BattleSwitch()
        {
            m_battleFlowManager.BattleSwitch();
        }
    }
}
