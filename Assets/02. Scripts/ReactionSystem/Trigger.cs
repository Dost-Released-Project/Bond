using System;
using PipeLine;
using UnityEngine;

namespace Reactions
{
    public enum E_ObserveFilter
    {
        Self,
        Ally,
        Enemy,
        Specific
    }
    
    public interface ITrigger
    {
        bool CheckCondition(BattleContext context);
    }

    [Serializable]
    public class Trigger : ITrigger
    {
        public E_ObserveFilter Filter;
        public BaseCharacter Subject;
        public ICondition Condition;
        
        public bool CheckCondition(BattleContext context)
        {
            return Condition.IsMet(Subject, context);
        }
    }
    
    // public interface ITrigger
    // {
    //     bool CheckCondition(BattleContext args);
    //     bool IsCompatibleWith(SkillType skillType);
    // }
    //
    // [System.Serializable]
    // public class Trigger : ITrigger
    // {
    //     // 이건 딱히 내가 생각해서 적은거 아니고 그냥 기획서 복붙한거임. 
    //     public int Id;             // 트리거 자체의 고유 식별 번호
    //     public int Category;       // 로직 그룹핑 및 UI 필터링을 위한 분류 체계
    //     public string TriggerKey;  // 코드상에서 이벤트 리스너가 감시할 고유 문자열 식별자
    //     public string Description; // 해당 트리거가 발생하는 구체적인 상황에 대한 기획적 설명
    //     public float ValueParam;   // 스탯 기반 트리거 등에서 사용하는 가변 수치값 (예: HP N% 미만)
    //
    //     public bool CheckCondition(BattleContext args)
    //     {
    //         return true;
    //     }
    //
    //     public bool IsCompatibleWith(SkillType skillType)
    //     {
    //         if (string.IsNullOrEmpty(TriggerKey)) return true;
    //         if (TriggerKey.StartsWith("TRG_OFF"))
    //             return skillType == SkillType.OFFENSIVE || skillType == SkillType.SPELL;
    //         if (TriggerKey.StartsWith("TRG_DEF"))
    //             return skillType == SkillType.DEFENSIVE;
    //         if (TriggerKey.StartsWith("TRG_SIT"))
    //             return skillType == SkillType.SUPPORT;
    //         return true; // TRG_STA_* 등: 제한 없음
    //     }
    // }
    
    // 공격 기반 (OFFENSIVE) TRG_OFF_CRIT, TRG_OFF_KILL	치명타 적중, 적 처치, 특정 속성 사용 등
    // 방어/피격 기반 (DEFENSIVE) TRG_DEF_TG_IN	피격 예고, 아군 피격, 회피 성공, 상태이상 발생 등
    // 전장 상황 기반 (SITUATIONAL) TRG_SIT_MISS	공격 빗나감, 적 스킬 캐스팅, 구역 진입/이탈 등
    // 스탯 기반 (STAT_BASED) TRG_STA_HP_L	개별/파티 평균 체력 저하, 자원(MP) 고갈 등
}