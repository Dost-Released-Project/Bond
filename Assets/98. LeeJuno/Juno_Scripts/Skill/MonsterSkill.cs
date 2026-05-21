using System;
using UnityEngine;
using BattleSystem;

[Serializable]
public class MonsterSkill : SkillBase
{
    /// <summary>
    /// SkillBase의 Init() 호출 이후, 몬스터 스탯에 기반하여 스킬 위력을 보정한다.
    /// </summary>
    public void ApplyStat(Stat casterStat)
    {
        if (_skillData == null || casterStat == null) return;
        
        float baseValue = _skillData.Value;
        float bonus = 0f;
        
        if (_skillData.Type == SkillType.OFFENSIVE || _skillData.Type == SkillType.DEFENSIVE) 
        {
            bonus = casterStat.STR;
        }
        else if (_skillData.Type == SkillType.SPELL || _skillData.Type == SkillType.SUPPORT) 
        {
            bonus = casterStat.INT;
        }
        
        damage = baseValue + bonus;
    }

    public override void UseSkill()
    {
        // Rule 2 – Blind Logic: 상태 변화(로직)만 처리.
        // 비주얼 연출과 타격 효과 적용의 메인 루프는 BaseCharacter의 onBattleAction을 통해 
        // BattleManager와 Target에 위임되므로, 여기서는 특수한 전처리/후처리 데이터 변경만 담당한다.
        Debug.Log($"[MonsterSkill - Logic] '{Data.DisplayName}'(ID:{Data.Id}) 스킬 효과 발동 대기 (Type: {Data.Type}, Calculated Damage/Value: {damage})");
    }
}
