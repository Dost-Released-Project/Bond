using System.Collections.Generic;
using Reactions;

public enum RoleType
{
    None,
    Tanker,   // 활성 트리거: TRG_DEF_TG_IN (피격 시), TRG_SIT_ALLY_CRISIS (아군 위기 시)
    Dealer,   // 활성 트리거: TRG_OFF_KILL (적 처치 시), TRG_OFF_CRIT (치명타 시)
    Supporter // 활성 트리거: TRG_SIT_ALLY_TURN_END (아군 턴 종료 시), TRG_DEF_STATUS (상태이상 시)
}

public partial class BaseCharacterData
{
    /// <summary>
    /// 테스트 용 객체
    /// </summary>
    public static BaseCharacterData Empty => new BaseCharacterData()
    {
        Name = "CharacterData for Test"
    };
    
    public string Id;
    public string ImageAddress;

    public string Name;
    public Profession Profession;
    public int Level = 0;
    public int Insanity = 0; // 스트레스(광기) 지수 0~100, Stress는 STR과 혼동될 수 있어서 명칭 변경
    public RoleType RoleType = RoleType.None;

    public SkillBase[] Skills = new SkillBase[4];
    public Trait[] Traits = new Trait[4];
    public Equipment[] Equips = new Equipment[2];

    public Dictionary<BaseCharacter, int>
        Relation = new Dictionary<BaseCharacter, int>(); // 딕셔너리 선택 이유: 동료는 언제든 교체될 수 있기에 딕셔너리로 관리 및 저장.

    public Reaction[] RoleReactions = new Reaction[2];
    public Reaction[] TraitReactions = new Reaction[4];

    private BaseCharacterData()
    {
        for(int i = 0; i < RoleReactions.Length; i++)
        {
            RoleReactions[i] = new Reaction();
        }
        
        for(int i = 0; i < TraitReactions.Length; i++)
        {
            TraitReactions[i] = new Reaction();
        }
    }
}