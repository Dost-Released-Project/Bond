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

    [Serializable]
    public class Trigger : ITrigger
    {
        [SerializeReference, SubclassSelector]
        public List<ICondition> Conditions = new List<ICondition>() { new SubjectCondition() };

        public Trigger() { }

        public Trigger(params ICondition[] condition)
        {
            Conditions = condition.ToList();
        }

        public bool CheckCondition(BaseCharacter subject, BattleContext context)
        {
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
}