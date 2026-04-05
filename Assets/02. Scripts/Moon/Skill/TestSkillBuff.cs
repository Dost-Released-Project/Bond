using UnityEngine;

public class TestSkillBuff : SkillBase
{
    private float cooldown;
    public override void UseSkill()
    {
        cooldown -= 1;
        if (cooldown <= 0)
        {
            Debug.Log("굉장히 엄청난 기저귀 버프");
            if(SkillCooldown != 0)
                cooldown = SkillCooldown;
        }
    }
}
