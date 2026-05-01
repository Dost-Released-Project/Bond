using System.Collections.Generic;
using System.Linq;
using PipeLine;
using UnityEngine;

namespace Reactions
{
    public enum ReactionResult
    {
        Success = 0,      // 이성 — UserSkill 실행
        Anomaly = 1,      // 본능 — AnomalySkill 강제 실행
        BondAwakening = 2 // 유대적 각성 — 돌발 취소 + 강화 스킬
    }

    public class ReactionExecution
    {
        public Reaction Reaction;       // 판정한 리액션
        public ReactionResult Result;   // 판정 결과 (연출 시스템에서도 사용)

        public ReactionExecution(Reaction reaction, ReactionResult result)
        {
            Reaction = reaction;
            Result = result;
        }
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
    
    public interface IReactionRegistry
    {
        /// <summary>
        /// 유저가 설정 완료한 리액션 목록을 등록합니다.
        /// </summary>
        /// <param name="reaction"></param>
        void Register(Reaction reaction);

        /// <summary>
        /// 해당 캐릭터의 등록된 리액션을 모두 등록 해제합니다. 
        /// </summary>
        /// <param name="character"></param>
        void Unregister(BaseCharacter character);
        
        /// <summary>
        /// 해당 리액션을 등록 해제합니다.
        /// </summary>
        /// <param name="reaction"></param>
        void Unregister(Reaction reaction);
    }
    
    public class ReactionSystem : IReactionRegistry, IReactionResolver
    {
        private readonly List<Reaction> reactions;
        
        public void Register(Reaction reaction)
        {
            reactions.Add(reaction);
        }

        public void Unregister(BaseCharacter character)
        {
            var filtered = reactions.Where(reaction => reaction.Agent == character);
            foreach (var reaction in filtered)
            {
                Unregister(reaction);
            }
        }

        public void Unregister(Reaction reaction)
        {
            reactions.Remove(reaction);
        }
        
        public IReadOnlyList<ReactionExecution> Resolve(BattleContext context)
        {
            // 조건에 맞는 리액션 색출
            var conditionPassed = reactions
                .Where(e => e.Trigger.CheckCondition(context))
                .ToList();
            
            // 리액션 성공 실패 계산
            // TODO: 정렬 과정 필요
            return ExecuteReactions(conditionPassed).ToList().AsReadOnly(); 
        }

        /// <summary>
        /// 리액션의 성공 확률을 계산해 반환합니다.
        /// </summary>
        /// <param name="reaction">성공을 계산할 리액션</param>
        /// <returns></returns>
        private static ReactionResult IsSuccess(Reaction reaction) // TODO: 구현
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
        private static IEnumerable<ReactionExecution> ExecuteReactions(IEnumerable<Reaction> reactions)
        {
            return reactions.Select(reaction => new ReactionExecution(reaction, ReactionResult.Success));
        }
    }
}