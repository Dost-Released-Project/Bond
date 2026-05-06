using System;
using System.Runtime.Serialization;

namespace Reactions
{
    public enum ReactionSource
    {
        Role,
        Trait
    }
    
    [Serializable]
    public class Reaction : ISerializable
    {
        public string Id;
        [NonSerialized] public BaseCharacter Agent;    // 조건이 만족 됐을때 행동할 주체
        public ReactionSource Source;  // 출처 (역할 or 성향)
        public Trigger Trigger;        // 리액션 행동을 발동시키는 조건
        public SkillBase Behaviour;    // 조건 만족시 하게될 행동
        public SkillBase AnomalySkill; // 돌발 행동 시 행동

        public Reaction()
        {
            Id = System.Guid.NewGuid().ToString();
        }
        
        public override string ToString()
        {
            return $"ID: {Id} | {Agent}'s reaction-{Behaviour}-";
        }

        public void GetObjectData(SerializationInfo info, StreamingContext context)
        {
            info.AddValue("Id", Id);
            info.AddValue("Sex", false);
        }
    }
}