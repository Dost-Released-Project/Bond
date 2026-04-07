using UnityEngine;

public class AutoBattle_Sup : AutoBattle
{
    // 서포터의 자동 전투 판단 로직, 플레이어블일 경우 로직 실행 X
    // 판단 1. 서포터 스킬 사용 가능 여부
    // 판단 2. 1번이 불가하거나 쿨타임일 경우 파티원 체력 확인
    // 판단 3. 2번에 체력이 적은 파티원이 있다면 치유.
    // 판단 4. 파티원의 체력이 괜찮다면 일반 공격.
    public override void BattleAction(SkillBase skill)
    {
        if(isPlayable) return; // 자동 전투 여부
        
        skill.UseSkill(); // 스킬 사용 로직
    }
    
    public AutoBattle_Sup() { } // 기본 생성자
    
    public AutoBattle_Sup(string str) // 테스트용 생성자
    {
        Debug.Log($"{str}의 역할군은 서포터입니다.");
    }
}
