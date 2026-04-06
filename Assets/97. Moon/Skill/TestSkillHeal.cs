using UnityEngine;

public class TestSkillHeal : SkillBase
{
    private float cooldown;
    public override void UseSkill()
    {
        cooldown -= 1;
        if (cooldown <= 0)
        {
            Debug.Log("둘이 먹다 하나가 죽어도 모르는 기적의 힐");
            if(SkillCooldown != 0)
                cooldown = SkillCooldown;
        }
    }
}
