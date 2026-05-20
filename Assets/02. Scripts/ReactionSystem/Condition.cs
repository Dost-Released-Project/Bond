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
        string Description { get; }
        
        // bool IsMet(E_CompareFilter coFilter, E_ObserveFilter obFilter, BaseCharacter observer, BattleContext context, BaseCharacter subject);
        // IEnumerable<BaseCharacter> GetTargets(E_CompareFilter coFilter, E_ObserveFilter obFilter, BaseCharacter observer, BattleContext context, BaseCharacter subject);
    }

    public static class TriggerTargetComparer
    {
        public static IEnumerable<BaseCharacter> Compare(E_TargetFilter coFilter, E_ObserveFilter obFilter, BaseCharacter observer, BattleContext context, BaseCharacter subject)
        {
            List<BaseCharacter> compareTarget = coFilter switch
            {
                E_TargetFilter.Caster => new() { context.caster },
                E_TargetFilter.Target => context.target != null ? new() { context.target } : new(),
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

        public static bool IsThere(E_TargetFilter target, E_ObserveFilter filter, BaseCharacter observer, BattleContext context, BaseCharacter subject)
            => Compare(target, filter, observer, context, subject).Any();
    }

    public abstract class ReactionTriggerCondition : ICondition
    {
        public abstract bool IsMet(BaseCharacter subject, BattleContext context);
        public abstract ICondition Copy();
        public abstract string Description { get; }
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
            return context.target == subject && subject.HpRatio <= Threshold && context.runtimeSkill.Data.Type == SkillType.OFFENSIVE;;
        }

        public ICondition Copy()
        {
            return new HpBelowCondition(Threshold);
        }

        public string Description => $"Hp 비율이 {Threshold} 이하인 상태에서 공격 받았을 때";
    }

    [Serializable]
    public class CritCondition : ICondition
    {
        public bool IsMet(BaseCharacter subject, BattleContext context)
        {
            return context.caster == subject && context.isCritical && context.runtimeSkill.Data.Type == SkillType.OFFENSIVE;
        }

        public ICondition Copy()
        {
            return new CritCondition();
        }

        public string Description => $"공격 스킬로 크리를 가했을 때";
    }
    
    [Serializable]
    public class EvadeCondition : ICondition
    {
        public bool IsMet(BaseCharacter subject, BattleContext context)
        {
            return context.target == subject && context.isEvaded && context.runtimeSkill.Data.Type == SkillType.OFFENSIVE;;
        }

        public ICondition Copy()
        {
            return new EvadeCondition();
        }
        public string Description => $"공격을 회피했을 때";
    }

    [Serializable]
    public class KillCondition : ICondition
    {
        public bool IsMet(BaseCharacter subject, BattleContext context)
        {
            return context.caster == subject && context.target.IsDead && context.runtimeSkill.Data.Type == SkillType.OFFENSIVE;;
        }

        public ICondition Copy()
        {
            return new KillCondition();
        }
        public string Description => $"공격 스킬로 죽였을 때";
    }

    [Serializable]
    public class HitCondition : ICondition
    {
        public bool IsMet(BaseCharacter subject, BattleContext context)
        {
            return context.target == subject && context.isEvaded == false && context.runtimeSkill.Data.Type == SkillType.OFFENSIVE;
        }

        public ICondition Copy()
        {
            return new HitCondition();
        }
        public string Description => $"공격을 당했을 때";
    }
    
    [Serializable]
    public class TargetCondition : ICondition
    {
        public bool IsMet(BaseCharacter subject, BattleContext context)
        {
            return context.target == subject;
        }

        public ICondition Copy()
        {
            return new TargetCondition();
        }
        public string Description => $"행동의 대상으로 지정 됐을 때";
    }
}