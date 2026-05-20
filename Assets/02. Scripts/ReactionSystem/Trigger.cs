using System;
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
        [SerializeReference, SubclassSelector] public ICondition Condition;

        public Trigger() { }

        public Trigger(BaseCharacter subject, ICondition condition)
        {
            SubjectCharacterId = subject;
            Condition = condition;
        }
        
        public bool CheckCondition(BattleContext context)
        {
            Debug.Assert(BaseCharacter.Dict.ContainsKey(SubjectCharacterId));
            return Condition.IsMet(BaseCharacter.Dict[SubjectCharacterId], context);
        }

        public string Description => $"{BaseCharacter.Dict[SubjectCharacterId].Name}이 {Condition.Description}";
    }
}