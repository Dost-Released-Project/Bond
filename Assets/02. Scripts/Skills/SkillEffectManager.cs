using System.Collections.Generic;
using PipeLine;
using Skills.Effects;
using UnityEngine;

namespace Skills
{
    public class SkillEffectManager
    {
        private readonly Dictionary<SkillEffectType, ISkillEffect> _effects = new Dictionary<SkillEffectType, ISkillEffect>();

        public SkillEffectManager()
        {
            // 효과 타입별 클래스 등록
            _effects[SkillEffectType.HpChange] = new HpChangeEffect();
            _effects[SkillEffectType.체력_감소] = new HpChangeEffect(SkillEffectType.체력_감소, 0f);
            _effects[SkillEffectType.체력_회복] = new HpChangeEffect(SkillEffectType.체력_회복, 0f);

            _effects[SkillEffectType.Buff] = new BuffEffect();
            _effects[SkillEffectType.버프_스탯_힘] = new BuffEffect(SkillEffectType.버프_스탯_힘, 0f);
            _effects[SkillEffectType.버프_스탯_민첩] = new BuffEffect(SkillEffectType.버프_스탯_민첩, 0f);
            _effects[SkillEffectType.버프_스탯_지능] = new BuffEffect(SkillEffectType.버프_스탯_지능, 0f);
            // 새로운 효과가 추가되면 여기에 등록합니다.
        }

        public void ApplyEffects(BattleContext context, SkillBase skill)
        {
            if (skill.Data.SkillEffectTypes == null) return;

            foreach (var effectType in skill.Data.SkillEffectTypes)
            {
                if (_effects.TryGetValue(effectType, out var effect))
                {
                    effect.Execute(context, skill);
                }
                else
                {
                    Debug.LogWarning($"[SkillEffectManager] 미등록 효과 타입: {effectType}");
                }
            }
        }
    }
}
