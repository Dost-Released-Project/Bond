using System.Collections.Generic;
using System.Linq;
using PipeLine;
using UnityEngine;
using UnityEngine.Rendering.Universal;

namespace Reactions
{
    public enum ReactionResult
    {
        Success = 0,      // 이성 — UserSkill 실행
        Anomaly = 1,      // 본능 — AnomalySkill 강제 실행
        BondAwakening = 2 // 유대적 각성 — 돌발 취소 + 강화 스킬
    }
    
    public interface IReactionResolver
    {
        /// <summary>
        /// 해당 배틀 컨텍스트에서 조건을 만족한 리액션 목록을 판정 결과와 함께 반환합니다.
        /// </summary>
        /// <param name="context"></param>
        /// <returns>ReactionExecution 컬렉션</returns>
        IReadOnlyList<ReactionExecution> Resolve(BattleContext context);
    }
    
    // 그냥 전투 중인 캐릭터의 참조를 들고 있는게 나은것 같음
    // 아니면 context에 캐릭터들의 참조를 담거나
    public class ReactionSystem : IReactionResolver
    {
        private readonly List<BaseCharacter> Characters;
        
        public void Register(BaseCharacter agent)
        {
            Characters.Add(agent);
        }

        public void Unregister(BaseCharacter agent)
        {
            Characters.Remove(agent);
        }
        
        public IReadOnlyList<ReactionExecution> Resolve(BattleContext context)
        {
            List<ReactionExecution> executions = new List<ReactionExecution>();

            // 조건에 맞는 리액션 색출
            foreach (var chara in Characters)
            {
                foreach (var reaction in chara.Reactions)
                {
                    if (reaction.Trigger.CheckCondition(context))
                    {
                        var result = IsSuccess(chara, reaction);
                        var execution = new ReactionExecution(chara, reaction, result);
                        
                        executions.Add(execution);
                    }
                }
            }
            
            // 리액션 성공 실패 계산
            // TODO: 정렬 과정 필요
            return executions.AsReadOnly(); 
        }

        /// <summary>
        /// 리액션의 성공 확률을 계산해 반환합니다.
        /// </summary>
        /// <param name="execution">성공을 계산할 리액션</param>
        /// <returns></returns>
        private static ReactionResult IsSuccess(BaseCharacter agent, Reaction reaction) // TODO: 구현
        {
            // 계산을 위한 데이터를 담은 구조체를 정의해야 될 수도 있음.
            // 아니면 배틀 컨텍스트랑 리액션 정보로만 계산하던가
            
            // 계산식: [기본 확률 + 스트레스 가산치] - (지능 × 계수 + 파티 관계 보너스)
            
            return (ReactionResult)Random.Range(0,3); // 임시 처리임
        }
        
        /// <summary>
        /// 리액션의 성공 확률을 계산해 리액션 데이터를 수정합니다.
        /// </summary>
        /// <param name="reactions">성공을 계산할 리액션들</param>
        /// <returns></returns>
        // private static IEnumerable<ReactionExecution> ExecuteReactions(IEnumerable<Reaction> reactions)
        // {
        //     return reactions.Select(reaction => new ReactionExecution(reaction, ReactionResult.Success));
        // }
    }
}