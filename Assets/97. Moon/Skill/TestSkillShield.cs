using UnityEngine;

public class TestSkillShield : SkillBase
{
    private float cooldown;
    public override void UseSkill()
    {
        cooldown -= 1;
        if (cooldown <= 0)
        {
            Debug.Log("그 어떠한 공격도 막지 못하는 기적의 쉴드");
            if(SkillCooldown != 0)
                cooldown = SkillCooldown;
        }
    }
}
