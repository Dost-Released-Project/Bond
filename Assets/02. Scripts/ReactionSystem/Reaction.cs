using System;
using System.Collections;
using System.Collections.Generic;
using _03._PipeLine;
using UnityEngine;

namespace ReactionSystem
{
    public abstract class Reaction
    {
        public BaseCharacter Agent;     // 조건이 만족 됐을때 행동할 주체
        public Trigger Trigger;         // 리액션 행동을 발동시키는 조건
        public Action Behaviour;        // 조건 만족시 하게될 행동
        public bool Success;            // 성공 실패 결과
    }
    public class Reaction<T> : Reaction where T : EventArgs
    {
        public new Trigger<T> Trigger;  // 리액션 행동을 발동시키는 조건
        public new Action<T> Behaviour; // 조건 만족시 하게될 행동

        public Reaction(BaseCharacter agent, Trigger<T> trigger, Action<T> behaviour)
        {
            Agent = agent;
            Trigger = trigger;
            Behaviour = behaviour;
        }

        public void Action(T eventArgs)
        {
            // TODO: 전달변수 교체
            if (Trigger.Condition(eventArgs) && ReactionSystem.IsSuccess(new object()))
            {
                Behaviour?.Invoke(eventArgs);
            }
        }
    }
}