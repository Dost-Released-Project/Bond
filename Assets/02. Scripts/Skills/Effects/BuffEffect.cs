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

            // TODO: 버프/상태이상 적용 로직 구현
            Debug.Log($"[{skill.Data.DisplayName}] {context.target.Name}에게 버프 적용 (타입: {_type}, 수치: {_value}) (BuffEffect - 구현 대기)");
        }
    }
}