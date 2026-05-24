using PipeLine;
using UnityEngine;

namespace Skills.Effects
{
    public class HpChangeEffect : ISkillEffect
    {
        public void Execute(BattleContext context, SkillBase skill)
        {
            if (context.target == null || context.target.IsDead) return;

            int finalAmount = Mathf.RoundToInt(context.value);

            // 스킬 카테고리가 SUPPORT면 회복, 그 외에는 데미지 적용
            if (skill.Data.Type == SkillType.SUPPORT)
            {
                context.target.RecoverHp(finalAmount);
                Debug.Log($"[{skill.Data.DisplayName}] {context.target.Name}에게 {finalAmount} 회복 적용 (HpChangeEffect)");
            }
            else
            {
                context.target.ReduceHP(finalAmount);
                Debug.Log($"[{skill.Data.DisplayName}] {context.target.Name}에게 {finalAmount} 데미지 적용 (HpChangeEffect)");
            }
        }
    }
}