using System;

namespace Reactions
{
    public abstract class Reaction
    {
        public BaseCharacter Agent;     // 조건이 만족 됐을때 행동할 주체
        public Trigger Trigger;         // 리액션 행동을 발동시키는 조건
        public Action Behaviour;        // 조건 만족시 하게될 행동
        public bool Success;            // 성공 실패 결과
    }
}