using System;

namespace Reactions
{
    public class Reaction
    {
        public BaseCharacter Agent;     // 조건이 만족 됐을때 행동할 주체
        public ITrigger Trigger;        // 리액션 행동을 발동시키는 조건
        public SkillBase Behaviour;     // 조건 만족시 하게될 행동
        public bool Success;            // 성공 실패 결과

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