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

            // 적용 전 스탯 획득
            float beforeValue = GetStatValue(context.target, statType);

            // 대상 캐릭터에게 버프 적용
            context.target.ApplyBuff(buff);
            context.target.CalcStat();

            // 적용 후 스탯 획득
            float afterValue = GetStatValue(context.target, statType);

            Debug.Log($"[{skill.Data.DisplayName}] {context.target.Name}에게 버프 적용 완료 (스탯: {statType}, 수치: {finalAmount}, Before: {beforeValue} -> After: {afterValue}, 지속: {duration}턴)");
        }

        private float GetStatValue(BaseCharacter character, StatType statType)
        {
            if (character == null || character.Stat == null) return 0f;

            return statType switch
            {
                StatType.STR => character.Stat.STR,
                StatType.AGI => character.Stat.AGI,
                StatType.INT => character.Stat.INT,
                StatType.MaxHP => character.Stat.max_Hp,
                StatType.Atk => character.Stat.atk,
                StatType.Def => character.Stat.def,
                StatType.Speed => character.Stat.speed,
                StatType.Cri => character.Stat.crt,
                StatType.Acc => character.Stat.acc,
                StatType.Eva => character.Stat.eva,
                StatType.ReactionCtrl => character.Stat.Reaction_Ctrl,
                StatType.SpAtk => character.Stat.Sp_Atk,
                StatType.DamageMultiplier => character.StatController.ApplyModifiers(StatType.DamageMultiplier, 1f),
                StatType.DamageReduction => character.StatController.ApplyModifiers(StatType.DamageReduction, 1f),
                StatType.HealEfficiency => character.StatController.ApplyModifiers(StatType.HealEfficiency, 1f),
                StatType.StressResistance => character.StatController.ApplyModifiers(StatType.StressResistance, 1f),
                _ => 0f
            };
        }
    }
}