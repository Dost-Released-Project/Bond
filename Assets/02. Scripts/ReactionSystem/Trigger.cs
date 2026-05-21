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
        bool CheckCondition(BattleContext context);
        string Description { get; }
    }

    [Serializable]
    public class Trigger : ITrigger
    {
        public string SubjectCharacterId; // 캐릭터 ID

        [SerializeReference, SubclassSelector]
        public List<ICondition> Conditions = new List<ICondition>() { new TargetCondition() };

        public Trigger() { }

        public Trigger(BaseCharacter subject, params ICondition[] condition)
        {
            SubjectCharacterId = subject;
            Conditions = condition.ToList();
        }

        public bool CheckCondition(BattleContext context)
        {
            Debug.Assert(BaseCharacter.Dict.ContainsKey(SubjectCharacterId));
            var subject = BaseCharacter.Dict[SubjectCharacterId];
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
                    sb.Append($" && {Conditions[i].Description}");
                }
                return $"{BaseCharacter.Dict[SubjectCharacterId].Name}이 {sb.ToString()}";
            }
        }
    }
}