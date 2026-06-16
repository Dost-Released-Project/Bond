using System;
using Newtonsoft.Json;
using PipeLine;
using Skills;
using UnityEngine;

[Serializable]
public abstract class SkillBase
{
    public float SkillCooldown { get; private set; }

    [SerializeField] protected float damage;
    [SerializeField] protected int currentLevel;

    // Init 이후 참조할 데이터
    [JsonProperty][SerializeField] protected SkillData _skillData;
    
    // 로직 그룹: 효과 관리자 (정적 인스턴스로 간단히 접근하거나 주입 가능)
    protected static readonly SkillEffectManager _effectManager = new SkillEffectManager();

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

    [JsonIgnore] public SkillData Data => _skillData;

    public virtual void UseSkill(BattleContext context)
    {
        if (_skillData == null) return;
        _effectManager.ApplyEffects(context, this);
    }
}
