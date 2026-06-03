using System;
using System.Collections.Generic;
using Buffs;
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
                default: // Observed / None → 매치된 관찰 대상
                    if (execution.MatchedSubjects != null)
                        foreach (var s in execution.MatchedSubjects)
                            yield return s;
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
    /// PreApply 가 Evasion/Critical/Defense 이전에 있을 때 동작 — 이후 단계가 reactor(새 타겟) 기준으로 흘러야 함.
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

    /// <summary>
    /// 버프 부여: 대상(관찰 대상 / Caster / Target)에게 일정 턴 지속되는 능력치 모디파이어를 건다.
    /// 공격력↑ 은 StatType.DamageMultiplier, 방어력↑ 은 StatType.DamageReduction(Flat, 비율값)으로 저작하면
    /// 파이프라인(EntryStep / DefenseStep)이 라이브로 읽어 데미지 계산에 반영한다.
    /// 값은 인라인(Modifiers)으로 저작한다.
    /// </summary>
    [Serializable][AddTypeMenu("Apply Buff", -400)]
    public class BuffReactionEffect : ReactionEffect
    {
        [Tooltip("버프를 받을 대상. 보통 Observed(조건을 만족시킨 관찰 대상).")]
        public E_TargetFilter BuffTarget = E_TargetFilter.Observed;

        [Tooltip("버프 식별자(중복/스택 판정). 비우면 Description 으로 대체된다.")]
        public string BuffId;

        [Tooltip("지속(버프 받은 캐릭터의 자기 턴 수).")]
        public int DurationTurns = 2;

        [Tooltip("적용할 능력치 모디파이어. 공격력↑=DamageMultiplier, 방어력↑=DamageReduction. value=추가 비율(0.3=+30%), mode(Flat/Percent) 무관.")]
        public List<StatModifier> Modifiers = new List<StatModifier>();

        public override UniTask Apply(BaseCharacter reactor, ReactionExecution execution, BattleContext originalContext)
        {
            foreach (var target in ResolveTargets(execution, originalContext))
            {
                if (target == null || target.IsDead) continue;
                target.ApplyBuff(new ActiveBuff
                {
                    Id = string.IsNullOrEmpty(BuffId) ? Description : BuffId,
                    Modifiers = CloneModifiers(Modifiers),
                    RemainingTurns = DurationTurns
                });
            }
            return UniTask.CompletedTask;
        }

        private IEnumerable<BaseCharacter> ResolveTargets(ReactionExecution execution, BattleContext context)
        {
            switch (BuffTarget)
            {
                case E_TargetFilter.Caster:
                    yield return context.caster;
                    yield break;
                case E_TargetFilter.Target:
                    yield return context.target;
                    yield break;
                default: // Observed / None → 매치된 관찰 대상
                    if (execution.MatchedSubjects != null)
                        foreach (var s in execution.MatchedSubjects)
                            yield return s;
                    yield break;
            }
        }

        private static List<StatModifier> CloneModifiers(List<StatModifier> src)
        {
            var list = new List<StatModifier>(src?.Count ?? 0);
            if (src != null)
                foreach (var m in src)
                    list.Add(new StatModifier { id = m.id, name = m.name, type = m.type, mode = m.mode, value = m.value });
            return list;
        }

        public override string Description => $"{BuffTarget}에게 버프 {DurationTurns}턴";

        public override ReactionEffect Clone() => new BuffReactionEffect
        {
            BuffTarget = BuffTarget,
            BuffId = BuffId,
            DurationTurns = DurationTurns,
            Modifiers = CloneModifiers(Modifiers)
        };
    }
}
