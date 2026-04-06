using System;
using _02._Scripts.BattleSystem;
using ReactionSystem.Event;
using UnityEngine;
using VContainer;

namespace ReactionSystem
{
    public class ReactionSystem
    {
        [Inject]
        private readonly EventBus eventBus;
        
        public void Register<T>(Reaction<T> reaction) where T: EventArgs
        {
            eventBus.Subscribe<T>(reaction.Action);
        }

        public void Unregister<T>(Reaction<T> reaction) where T : EventArgs
        {
            eventBus.Unsubscribe<T>(reaction.Action);
        }
        
        /// <summary>
        /// 리액션 성공 확률 계산 함수
        /// </summary>
        /// <param name="input">확률 계산에 사용될 변수들</param>
        /// <returns></returns>
        public static bool IsSuccess(object input) // TODO: 매개변수 정의
        {
            // 계산식: [기본 확률 + 스트레스 가산치] - (지능 × 계수 + 파티 관계 보너스)
            
            return true;
        }
    }
}