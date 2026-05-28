using System.Collections.Generic;
using System.Text;
using PipeLine;
using UnityEngine;
using Random = UnityEngine.Random;

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
        IReadOnlyList<ReactionExecution> Resolve(BattleContext context, E_ReactionPhase phase);
    }

    public class ReactionSystem : IReactionResolver
    {
        private readonly List<BaseCharacter> Characters = new List<BaseCharacter>();
        private readonly Dictionary<BaseCharacter, E_BattleSide> _sideCache = new Dictionary<BaseCharacter, E_BattleSide>();

        public void Register(BaseCharacter agent)
        {
            if (agent == null) return;
            Characters.Add(agent);
            if (agent.CurrentSlot != null)
                _sideCache[agent] = agent.CurrentSlot.side;
        }

        public void Unregister(BaseCharacter agent)
        {
            if (agent == null) return;
            Characters.Remove(agent);
            _sideCache.Remove(agent);
        }

        /// <summary>
        /// 캐시된 진영이 있으면 그것을, 없으면 현재 슬롯에서 한 번 더 시도해 캐시.
        /// 사망 후 슬롯이 Clear 되어도 캐시는 유지된다.
        /// </summary>
        private E_BattleSide? SideOf(BaseCharacter c)
        {
            if (c == null) return null;
            if (_sideCache.TryGetValue(c, out var cached)) return cached;
            var live = c.CurrentSlot?.side;
            if (live != null) _sideCache[c] = live.Value;
            return live;
        }

        public IReadOnlyList<ReactionExecution> Resolve(BattleContext context, E_ReactionPhase phase)
        {
            var executions = new List<ReactionExecution>();

            foreach (var owner in Characters)
            {
                if (owner.IsDead) continue;

                foreach (var reaction in owner.Reactions)
                {
                    if (reaction == null) continue;
                    if (reaction.Phase == E_ReactionPhase.None) continue;
                    if (reaction.Phase != phase) continue;
                    if (reaction.Trigger == null) continue;

                    var matched = new List<BaseCharacter>();
                    foreach (var candidate in ResolveCandidates(reaction, owner, context, phase))
                    {
                        if (reaction.Trigger.CheckCondition(candidate, context))
                            matched.Add(candidate);
                    }
                    if (matched.Count == 0) continue;

                    var result = IsSuccess(owner, matched, reaction);
                    executions.Add(new ReactionExecution(owner, reaction, result, matched));
                }
            }

            executions.Sort();

            var sb = new StringBuilder();
            foreach (var execution in executions)
            {
                var subjects = string.Join(", ",
                    execution.MatchedSubjects == null ? new[] { "-" } : execution.MatchedSubjects.ConvertToNames());
                sb.AppendLine($"{execution.Agent.Name} | Speed: {execution.Agent.Speed} | Matched: {subjects}");
            }
            
            if (executions.Count != 0)
            {
                Debug.Log($"<color=lightblue>" +
                          $"Reaction Count [{phase}]: {executions.Count}\n" +
                          $"{sb.ToString()}</color>\n\n" + 
                          $"BattleContext:\n" +
                          $"Caster: {context.caster?.Name}\n" +
                          $"Target: {context.target?.Name}\n" +
                          $"Skill: {context.runtimeSkill.Data.DisplayName}\n" +
                          $"SkillType: {context.runtimeSkill.Data.Type}\n" +
                          $"IsCritical: {context.isCritical}\n" +
                          $"IsEvaded: {context.isEvaded}\n" +
                          $"</color>");
            }

            return executions.AsReadOnly();
        }

        private IEnumerable<BaseCharacter> ResolveCandidates(
            Reaction reaction, BaseCharacter owner, BattleContext context, E_ReactionPhase phase)
        {
            switch (reaction.ObserveFilter)
            {
                case E_ObserveFilter.Self:
                    if (owner != null) yield return owner;
                    yield break;

                case E_ObserveFilter.Specific:
                    if (!string.IsNullOrEmpty(reaction.SubjectCharacterId)
                        && BaseCharacter.Dict.TryGetValue(reaction.SubjectCharacterId, out var specific))
                        yield return specific;
                    yield break;

                case E_ObserveFilter.Ally:
                case E_ObserveFilter.OtherAlly:
                case E_ObserveFilter.Enemy:
                    var ownerSide = SideOf(owner);
                    if (ownerSide == null) yield break;

                    var seen = new HashSet<BaseCharacter>();

                    foreach (var c in Characters)
                    {
                        if (c == null || c.IsDead) continue;
                        var cSide = SideOf(c);
                        if (cSide == null) continue;
                        if (!MatchesSide(reaction.ObserveFilter, cSide.Value, ownerSide.Value, c, owner)) continue;
                        if (seen.Add(c)) yield return c;
                    }
                    
                    // PostApply 단계에선 context의 actor가 막 죽었어도 후보로 포함 (KillCondition 등)
                    if (phase == E_ReactionPhase.PostApply)
                    {
                        if (context.caster != null && TryIncludeActor(context.caster, reaction.ObserveFilter, ownerSide.Value, owner, seen))
                            yield return context.caster;
                        if (context.target != null && TryIncludeActor(context.target, reaction.ObserveFilter, ownerSide.Value, owner, seen))
                            yield return context.target;
                    }
                    
                    yield break;
            }
        }

        // PostApply에서 context의 actor(사망 가능성 있음)를 필터 통과 시 후보 집합에 추가.
        // seen.Add 의 반환을 그대로 돌려줘서 호출부가 중복 없이 yield 할지 결정할 수 있게 함.
        private bool TryIncludeActor(BaseCharacter actor, E_ObserveFilter filter, E_BattleSide ownerSide, BaseCharacter owner, HashSet<BaseCharacter> seen)
        {
            var actorSide = SideOf(actor);
            if (actorSide == null) return false;
            if (!MatchesSide(filter, actorSide.Value, ownerSide, actor, owner)) return false;
            return seen.Add(actor);
        }

        // ObserveFilter 종류별로 후보가 owner의 진영 관계에 맞는지 판정.
        // Ally(자신 포함) / OtherAlly(자신 제외) / Enemy(반대 진영) 분기.
        private static bool MatchesSide(E_ObserveFilter filter, E_BattleSide candidateSide, E_BattleSide ownerSide, BaseCharacter candidate, BaseCharacter owner)
        {
            return filter switch
            {
                E_ObserveFilter.Ally => candidateSide == ownerSide,
                E_ObserveFilter.OtherAlly => candidateSide == ownerSide && candidate != owner,
                E_ObserveFilter.Enemy => candidateSide != ownerSide,
                _ => false,
            };
        }

        /// <summary>
        /// 리액션의 성공 확률을 계산해 반환합니다.
        /// </summary>
        private static ReactionResult IsSuccess(BaseCharacter agent, IReadOnlyList<BaseCharacter> matched, Reaction reaction) // TODO: 구현
        {
            // 계산식: [기본 확률 + 스트레스 가산치] - (지능 × 계수 + 파티 관계 보너스)
            // 파티 관계 보너스는 agent와 matched 사이의 Relation 값을 사용 예정

            return (ReactionResult)Random.Range(0,3); // 임시 처리임
        }
    }

    internal static class ReactionDebugExt
    {
        public static IEnumerable<string> ConvertToNames(this IReadOnlyList<BaseCharacter> chars)
        {
            for (int i = 0; i < chars.Count; i++)
                yield return chars[i]?.Name ?? "?";
        }
    }
}
