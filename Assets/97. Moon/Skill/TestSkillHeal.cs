using UnityEngine;

public class TestSkillHeal : SkillBase
{
    private float cooldown;
    public override void UseSkill()
    {
        Debug.Log("힐 스킬 사용");
    }
}
