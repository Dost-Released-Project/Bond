using System;
using System.Collections.Generic;
using System.Linq;
using PipeLine;
using UnityEngine;

namespace Reactions
{
    public enum E_ObserveFilter
    {
        Self,
        Ally,
        Enemy,
        Specific
    }

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

    public class HpBelowCondition : ICondition
    {
        public float Threshold;

        public bool IsMet(BaseCharacter subject, BattleContext context)
        {
            return context.targets.Contains(subject) && subject.HpRatio <= Threshold;
        }
    }
    
    public class KillCondition : ICondition
    {
        public bool IsMet(BaseCharacter subject, BattleContext context)
        {
            return context.caster == subject && context.targets.Any(c => c.IsDead);
        }
    }

    public class TargetingCondition : ICondition
    {
        public bool IsMet(BaseCharacter subject, BattleContext context)
        {
            return context.targets.Contains(subject);
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