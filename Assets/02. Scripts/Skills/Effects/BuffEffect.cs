using System.Collections.Generic;
using Buffs;
using PipeLine;
using UnityEngine;

namespace Skills.Effects
{
    public class BuffEffect : ISkillEffect
    {
        private SkillEffectType _type;
        private float _value;

        public BuffEffect() : this(SkillEffectType.Buff, 0f) {}
        public BuffEffect(SkillEffectType type, float value)
        {
            _type = type;
            _value = value;
        }

        public void Execute(BattleContext context, SkillBase skill)
        {
            if (context.target == null || context.target.IsDead) return;

            // SkillEffectType을 StatType으로 매핑
            StatType statType = _type switch
            {
                SkillEffectType.버프_스탯_힘 => StatType.STR,
                SkillEffectType.버프_스탯_민첩 => StatType.AGI,
                SkillEffectType.버프_스탯_지능 => StatType.INT,
                _ => StatType.Atk // 기본 Buff 또는 매핑되지 않은 경우 공격력 버프로 처리
            };

            // 버프 수치 결정
            float finalAmount = context.value;

            // 지속시간 설정 (스킬 데이터에 기재된 Duration 사용, 없으면 기본 3턴)
            int duration = skill.Data.Duration > 0 ? skill.Data.Duration : 3;

            // 모디파이어 생성
            var modifier = new StatModifier
            {
                id = skill.Data.Id,
                name = skill.Data.DisplayName,
                type = statType,
                mode = ModifierMode.Flat,
                value = finalAmount
            };

            // 활성 버프 생성
            var buff = new ActiveBuff(
                skill.Data.Id,
                new List<StatModifier> { modifier },
                duration
            );

            // 대상 캐릭터에게 버프 적용
            context.target.ApplyBuff(buff);

            Debug.Log($"[{skill.Data.DisplayName}] {context.target.Name}에게 버프 적용 완료 (스탯: {statType}, 수치: {finalAmount}, 지속: {duration}턴)");
        }
    }
}