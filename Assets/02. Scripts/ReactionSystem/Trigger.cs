using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using PipeLine;
using UnityEngine;
using UnityEngine.Serialization;

namespace Reactions
{
    public enum E_ObserveFilter
    {
        Self,
        Ally,
        Enemy,
        Specific
    }
    
    public enum E_TargetFilter
    {
        None,
        Caster,
        Target
    }
    
    public interface ITrigger
    {
        bool CheckCondition(BaseCharacter subject, BattleContext context);
        string Description { get; }
    }

    [Serializable][AddTypeMenu("SkillTypeIs", -1000)]
    public class Trigger : ITrigger
    {
        [Serializable]
        public class Condition
        {
            [HideInInspector]
            public List<ICondition> Essential = new List<ICondition>();
            
            [SerializeReference, SubclassSelector]
            public List<ICondition> Additional = new List<ICondition>();
            
            public List<ICondition> Whole => Essential.Concat(Additional).ToList();
        }

        [SerializeField] protected Condition _condition = new Condition();
        public List<ICondition> Conditions => _condition.Whole;

        public Trigger() { }

        public Trigger(params ICondition[] condition)
        {
            _condition.Essential = condition.ToList();
        }

        public bool CheckCondition(BaseCharacter subject, BattleContext context)
        {
            Debug.Log($"Checking condition {Conditions.Count}");
            return Conditions.All(condition => condition.IsMet(new ReactionTriggerConditionArgs() { Subject = subject, BattleContext = context }));
        }

        public string Description
        {
            get
            {
                StringBuilder sb = new StringBuilder();
                sb.AppendLine(Conditions[0].Description);
                for (int i = 1; i < Conditions.Count; i++)
                {
                    sb.AppendLine($" && {Conditions[i].Description}");
                }
                return $"{sb.ToString()}";
            }
        }
    }

    [Serializable]
    public class GetHitTrigger : Trigger
    {
        public GetHitTrigger()
        {
            _condition.Essential = new List<ICondition>()
            {
                new SubjectCondition(E_TargetFilter.Target),
                new SkillTypeCondition(SkillType.OFFENSIVE, SkillType.SPELL),
                new HitCondition()
            };
        }
    }

    [Serializable]
    public class DeathBlowTrigger : Trigger
    {
        public DeathBlowTrigger()
        {
            _condition.Essential = new List<ICondition>()
            {
                new SubjectCondition(E_TargetFilter.Target),
                new SkillTypeCondition(SkillType.OFFENSIVE, SkillType.SPELL),
                new HitCondition(),
                new DamageCondition() { Threshold = new DamageCondition.PercentOfCurrentHpThreshold() { Ratio = 1f } }
            };
        }
    }
}