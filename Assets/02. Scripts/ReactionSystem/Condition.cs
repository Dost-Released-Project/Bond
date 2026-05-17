using System;
using System.Collections.Generic;
using System.Linq;
using PipeLine;
using UnityEngine;

namespace Reactions
{
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
        bool IsMet(BaseCharacter subject, BattleContext context);
        ICondition Copy();
        
        // bool IsMet(E_CompareFilter coFilter, E_ObserveFilter obFilter, BaseCharacter observer, BattleContext context, BaseCharacter subject);
        // IEnumerable<BaseCharacter> GetTargets(E_CompareFilter coFilter, E_ObserveFilter obFilter, BaseCharacter observer, BattleContext context, BaseCharacter subject);
    }

    public static class TriggerTargetComparer
    {
        public static IEnumerable<BaseCharacter> Compare(E_CompareFilter coFilter, E_ObserveFilter obFilter, BaseCharacter observer, BattleContext context, BaseCharacter subject)
        {
            List<BaseCharacter> compareTarget = coFilter switch
            {
                E_CompareFilter.Caster => new() { context.caster },
                E_CompareFilter.Target => context.targets,
                _ => new List<BaseCharacter>()
            };

            List<BaseCharacter> observeTarget = obFilter switch
            {
                E_ObserveFilter.Self => new List<BaseCharacter>(){observer,},
                E_ObserveFilter.Specific => new List<BaseCharacter>(){subject},
                _ => new List<BaseCharacter>(){subject}
            };

            return compareTarget.Intersect(observeTarget);
        }

        public static bool IsThere(E_CompareFilter target, E_ObserveFilter filter, BaseCharacter observer, BattleContext context, BaseCharacter subject)
            => Compare(target, filter, observer, context, subject).Any();
    }

    [Serializable]
    public class HpBelowCondition : ICondition
    {
        public float Threshold;
        
        public HpBelowCondition() { }

        public HpBelowCondition(float threshold)
        {
            Threshold = threshold;
        }

        public bool IsMet(BaseCharacter subject, BattleContext context)
        {
            return context.targets.Contains(subject) && subject.HpRatio <= Threshold;
        }

        public ICondition Copy()
        {
            return new HpBelowCondition(Threshold);
        }
    }

    public class CritCondition : ICondition
    {
        public bool IsMet(BaseCharacter subject, BattleContext context)
        {
            return context.caster == subject && context.isCritical;
        }

        public ICondition Copy()
        {
            return new CritCondition();
        }
    }
    
    public class EvadeCondition : ICondition
    {
        public bool IsMet(BaseCharacter subject, BattleContext context)
        {
            return context.targets.Contains(subject) && context.isEvaded;
        }

        public ICondition Copy()
        {
            return new EvadeCondition();
        }
    }

    public class KillCondition : ICondition
    {
        public bool IsMet(BaseCharacter subject, BattleContext context)
        {
            return context.caster == subject && context.targets.Any(c => c.IsDead);
        }

        public ICondition Copy()
        {
            return new KillCondition();
        }
    }

    public class HitCondition : ICondition
    {
        public bool IsMet(BaseCharacter subject, BattleContext context)
        {
            return context.targets.Contains(subject) && context.isEvaded == false;
        }

        public ICondition Copy()
        {
            return new HitCondition();
        }
    }
    
    public class TargetCondition : ICondition
    {
        public bool IsMet(BaseCharacter subject, BattleContext context)
        {
            return context.targets.Contains(subject);
        }

        public ICondition Copy()
        {
            return new TargetCondition();
        }
    }
}