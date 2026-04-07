using UnityEngine;

public class AutoBattle_Atk : AutoBattle
{
    // 딜러의 자동 전투 판단 로직, 플레이어블일 경우 로직 실행 X
    // 판단 1. 딜러 스킬 사용 가능 여부
    // 판단 2. 1번이 불가하거나 쿨타임일 경우 적 체력 확인.
    // 판단 3. 2번 체력이 낮은 적을 우선 공격. 체력이 모두 동일하다면 위협적인 적 공격
    public override void BattleAction(SkillBase skill)
    {
        if(isPlayable) return; // 자동 전투 여부
        
        skill.UseSkill(); // 스킬 사용 로직
    }
    
    public AutoBattle_Atk() { } // 기본 생성자

    public AutoBattle_Atk(string str) // 테스트용 생성자
    {
        Debug.Log($"{str}의 역할군은 딜러입니다.");
    }
}
