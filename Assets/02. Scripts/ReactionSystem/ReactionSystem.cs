using System.Collections.Generic;
using System.Linq;
using _03._PipeLine;

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
        
        /// <summary>
        /// 배틀 컨텍스트에 맞는 리액션들을 가져오는 함수
        /// </summary>
        /// <param name="eventArgs"></param>
        /// <returns></returns>
        public List<Reaction> GetReactions(BattleContext eventArgs)
        {
            // 조건에 맞는 리액션 색출
            var triggered = reactions
                .Where(e => e.Trigger.CheckCondition(eventArgs))
                .ToList();
            
            // 리액션 성공 실패 계산
            CalcSuccess(triggered);

            return triggered;
        }

        /// <summary>
        /// 리액션 성공 확률 계산 함수
        /// </summary>
        /// <param name="reaction">성공을 계산할 리액션</param>
        /// <returns></returns>
        private static bool IsSuccess(Reaction reaction) // TODO: 구현
        {
            // 계산을 위한 데이터를 담은 구조체를 정의해야 될 수도 있음.
            // 아니면 배틀 컨텍스트랑 리액션 정보로만 계산하던가
            
            // 계산식: [기본 확률 + 스트레스 가산치] - (지능 × 계수 + 파티 관계 보너스)
            
            return true;
        }
        
        /// <summary>
        /// 리액션 성공 확률 계산 함수
        /// </summary>
        /// <param name="reactions">성공을 계산할 리액션들</param>
        /// <returns></returns>
        private static void CalcSuccess(IEnumerable<Reaction> reactions)
        {
            foreach (var reaction in reactions)
            {
                reaction.Success = IsSuccess(reaction);
            }
        }
    }
}