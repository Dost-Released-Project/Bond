
using UnityEngine;

public class SampleSkill : SkillBase
{
    public SampleSkill()
    {
        if (_skillData == null)
            _skillData = ScriptableObject.CreateInstance<SkillData>();

        string randomName = $"{RandomUtil.GetRandomAlphabetOrDigit()}{RandomUtil.GetRandomAlphabetOrDigit()}";

        // 테스트 연출을 위해 타겟 정보를 포함하여 초기화
        var rawData = new SkillRawData();
        rawData.SkillId = randomName;
        rawData.SkillName = randomName;
        rawData.Target = SkillTarget.Enemy; // 상대 진영을 타겟으로 설정
        rawData.EnemyTargetMask = 1 << UnityEngine.Random.Range(0, 4); // 상대 진영 중 랜덤 1칸

        _skillData.SetData(rawData);
    }

    public SampleSkill(SkillData skillData)
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
