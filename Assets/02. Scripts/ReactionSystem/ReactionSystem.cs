using System;
using System.Collections.Generic;
using System.Linq;
using _02._Scripts.BattleSystem;
using _03._PipeLine;
using ReactionSystem.Event;
using UnityEngine;
using VContainer;

namespace ReactionSystem
{
    public class ReactionSystem
    {
        private readonly List<Reaction> reactions;
        
        public void Register(Reaction reaction)
        {
            reactions.Add(reaction);
        }

        public void Unregister(Reaction reaction)
        {
            reactions.Remove(reaction);
        }
        
        /// <summary>
        /// 배틀 컨텍스트에 맞는 리액션들을 가져오는 함수
        /// </summary>
        /// <param name="ctx"></param>
        /// <returns></returns>
        public List<Reaction> GetReactions(BattleContext ctx)
        {
            // 조건에 맞는 함수 색출
            var triggered = reactions
                .Where(e => e.Trigger.Condition(ctx))
                .ToList();

            // 발동 성공 여부 계산
            foreach (var reaction in triggered)
            {
                reaction.Success = IsSuccess(reaction);
            }

            return triggered;
        }

        /// <summary>
        /// 리액션 성공 확률 계산 함수
        /// </summary>
        /// <param name="input">확률 계산에 사용될 변수들</param>
        /// <returns></returns>
        public static bool IsSuccess(object input) // TODO: 구현
        {
            // 계산을 위한 데이터를 담은 구조체를 정의해야 될 수도 있음.
            // 아니면 배틀 컨텍스트랑 리액션 정보로만 계산하던가
            
            // 계산식: [기본 확률 + 스트레스 가산치] - (지능 × 계수 + 파티 관계 보너스)
            
            return true;
        }
    }
}