using System.Collections.Generic;
using Bond.Expedition;
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
    public string IdleImageId;
    public string BattleImageId;

    [Header("스탯")]
    public int MaxHP;
    public int Atk;
    public float Def;
    public int Speed;
    public float Cri;
    public float Acc;
    public float Eva;
    public int SpAtk;


    [Header("스킬")]
    public List<string> SkillIds;

    /// <summary>
    /// 파서에서 호출하는 데이터 초기화 메서드.
    /// base.Initialize()로 id, displayName을 설정한다.
    /// </summary>
    public void SetData(
        string id,
        string displayName,
        RoleType roleType,
        int level,
        int maxHP,
        int atk,
        float def,
        int speed,
        float cri,
        float acc,
        float eva,
        int spAtk,
        string imageAddress,
        string idleImageId,
        string battleImageId,
        List<string> skillIds)
    {
        base.Initialize(id, displayName, "");
        this.RoleType = roleType;
        this.Level = level;
        this.MaxHP = maxHP;
        this.Atk = atk;
        this.Def = def;
        this.Speed = speed;
        this.Cri = cri;
        this.Acc = acc;
        this.Eva = eva;
        this.SpAtk = spAtk;
        this.ImageAddress = imageAddress;
        this.IdleImageId = idleImageId;
        this.BattleImageId = battleImageId;
        this.SkillIds = skillIds;
    }
}