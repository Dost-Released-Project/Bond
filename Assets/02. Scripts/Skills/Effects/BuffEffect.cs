using PipeLine;
using UnityEngine;

namespace Skills.Effects
{
    public class BuffEffect : ISkillEffect
    {
        public void Execute(BattleContext context, SkillBase skill)
        {
            if (context.target == null || context.target.IsDead) return;

            // TODO: 버프/상태이상 적용 로직 구현
            Debug.Log($"[{skill.Data.DisplayName}] {context.target.Name}에게 버프 적용 (BuffEffect - 구현 대기)");
        }
    }
}