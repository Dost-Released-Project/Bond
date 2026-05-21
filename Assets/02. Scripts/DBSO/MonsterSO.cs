using UnityEngine;

/// <summary>
/// [D] Pure Data: 캐릭터 정적 데이터를 담는 SO.
/// 1차 구현 범위: Id, Name, RoleType, Level, STR, AGI, INT 7개 필드만 저장.
/// Profession, Skill, Trait 등 참조형은 추후 추가 예정.
/// </summary>
public class MonsterSO : BaseSO
{
    [Header("기본 정보")]
    public RoleType RoleType;
    public int Level;
    public string ImageAddress;

    [Header("기본 스탯")]
    public int STR;
    public int AGI;
    public int INT;

    [Header("스킬")]
    public string[] SkillIds;

    /// <summary>
    /// 파서에서 호출하는 데이터 초기화 메서드.
    /// base.Initialize()로 id, displayName을 설정한다.
    /// </summary>
    public void SetData(
        string id,
        string displayName,
        RoleType roleType,
        int level,
        int str,
        int agi,
        int intel,
        string imageAddress,
        string[] skillIds)
    {
        base.Initialize(id, displayName, "");
        this.RoleType     = roleType;
        this.Level        = level;
        this.STR          = str;
        this.AGI          = agi;
        this.INT          = intel;
        this.ImageAddress = imageAddress;
        this.SkillIds     = skillIds;
    }
}
