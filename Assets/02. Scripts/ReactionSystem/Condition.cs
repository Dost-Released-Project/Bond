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
        [Tooltip("Caster or Target")] public E_TargetFilter Filter = E_TargetFilter.None;
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
        public List<SkillType> Types = new List<SkillType>();
        public override bool IsMet(ReactionTriggerConditionArgs args)
        {
            return Types.Contains(args.BattleContext.runtimeSkill.Data.Type);
        }
        
        public SkillTypeCondition() { }

        public SkillTypeCondition(params SkillType[] type)
        {
            Types.AddRange(type);
        }

        public override ReactionTriggerCondition Copy()
        {
            return new SkillTypeCondition() { Types = new List<SkillType>(Types) };
        }

        public override string Description => $"스킬 타입이 {string.Join(" or ", Types)}일 때";
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
    public class HpAboveCondition: ReactionTriggerCondition
    {
        public float Threshold;

        public override bool IsMet(ReactionTriggerConditionArgs args)
            => args.Subject.HpRatio >= Threshold;

        public override ReactionTriggerCondition Copy()
            => new HpAboveCondition { Threshold = Threshold };

        public override string Description => $"관찰 대상의 Hp 비율이 {Threshold} 이상일 때";
    }

    [Serializable]
    public class StressAboveCondition: ReactionTriggerCondition
    {
        public int Threshold = 50;

        public override bool IsMet(ReactionTriggerConditionArgs args)
            => args.Subject.Insanity >= Threshold;

        public override ReactionTriggerCondition Copy()
            => new StressAboveCondition { Threshold = Threshold };

        public override string Description => $"관찰 대상의 스트레스가 {Threshold} 이상일 때";
    }

    [Serializable]
    public class PartyStressAverageCondition: ReactionTriggerCondition
    {
        public float Threshold = 60f; // 같은 진영 생존자 평균 스트레스(0~100) 임계. 초과 시 발동.

        public override bool IsMet(ReactionTriggerConditionArgs args)
        {
            var party = args.Subject.GetSameSideAllies(true).ToList();
            if (party.Count == 0) return false;
            float avg = (float)party.Sum(c => c.Insanity) / party.Count;
            return avg > Threshold;
        }

        public override ReactionTriggerCondition Copy()
            => new PartyStressAverageCondition { Threshold = Threshold };

        public override string Description => $"파티 평균 스트레스가 {Threshold} 초과일 때";
    }

    [Serializable]
    public class ReactionCountCondition: ReactionTriggerCondition
    {
        public int Threshold = 3; // 마지막 자기 턴 이후 발동한 리액션 수 임계. 이상이면 발동.

        public override bool IsMet(ReactionTriggerConditionArgs args)
            => args.Subject.ReactionsFiredCount >= Threshold;

        public override ReactionTriggerCondition Copy()
            => new ReactionCountCondition { Threshold = Threshold };

        public override string Description => $"리액션을 {Threshold}회 이상 발동했을 때";
    }

    [Serializable]
    public class AllyAnomalyCondition: ReactionTriggerCondition
    {
        public override bool IsMet(ReactionTriggerConditionArgs args)
            => args.Subject.GetSameSideAllies(false).Any(a => a.HasRecentAnomaly);

        public override ReactionTriggerCondition Copy() => new AllyAnomalyCondition();

        public override string Description => "같은 진영 아군이 최근 돌발했을 때";
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
            public abstract ThresholdValue Copy();
        }

        public class AbsoluteThreshold : ThresholdValue
        {
            public float Value;
            public override float Resolve(ReactionTriggerConditionArgs args) => Value;
            public override string Description => $"피해량이 {Value} 이상일 때";
            public override ThresholdValue Copy() => new AbsoluteThreshold { Value = Value };
        }

        public class PercentOfMaxHpThreshold : ThresholdValue
        {
            public float Ratio; // 0~1
            public override float Resolve(ReactionTriggerConditionArgs args)
                => args.Subject.Stat.max_Hp * Ratio;
            public override string Description => $"피해량이 대상 최대 체력의 {Ratio * 100}% 이상일 때";
            public override ThresholdValue Copy() => new PercentOfMaxHpThreshold { Ratio = Ratio };
        }

        public class PercentOfCurrentHpThreshold : ThresholdValue
        {
            public float Ratio; // 0~1
            public override float Resolve(ReactionTriggerConditionArgs args)
                => args.Subject.Stat.current_Hp * Ratio;
            public override string Description => $"피해량이 대상 현재 체력의 {Ratio * 100}% 이상일 때";
            public override ThresholdValue Copy() => new PercentOfCurrentHpThreshold { Ratio = Ratio };
        }

        [SerializeReference, SubclassSelector]
        public ThresholdValue Threshold;
        public override bool IsMet(ReactionTriggerConditionArgs args)
        {
            return args.BattleContext.value >= Threshold.Resolve(args);
        }

        public override ReactionTriggerCondition Copy()
        {
            return new DamageCondition { Threshold = Threshold?.Copy() };
        }

        public override string Description => Threshold.Description;
    }
}