using System;
using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using PipeLine;
using Unity.VisualScripting;
using UnityEngine;

namespace Reactions
{
    public abstract class ReactionEffect
    {
        /// <summary>
        /// 리액션이 발화될 때 수행할 효과.
        /// originalContext 는 원본 행동의 BattleContext — Pre-Apply 단계에서 변형 가능.
        /// </summary>
        public abstract UniTask Apply(BaseCharacter reactor, ReactionExecution execution, BattleContext originalContext);

        public abstract string Description { get; }
        public abstract ReactionEffect Clone();
    }

    /// <summary>
    /// 기존 동작: reactor 의 스킬을 새 BattleContext 로 발동.
    /// 대상은 매치된 관찰 대상(Observed) 또는 원본의 caster/target.
    /// </summary>
    [Serializable][AddTypeMenu("Cast Skill", -1000)]
    public class SkillCastReactionEffect : ReactionEffect
    {
        public int SkillIndex;
        public E_TargetFilter SkillTarget;

        public override async UniTask Apply(BaseCharacter reactor, ReactionExecution execution, BattleContext originalContext)
        {
            if (reactor == null || reactor.onBattleAction == null) return;
            if (SkillIndex < 0 || SkillIndex >= reactor.Skills.Length) return;
            var skill = reactor.Skills[SkillIndex];
            if (skill == null) return;

            foreach (var target in ResolveTargets(execution, originalContext))
            {
                if (target == null || target.IsDead) continue;

                var bc = new BattleContext(reactor, skill, true)
                {
                    isReaction = true,
                    target = target
                };
                await reactor.onBattleAction.Invoke(bc);
            }
        }

        private IEnumerable<BaseCharacter> ResolveTargets(ReactionExecution execution, BattleContext context)
        {
            switch (SkillTarget)
            {
                case E_TargetFilter.Caster:
                    yield return context.caster;
                    yield break;
                case E_TargetFilter.Target:
                    yield return context.target;
                    yield break;
                case E_TargetFilter.Observed:
                    if (execution.MatchedSubjects != null)
                        foreach (var s in execution.MatchedSubjects)
                            yield return s;
                    yield break;
                default:
                    yield return context.target;
                    yield break;
            }
        }

        public override string Description => $"스킬 #{SkillIndex} 발동 → {SkillTarget}";

        public override ReactionEffect Clone() => new SkillCastReactionEffect { SkillIndex = SkillIndex, SkillTarget = SkillTarget };
    }

    /// <summary>
    /// 대신 맞기: 원본 context 의 타겟을 reactor 로 변경.
    /// 이후 파이프라인의 Evasion/Critical/Defense 가 새 타겟(reactor) 기준으로 다시 계산됨.
    /// context.interceptedFor 에 원래 보호받은 캐릭터를 보관해 viz 가 끼어들기 연출을 재생할 수 있게 함.
    /// PreApply 가 EntryStep 직후에 있을 때 동작 — 이후 단계가 reactor 기준으로 흘러야 함.
    /// </summary>
    [Serializable][AddTypeMenu("Intercept", -500)]
    public class InterceptReactionEffect : ReactionEffect
    {
        public override UniTask Apply(BaseCharacter reactor, ReactionExecution execution, BattleContext originalContext)
        {
            if (reactor == null || originalContext == null) return UniTask.CompletedTask;

            originalContext.interceptedFor = originalContext.target;
            originalContext.target = reactor;

            Debug.Log($"<color=magenta>[Intercept]</color> {reactor.Name} 가 {originalContext.interceptedFor?.Name} 을 대신해 공격을 받습니다.");

            return UniTask.CompletedTask;
        }

        public override string Description => "대상이 받을 공격을 대신 받음";

        public override ReactionEffect Clone() => new InterceptReactionEffect();
    }
}
