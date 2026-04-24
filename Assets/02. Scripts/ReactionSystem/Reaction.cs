using System;

namespace Reactions
{
    public enum ReactionSource
    {
        Role,
        Trait
    }
    
    public class Reaction
    {
        public BaseCharacter Agent;    // 조건이 만족 됐을때 행동할 주체
        public ReactionSource Source;  // 출처 (역할 or 성향)
        public ITrigger Trigger;       // 리액션 행동을 발동시키는 조건
        public SkillBase Behaviour;    // 조건 만족시 하게될 행동
        public SkillBase AnomalySkill; // 돌발 행동 시 행동

        private int id;

        public Reaction(int id)
        {
            this.id = id;
        }

        public override string ToString()
        {
            return $"ID: {id} | {Agent}'s reaction-{Behaviour}-";
        }
    }
}