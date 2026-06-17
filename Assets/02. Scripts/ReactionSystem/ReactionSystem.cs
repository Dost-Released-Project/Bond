using System.Collections.Generic;
using System.Text;
using PipeLine;
using UnityEngine;
using BattleSystem.Interface;

namespace Reactions
{
    public enum ReactionResult
    {
        Default = 0,       // 평상시 — BaseEffect 실행 (역할=정상 행동, 성향=기본 행동)
        Anomaly = 1,       // 역할 대체 — 저관계 특이(돌발) 행동, AltEffect 실행
        BondAwakening = 2  // 성향 대체 — 고관계 강화 행동, AltEffect 실행
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
        public IBattleManager BattleManager { get; private set; }

        public void SetBattleManager(IBattleManager battleManager)
        {
            BattleManager = battleManager;
        }

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
                    if (owner.IsReactionSealed(reaction)) continue; // 봉인된 리액션은 발화 후보에서 제외

                    var matched = new List<BaseCharacter>();
                    foreach (var candidate in ResolveCandidates(reaction, owner, context, phase))
                    {
                        if (owner.IsUncooperativeWith(candidate)) continue; // 불협조 대상에겐 리액션/보조/보호하지 않음
                        if (reaction.Trigger.CheckCondition(candidate, context))
                            matched.Add(candidate);
                    }
                    if (matched.Count == 0) continue;

                    // 판정: 역할/성향에 따라 관계·스트레스·INT 기반으로 Default/대체(Anomaly|BondAwakening) 결정.
                    // 트리거가 충족되면 항상 실행을 생성하고, 결과가 어느 효과(Base/Alt)를 실행할지 가른다.
                    ReactionResult result = owner.JudgeReaction(reaction, matched);
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

            Debug.Log($"<color=lightblue>" +
                      $"Reaction Count [{phase}]: {executions.Count}\n" +
                      $"{sb.ToString()}\n" +
                      $"BattleContext:\n" +
                      $"Caster: {context.caster?.Name}\n" +
                      $"Target: {context.target?.Name}\n" +
                      $"Skill: {context.runtimeSkill.Data.DisplayName}\n" +
                      $"SkillType: {context.runtimeSkill.Data.Type}\n" +
                      $"IsCritical: {context.isCritical}\n" +
                      $"IsEvaded: {context.isEvaded}\n" +
                      $"</color>");

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
