using System;
using System.Runtime.Serialization;
using PipeLine;
using UnityEngine;

namespace Reactions
{
    public enum ReactionSource
    {
        Role,
        Trait
    }

    [Serializable]
    public class ReactionExecution : IComparable<ReactionExecution>
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
        
        public int CompareTo(ReactionExecution other) => other.Agent.Speed.CompareTo(Agent.Speed);

        public override string ToString()
        {
            return $"Agent: {Agent.Name}\n" +
                   $"Trigger: {Reaction.Trigger.Description}";
        }
    }
    
    [Serializable]
    public class Reaction
    {
        //public ReactionSource Source;
        public string SubjectCharacterId; // 캐릭터 ID
        [SerializeReference, SubclassSelector] public ITrigger Trigger;
        public int SkillIndex; // 반응으로 실행할 스킬의 인덱스
        public E_TargetFilter ReactionSkillTarget;
        //[SerializeReference] public SkillBase AnomalySkill; // 돌발 행동 시 행동

        public bool Check(BattleContext context)
        {
            if (Trigger == null)
                return false;
            return Trigger.CheckCondition(BaseCharacter.Dict[SubjectCharacterId], context);
        }
    }

    [CreateAssetMenu(fileName = "ReactionPreset", menuName = "Bond/Reactions/Character Preset")]
    public class ReactionPreset : ScriptableObject
    {
        public ITrigger Trigger;
        public SkillBase Skill;
        public E_TargetFilter SkillTarget;
    }
}