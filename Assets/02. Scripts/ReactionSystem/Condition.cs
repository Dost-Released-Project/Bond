using System;
using System.Collections.Generic;
using System.Linq;
using PipeLine;
using Unity.VisualScripting;
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
        bool IsMet(object args);
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
        public bool IsMet(object args)
        {
            return IsMet((ReactionTriggerConditionArgs)args);
        }

        public abstract bool IsMet(ReactionTriggerConditionArgs args);
        public abstract ReactionTriggerCondition Copy();
        public abstract string Description { get; }
    }

    public class ReactionTriggerConditionArgs
    {
        public BattleContext BattleContext;
        public BaseCharacter Subject;
    }
    
    [Serializable][AddTypeMenu("SubjectIs", -1000)]
    public class SubjectCondition: ReactionTriggerCondition
    {
        public E_TargetFilter Filter = E_TargetFilter.None;
        public override bool IsMet(ReactionTriggerConditionArgs args)
        {
            var context = args.BattleContext;
            var subject = args.Subject;
            return Filter switch
            {
                E_TargetFilter.Caster => context.caster == subject,
                E_TargetFilter.Target => context.target == subject,
                _ => false
            };
        }
        
        public SubjectCondition() { }

        public SubjectCondition(E_TargetFilter filter)
        {
            Filter = filter;
        }

        public override ReactionTriggerCondition Copy()
        {
            return new SubjectCondition() { Filter = Filter };
        }

        public override string Description
        {
            get
            {
                return Filter switch
                {
                    E_TargetFilter.Caster => "관찰 대상이 스킬의 주체일 때",
                    E_TargetFilter.Target => "관찰 대상이 스킬의 대상일 때",
                    _ => ""
                };
            }
        }
    }
    
    [Serializable][AddTypeMenu("SkillTypeIs", -100)]
    public class SkillTypeCondition: ReactionTriggerCondition
    {
        public List<SkillType> Type = new List<SkillType>();
        public override bool IsMet(ReactionTriggerConditionArgs args)
        {
            return Type.Contains(args.BattleContext.runtimeSkill.Data.Type);
        }
        
        public SkillTypeCondition() { }

        public SkillTypeCondition(params SkillType[] type)
        {
            Type.AddRange(type);
        }

        public override ReactionTriggerCondition Copy()
        {
            return new SkillTypeCondition() { Type = Type };
        }

        public override string Description => $"스킬 타입이 {Type}일 때";
    }

    [Serializable]
    public class HpBelowCondition: ReactionTriggerCondition
    {
        public float Threshold;

        public override bool IsMet(ReactionTriggerConditionArgs args)
        {
            return args.Subject.HpRatio <= Threshold;
        }

        public override ReactionTriggerCondition Copy()
        {
            return new HpBelowCondition() { Threshold = Threshold };
        }

        public override string Description => $"관찰 대상의 Hp 비율이 {Threshold} 이하일 때";
    }

    [Serializable]
    public class CritCondition: ReactionTriggerCondition
    {
        public override bool IsMet(ReactionTriggerConditionArgs args)
        {
            return args.BattleContext.isCritical;
        }

        public override ReactionTriggerCondition Copy()
        {
            return new CritCondition();
        }

        public override string Description => $"스킬에 크리티컬이 발생할 때";
    }
    
    [Serializable]
    public class EvadeCondition: ReactionTriggerCondition
    {
        public override bool IsMet(ReactionTriggerConditionArgs args)
        {
            return args.BattleContext.isEvaded;
        }

        public override ReactionTriggerCondition Copy()
        {
            return new EvadeCondition();
        }
        public override string Description => $"스킬에 회피가 발생했을 때";
    }

    [Serializable]
    public class KillCondition: ReactionTriggerCondition
    {
        public override bool IsMet(ReactionTriggerConditionArgs args)
        {
            return args.BattleContext.target.IsDead;
        }

        public override ReactionTriggerCondition Copy()
        {
            return new KillCondition();
        }
        public override string Description => $"스킬의 대상이 사망 상태일 때";
    }

    [Serializable]
    public class HitCondition: ReactionTriggerCondition
    {
        public override bool IsMet(ReactionTriggerConditionArgs args)
        {
            return args.BattleContext.isEvaded == false;
        }

        public override ReactionTriggerCondition Copy()
        {
            return new HitCondition();
        }
        public override string Description => $"스킬에 회피가 발생하지 않았을 때";
    }

    [Serializable]
    public class DamageCondition : ReactionTriggerCondition
    {
        public abstract class ThresholdValue
        {
            public abstract float Resolve(ReactionTriggerConditionArgs args);
            public abstract string Description { get; }
        }

        public class AbsoluteThreshold : ThresholdValue
        {
            public float Value;
            public override float Resolve(ReactionTriggerConditionArgs args) => Value;
            public override string Description => $"피해량이 {Value} 이상일 때";
        }

        public class PercentOfMaxHpThreshold : ThresholdValue
        {
            public float Ratio; // 0~1
            public override float Resolve(ReactionTriggerConditionArgs args)
                => args.Subject.Stat.max_Hp * Ratio;
            public override string Description => $"피해량이 대상 최대 체력의 {Ratio * 100}% 이상일 때";
        }

        public class PercentOfCurrentHpThreshold : ThresholdValue
        {
            public float Ratio; // 0~1
            public override float Resolve(ReactionTriggerConditionArgs args)
                => args.Subject.Stat.current_Hp * Ratio;
            public override string Description => $"피해량이 대상 현재 체력의 {Ratio * 100}% 이상일 때";
        }
        
        [SerializeReference, SubclassSelector]
        public ThresholdValue Threshold;
        public override bool IsMet(ReactionTriggerConditionArgs args)
        {
            return args.BattleContext.value >= Threshold.Resolve(args);
        }

        public override ReactionTriggerCondition Copy()
        {
            return new DamageCondition();
        }

        public override string Description => Threshold.Description;
    }
}