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

        for (int i = 0; i < _skillData.SkillEffectTypes.Count; i++)
        {
            var type = _skillData.SkillEffectTypes[i];
            float effectValue = (_skillData.Values != null && i < _skillData.Values.Count) ? _skillData.Values[i] : 0f;

            switch (type)
            {
                case SkillEffectType.HpChange:
                case SkillEffectType.체력_감소:
                case SkillEffectType.체력_회복:
                    _effects.Add(new HpChangeEffect(type, effectValue));
                    break;
                case SkillEffectType.Buff:
                case SkillEffectType.버프_스탯_힘:
                case SkillEffectType.버프_스탯_민첩:
                case SkillEffectType.버프_스탯_지능:
                    _effects.Add(new BuffEffect(type, effectValue));
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