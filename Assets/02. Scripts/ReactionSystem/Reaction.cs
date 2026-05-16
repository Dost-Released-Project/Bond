using System;
using System.Runtime.Serialization;
using UnityEngine;

namespace Reactions
{
    public enum ReactionSource
    {
        Role,
        Trait
    }

    [Serializable]
    public class ReactionExecution
    {
        public BaseCharacter Agent;
        public Reaction Reaction;
        public ReactionResult Result = ReactionResult.Success;

        public ReactionExecution(BaseCharacter agent, Reaction reaction, ReactionResult result)
        {
            Agent = agent;
            Reaction = reaction;
            Result = result;
        }
    }
    
    [Serializable]
    public class Reaction
    {
        public string Id;
        public ReactionSource Source;                       // 출처 (역할 or 성향)
        [SerializeReference] public ITrigger Trigger;       // 리액션 행동을 발동시키는 조건
        [SerializeReference] public SkillBase Behaviour;    // 조건 만족시 하게될 행동
        //[SerializeReference] public SkillBase AnomalySkill; // 돌발 행동 시 행동

        public Reaction()
        {
            Id = System.Guid.NewGuid().ToString();
        }

        public Reaction(SerializationInfo info, StreamingContext context)
        {
            Id = info.GetString("Id");
        }
        
        public override string ToString()
        {
            return $"ID: {Id} | reaction-{Behaviour}-";
        }
    }
}