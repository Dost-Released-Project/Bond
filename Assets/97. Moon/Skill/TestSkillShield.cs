using UnityEngine;

public class TestSkillShield : SkillBase
{
    private float cooldown;
    public override void UseSkill()
    {
        Debug.Log("쉴드 스킬 사용");
    }
}
