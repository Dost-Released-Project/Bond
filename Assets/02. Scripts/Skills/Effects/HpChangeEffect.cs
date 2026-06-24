using PipeLine;
using UnityEngine;

namespace Skills.Effects
{
    public class HpChangeEffect : ISkillEffect
    {
        private SkillEffectType _type;
        private float _value;

        public HpChangeEffect() : this(SkillEffectType.HpChange, 0f) {}
        public HpChangeEffect(SkillEffectType type, float value)
        {
            _type = type;
            _value = value;
        }

        public void Execute(BattleContext context, SkillBase skill)
        {
            if (context.target == null || context.target.IsDead) return;

            int finalAmount = Mathf.RoundToInt(context.value);

            // 스킬 카테고리가 SUPPORT거나 효과 타입이 체력_회복인 경우 회복 처리
            if (_type == SkillEffectType.체력_회복)
            {
                context.target.RecoverHp(finalAmount, context.isCritical);
                Debug.Log($"[{skill.Data.DisplayName}] {context.target.Name}에게 {finalAmount} 회복 적용 (HpChangeEffect)");
            }
            else
            {
                context.target.ReduceHP(finalAmount, context.isCritical);
                Debug.Log($"[{skill.Data.DisplayName}] {context.target.Name}에게 {finalAmount} 데미지 적용 (HpChangeEffect)");
            }
        }
    }
}