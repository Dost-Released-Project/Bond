
using UnityEngine;

public class SampleSkill : SkillBase
{
    public SampleSkill()
    {
        if (_skillData == null)
            _skillData = ScriptableObject.CreateInstance<SkillData>();
        _skillData.name = $"{RandomUtil.GetRandomAlphabetOrDigit()}{RandomUtil.GetRandomAlphabetOrDigit()}";
    }
    
    public override void UseSkill()
    {
    }
    
    public override string ToString()
    {
        return _skillData.name;
    }
}