using System;
using System.Collections.Generic;
using PipeLine;
using Skills.Effects;
using UnityEngine;

[Serializable]
public class CompositeSkill : SkillBase
{
    private List<ISkillEffect> _effects = new List<ISkillEffect>();

    public CompositeSkill() { }

    public CompositeSkill(SkillData skillData)
    {
        _skillData = skillData;
        InitEffects();
    }

    // 데이터가 바인딩 된 후 초기화 (예: BaseSkill.Init 호출 직후)
    public void InitEffects()
    {
        _effects.Clear();
        if (_skillData == null || _skillData.SkillEffectTypes == null) return;

        foreach (var type in _skillData.SkillEffectTypes)
        {
            switch (type)
            {
                case SkillEffectType.HpChange:
                    _effects.Add(new HpChangeEffect());
                    break;
                case SkillEffectType.Buff:
                    _effects.Add(new BuffEffect());
                    break;
                default:
                    Debug.LogWarning($"[CompositeSkill] 정의되지 않은 SkillEffectType: {type}");
                    break;
            }
        }
    }

    public override void UseSkill(BattleContext context)
    {
        foreach (var effect in _effects)
        {
            effect.Execute(context, this);
        }
    }

    public override string ToString()
    {
        return _skillData != null ? _skillData.DisplayName : "Unknown CompositeSkill";
    }
}