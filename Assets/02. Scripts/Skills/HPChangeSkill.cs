using System;
using UnityEngine;
using PipeLine;

[Serializable]
public class HPChangeSkill : SkillBase
{
    public HPChangeSkill() { }
    
    public HPChangeSkill(SkillData skillData)
    {
        _skillData = skillData;
    }
    
    public override void UseSkill(BattleContext context)
    {
        base.UseSkill(context);
    }
    
    public override string ToString()
    {
        return _skillData.DisplayName;
    }
}
