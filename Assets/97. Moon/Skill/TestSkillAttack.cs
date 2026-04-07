using UnityEngine;

public class TestSkillAttack : SkillBase
{
    private float cooldown;
    public override void UseSkill()
    {
        Debug.Log("공격 스킬 사용");
    }
}
