using BattleSystem.Interface;
using Bond.Expedition;
using VContainer.Unity;

namespace BattleSystem
{
    public class BattleStageEntry : IBattleStageEntry, IPostStartable
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

        private void CharacterSetting()
        {
            // Root의 ExpeditionPayLoad.party의 CharacterData랑 BaseCharacter결합
            BaseCharacter[] player = new BaseCharacter[4]; 
            
            int playerCnt = m_battlePayload.Party.Count;
            
            for (int i = 0; i < playerCnt; i++)
            {
                player[i] = new  BaseCharacter(m_battlePayload.Party[i]);
                m_formationManager.SetCharacterToSlot(player[i],  E_BattleSide.Player, i);
            }
            
            // Root의 StageContext.enemyparty의 CharacterData랑 BaseCharacter결합
            // 아래는 임시 코드
            BaseCharacter[] enemy = new BaseCharacter[4];
            CharacterRegister(player, enemy);
        }

        private void CharacterRegister(BaseCharacter[] playerCharacter, BaseCharacter[] enemyCharacter)
        {
            // ExpeditionFlowManager에 CharacterSetting에서 결합한 객체를 등록
            m_battleFlowManager.PartySetting(playerCharacter);
            m_battleFlowManager.EnemySetting(enemyCharacter);
            StartBattle();
        }

        private void StartBattle()
        {
            m_battleFlowManager.StartBattle();
        }
    }
}
