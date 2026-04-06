using UnityEngine;

public class TestSkillAttack : SkillBase
{
    private float cooldown;
    public override void UseSkill()
    {
        cooldown -= 1;
        if (cooldown <= 0)
        {
            Debug.Log("기적의 쉴드만 뚫을 수 있는 기적의 공격");
            if(SkillCooldown != 0)
                cooldown = SkillCooldown;
        }
    }
}
