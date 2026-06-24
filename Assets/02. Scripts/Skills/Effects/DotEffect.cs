using System.Collections.Generic;
using Buffs;
using PipeLine;
using UnityEngine;

namespace Skills.Effects
{
    public class DotEffect : ISkillEffect
    {
        private SkillEffectType _type;
        private float _value;

        public DotEffect() : this(SkillEffectType.도트, 0f) {}
        public DotEffect(SkillEffectType type, float value)
        {
            _type = type;
            _value = value;
        }

        public void Execute(BattleContext context, SkillBase skill)
        {
            if (context.target == null || context.target.IsDead) return;

            // 스킬 데이터에 기재된 효과 타입을 탐색하여 데미지/회복 판정
            bool isHeal = false;
            bool isDamage = false;

            if (skill.Data.SkillEffectTypes != null)
            {
                foreach (var effect in skill.Data.SkillEffectTypes)
                {
                    if (effect == SkillEffectType.체력_회복) isHeal = true;
                    if (effect == SkillEffectType.체력_감소) isDamage = true;
                }
            }

            // 효과 타입으로 판정되지 않았다면 스킬의 카테고리로 판정 (SUPPORT 면 회복, 그 외 SPELL/OFFENSIVE 면 피해)
            if (!isHeal && !isDamage)
            {
                if (skill.Data.Type == SkillType.SUPPORT) isHeal = true;
                else isDamage = true;
            }

            // 계산 완료된 최종값 context.value를 기반으로 턴당 HP 변화량 설정
            float hpChangePerTurn = context.value;
            if (isDamage)
            {
                hpChangePerTurn = -Mathf.Abs(hpChangePerTurn);
            }
            else if (isHeal)
            {
                hpChangePerTurn = Mathf.Abs(hpChangePerTurn);
            }

            // 지속시간 설정 (기본 3턴)
            int duration = skill.Data.Duration > 0 ? skill.Data.Duration : 3;

            // 버프 생성 (도트는 스탯 모디파이어가 필요 없으므로 빈 리스트)
            var buff = new ActiveBuff(
                skill.Data.Id + "_Dot",
                new List<StatModifier>(),
                duration,
                hpChangePerTurn
            );

            // 대상 캐릭터에게 버프 적용
            context.target.ApplyBuff(buff);

            Debug.Log($"[{skill.Data.DisplayName}] {context.target.Name}에게 도트 효과 부여 완료 (턴당 수치: {hpChangePerTurn}, 지속: {duration}턴)");
        }
    }
}
