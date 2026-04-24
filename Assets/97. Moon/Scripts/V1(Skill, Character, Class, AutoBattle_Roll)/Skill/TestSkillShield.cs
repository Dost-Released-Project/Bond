using UnityEngine;

public class TestSkillBuff : SkillBase
{
    // 인스펙터에서 설정할 스킬 ID (팀원의 SkillData ID와 일치해야 함)
    [SerializeField] private string _targetSkillId; 
    public string TargetSkillId => _targetSkillId;

    public SkillType Type => _skillData != null ? _skillData.Type : SkillType.OFFENSIVE;

    public override void UseSkill()
    {
        if (_skillData == null) return;
        Debug.Log($"[공격] {_skillData.DisplayName} 발사! : {_skillData.Description}");
    }
}
