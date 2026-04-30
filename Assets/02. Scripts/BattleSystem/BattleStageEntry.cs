using _02._Scripts.BattleSystem;
using Bond.Expedition;
using VContainer.Unity;

public class BattleStageEntry : IBattleStageEntry, IStartable
{
    private readonly IBattleFlowManager m_expeditionFlowManager;
    private readonly ExpeditionPayload m_expeditionPayload;

    public BattleStageEntry(IBattleFlowManager expeditionFlowManager,  ExpeditionPayload expeditionPayload)
    {
        m_expeditionFlowManager = expeditionFlowManager;
        m_expeditionPayload = expeditionPayload;
    }

    public void Start()
    {
        CharacterSetting();
    }

    private void CharacterSetting()
    {
        // Root의 ExpeditionPayLoad.party의 CharacterData랑 BaseCharacter결합
        // Root의 StageContext.enemyparty의 CharacterData랑 BaseCharacter결합
        // 아래는 임시 코드
        BaseCharacter[] player = new BaseCharacter[4];
        BaseCharacter[] enemy = new BaseCharacter[4];
        CharacterRegister(player, enemy);
    }

    private void CharacterRegister(BaseCharacter[] playerCharacter, BaseCharacter[] enemyCharacter)
    {
        // ExpeditionFlowManager에 CharacterSetting에서 결합한 객체를 등록
        m_expeditionFlowManager.PartySetting(playerCharacter);
        m_expeditionFlowManager.EnemySetting(enemyCharacter);
        StartBattle();
    }

    private void StartBattle()
    {
        m_expeditionFlowManager.StartBattle();
    }
}
