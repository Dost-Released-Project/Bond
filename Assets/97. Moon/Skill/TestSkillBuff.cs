using UnityEngine;

public class TestSkillBuff : SkillBase
{
    private float cooldown;
    public override void UseSkill()
    {
        Debug.Log("버프 스킬 사용");
    }
}
