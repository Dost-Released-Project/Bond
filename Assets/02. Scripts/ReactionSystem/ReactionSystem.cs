using System.Collections.Generic;
using System.Linq;
using _02._Scripts.BattleSystem;
using _03._PipeLine;
using UnityEngine;
using VContainer;

namespace Reactions
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
        
        // /// <summary>
        // /// 배틀 컨텍스트에 따른 리액션을 처리합니다.
        // /// </summary>
        // /// <param name="context">리액션을 일으킬 배틀 컨텍스트</param>
        // public void RaiseReactions(BattleContext context)
        // {
        //     var raised = GetReactions(context);
        //     foreach (var reaction in raised)
        //     {
        //         if (reaction.Success)
        //         {
        //             Debug.Log($"<color=yellow>{reaction}) is Raising</color>");
        //             battleManager.SkillApplyLogic(
        //                 new BattleContext()
        //                 {
        //                     caster = reaction.Agent,
        //                     runtimeSkill = reaction.Behaviour
        //                 });
        //         }
        //         else
        //         {
        //             Debug.Log($"<color=yellow>{reaction}) has Failed</color>");
        //         }
        //     }
        // }
        
        /// <summary>
        /// 배틀 컨텍스트에 맞는 리액션들을 반환합니다.
        /// </summary>
        /// <param name="battleContext"></param>
        /// <returns></returns>
        public List<Reaction> GetReactions(BattleContext battleContext)
        {
            // 조건에 맞는 리액션 색출
            var triggered = reactions
                .Where(e => e.Trigger.CheckCondition(battleContext))
                .ToList();
            
            // 리액션 성공 실패 계산
            CalcSuccess(triggered);

            return triggered;
        }

        /// <summary>
        /// 리액션의 성공 확률을 계산해 반환합니다.
        /// </summary>
        /// <param name="reaction">성공을 계산할 리액션</param>
        /// <returns></returns>
        public static bool IsSuccess(Reaction reaction) // TODO: 구현
        {
            // 계산을 위한 데이터를 담은 구조체를 정의해야 될 수도 있음.
            // 아니면 배틀 컨텍스트랑 리액션 정보로만 계산하던가
            
            // 계산식: [기본 확률 + 스트레스 가산치] - (지능 × 계수 + 파티 관계 보너스)
            
            return Random.Range(0,1f) < 0.5f;
        }
        
        /// <summary>
        /// 리액션의 성공 확률을 계산해 리액션 데이터를 수정합니다.
        /// </summary>
        /// <param name="reactions">성공을 계산할 리액션들</param>
        /// <returns></returns>
        public static void CalcSuccess(IEnumerable<Reaction> reactions)
        {
            foreach (var reaction in reactions)
            {
                reaction.Success = IsSuccess(reaction);
            }
        }
    }
}