using UnityEngine;

public abstract class SkillBase : MonoBehaviour
{
    public float SkillCooldown { get; private set; }

    protected float damage;
    protected int currentLevel;

    // Init 이후 참조할 데이터
    protected SkillData _skillData;

    /// <summary>
    /// 스킬 ID로 SkillData를 바인딩한다.
    /// 호출하는 쪽(이미 ISkillManager를 주입받은 클래스)에서 skillManager를 넘겨준다.
    /// </summary>
    public void Init(string skillId, ISkillManager skillManager)
    {
        _skillData = skillManager.GetSkill(skillId);

        if (_skillData != null)
            SkillCooldown = _skillData.CoolTime;
    }

    public abstract void UseSkill();
}
