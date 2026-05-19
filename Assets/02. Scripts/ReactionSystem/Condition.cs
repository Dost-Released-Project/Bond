using System;
using System.Collections.Generic;
using System.Linq;
using PipeLine;
using UnityEngine;

namespace Reactions
{
    public enum E_ContextTarget
    {
        Caster,
        Target
    }
    
    public enum E_ComparisonOperator
    {
        EqualTo,
        LessThan,
        LessOrEqual,
        GreaterThan,
        GreaterOrEqual,
    }
    
    public interface ICondition
    {
        bool IsMet(
            BaseCharacter subject, BattleContext context);
    }

    public class CritCondition : ICondition
    {
        public bool IsMet(BaseCharacter subject, BattleContext context)
        {
            return context.caster == subject && context.isCritical;
        }
    }

    public static class TriggerTargetComparer
    {
        public static IEnumerable<BaseCharacter> Compare(E_ContextTarget target, E_ObserveFilter filter, BaseCharacter observer, BattleContext context, BaseCharacter subject)
        {
            List<BaseCharacter> compareTarget = target switch
            {
                E_ContextTarget.Caster => new() { context.caster },
                E_ContextTarget.Target => context.target != null ? new() { context.target } : new(),
                _ => new List<BaseCharacter>()
            };

            List<BaseCharacter> observeTarget = filter switch
            {
                E_ObserveFilter.Self => new List<BaseCharacter>(){observer,},
                E_ObserveFilter.Specific => new List<BaseCharacter>(){subject},
                _ => new List<BaseCharacter>(){subject}
            };

            return compareTarget.Intersect(observeTarget);
        }
    }

    public class HpBelowCondition
    {
        public float Threshold;

        public bool IsMet(BaseCharacter subject, BattleContext context)
        {
            return context.target == subject && subject.HpRatio <= Threshold;
        }
    }
    
    public class KillCondition : ICondition
    {
        public bool IsMet(BaseCharacter subject, BattleContext context)
        {
            return context.caster == subject && context.target != null && context.target.IsDead;
        }
    }

    public class TargetingCondition : ICondition
    {
        public bool IsMet(BaseCharacter subject, BattleContext context)
        {
            return context.target == subject;
        }
    }
    
    public class EvadeCondition : ICondition
    {
        public bool IsMet(BaseCharacter subject, BattleContext context)
        {
            return context.caster == subject && context.isEvaded;
        }
    }
}