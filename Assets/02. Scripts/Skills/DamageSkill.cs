using System;

[Serializable]
public class HPChangeSkill : SkillBase
{
    public HPChangeSkill() { }
    
    public HPChangeSkill(SkillData skillData)
    {
        _skillData = skillData;
    }
    
    public override void UseSkill()
    {
        
    }
    
    public override string ToString()
    {
        return _skillData.DisplayName;
    }
}
