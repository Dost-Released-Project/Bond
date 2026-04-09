using System;
using _03._PipeLine;

namespace Reactions
{
    public abstract class Trigger
    {
        // 이건 딱히 내가 생각해서 적은거 아니고 그냥 기획서 복붙한거임. 
        public int Id;             // 트리거 자체의 고유 식별 번호
        public int Category;       // 로직 그룹핑 및 UI 필터링을 위한 분류 체계
        public string TriggerKey;  // 코드상에서 이벤트 리스너가 감시할 고유 문자열 식별자
        public string Description; // 해당 트리거가 발생하는 구체적인 상황에 대한 기획적 설명
        public float ValueParam;   // 스탯 기반 트리거 등에서 사용하는 가변 수치값 (예: HP N% 미만)

        public abstract bool CheckCondition(BattleContext args);
    }
    
    public class Trigger<T> where T : EventArgs
    {
        public bool CheckCondition(EventArgs args)
        {
            return CheckCondition(args as T);
        }

        public bool CheckCondition(T args)
        {
            return true;
        }
    }
    
    // 공격 기반 (OFFENSIVE) TRG_OFF_CRIT, TRG_OFF_KILL	치명타 적중, 적 처치, 특정 속성 사용 등
    // 방어/피격 기반 (DEFENSIVE) TRG_DEF_TG_IN	피격 예고, 아군 피격, 회피 성공, 상태이상 발생 등
    // 전장 상황 기반 (SITUATIONAL) TRG_SIT_MISS	공격 빗나감, 적 스킬 캐스팅, 구역 진입/이탈 등
    // 스탯 기반 (STAT_BASED) TRG_STA_HP_L	개별/파티 평균 체력 저하, 자원(MP) 고갈 등
    
    // 구독형으로 반응하는 애들이 더 많을텐데
    // 옵저버 패턴을 하지 말라고 했던이유는 예측 표시를 해주기 위해
    // 옵저버 패턴이어도 예측 표시는 될것 같고
    // 옵저버 패턴인 애들과 아닌 애들 투트랙으로 돌리면 너무 불편할것 같은데?
}