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

    /// <summary>
    /// 리액션 봉인: 대상(자기=리액터 / 관찰 대상)의 리액션을 일정 턴 동안 못 쓰게 한다.
    /// DesignedOnly 로 역할(설계) 리액션을 봉인하면 "설계 리액션 무시"의 지속 버전이 된다.
    /// </summary>
    [Serializable][AddTypeMenu("Seal Reactions", -300)]
    public class SealReactionEffect : ReactionEffect
    {
        [Tooltip("자기(리액터)를 봉인할지. false면 관찰 대상(Observed)을 봉인.")]
        public bool TargetSelf = true;

        [Tooltip("봉인 범위. All=전체 / DesignedOnly=역할(설계) 리액션 / Slots=무작위 N개")]
        public SealKind Kind = SealKind.All;

        [Tooltip("Kind==Slots 일 때 봉인할 슬롯 수")]
        public int Count = 1;

        [Tooltip("지속(봉인 대상의 자기 턴 수). 1=이번 라운드, 2=다음 턴까지")]
        public int DurationTurns = 1;

        public override UniTask Apply(BaseCharacter reactor, ReactionExecution execution, BattleContext originalContext)
        {
            var target = TargetSelf ? reactor : FirstObserved(execution) ?? reactor;
            if (target == null) return UniTask.CompletedTask;

            var seal = new ReactionSeal { Kind = Kind, RemainingTurns = DurationTurns };
            if (Kind == SealKind.Slots) seal.Slots = PickRandomSlots(target, Count);

            target.ApplySeal(seal);
            return UniTask.CompletedTask;
        }

        private static BaseCharacter FirstObserved(ReactionExecution execution)
            => (execution.MatchedSubjects != null && execution.MatchedSubjects.Count > 0)
                ? execution.MatchedSubjects[0] : null;

        private static HashSet<Reaction> PickRandomSlots(BaseCharacter c, int count)
        {
            var pool = new List<Reaction>();
            foreach (var r in c.Reactions) if (r != null) pool.Add(r);

            var picked = new HashSet<Reaction>();
            for (int i = 0; i < count && pool.Count > 0; i++)
            {
                int idx = UnityEngine.Random.Range(0, pool.Count);
                picked.Add(pool[idx]);
                pool.RemoveAt(idx);
            }
            return picked;
        }

        public override string Description =>
            (TargetSelf ? "자신" : "대상") + $" 리액션 봉인 ({Kind}, {DurationTurns}턴)";

        public override ReactionEffect Clone() => new SealReactionEffect
        {
            TargetSelf = TargetSelf, Kind = Kind, Count = Count, DurationTurns = DurationTurns
        };
    }

    /// <summary>
    /// 여러 ReactionEffect 를 순차 실행. 리액션 1개당 Effect 가 하나뿐인 제약을 풀어
    /// "행동 + 봉인 + 스트레스" 같은 조합을 한 리액션에 담는다.
    /// </summary>
    [Serializable][AddTypeMenu("Composite", -200)]
    public class CompositeReactionEffect : ReactionEffect
    {
        [SerializeReference, SubclassSelector]
        public List<ReactionEffect> Effects = new List<ReactionEffect>();

        public override async UniTask Apply(BaseCharacter reactor, ReactionExecution execution, BattleContext originalContext)
        {
            if (Effects == null) return;
            foreach (var e in Effects)
                if (e != null) await e.Apply(reactor, execution, originalContext);
        }

        public override string Description
        {
            get
            {
                if (Effects == null || Effects.Count == 0) return "Composite(비어 있음)";
                var parts = new List<string>(Effects.Count);
                foreach (var e in Effects) parts.Add(e?.Description ?? "-");
                return string.Join(" + ", parts);
            }
        }

        public override ReactionEffect Clone()
        {
            var clone = new CompositeReactionEffect();
            if (Effects != null)
                foreach (var e in Effects) clone.Effects.Add(e?.Clone());
            return clone;
        }
    }

    /// <summary>
    /// 스트레스(Insanity) 증감. Delta&gt;0 = 증가, Delta&lt;0 = 감소.
    /// 기존 API 재사용 — 증가는 ReduceInsanity(명명과 반대로 +), 감소는 RecoverInsanity.
    /// </summary>
    [Serializable][AddTypeMenu("Modify Stress", -250)]
    public class StressReactionEffect : ReactionEffect
    {
        [Tooltip("스트레스 변화량. 양수=증가, 음수=감소.")]
        public int Delta = 10;

        [Tooltip("대상. Self=리액터 자신, Observed=관찰 대상, Caster/Target=원본 행동 기준.")]
        public E_TargetFilter Target = E_TargetFilter.Self;

        public override UniTask Apply(BaseCharacter reactor, ReactionExecution execution, BattleContext originalContext)
        {
            foreach (var t in ResolveTargets(reactor, execution, originalContext))
            {
                if (t == null) continue;
                if (Delta >= 0) t.ReduceInsanity(Delta);   // 기존 명명: ReduceInsanity = 스트레스 증가
                else t.RecoverInsanity(-Delta);
            }
            return UniTask.CompletedTask;
        }

        private IEnumerable<BaseCharacter> ResolveTargets(BaseCharacter reactor, ReactionExecution execution, BattleContext context)
        {
            switch (Target)
            {
                case E_TargetFilter.Caster: yield return context.caster; yield break;
                case E_TargetFilter.Target:  yield return context.target;  yield break;
                case E_TargetFilter.Observed:
                    if (execution.MatchedSubjects != null)
                        foreach (var s in execution.MatchedSubjects) yield return s;
                    yield break;
                default: // Self / None → 리액터
                    yield return reactor;
                    yield break;
            }
        }

        public override string Description => $"{Target} 스트레스 {(Delta >= 0 ? "+" : "")}{Delta}";

        public override ReactionEffect Clone() => new StressReactionEffect { Delta = Delta, Target = Target };
    }

    /// <summary>
    /// 리액터를 자기 진영의 전열(Front, index 0) 또는 후열(Back, index 3)로 이동.
    /// 대상 슬롯이 차 있으면 SwapFormation 으로 교체된다(다키스트 스타일).
    /// </summary>
    [Serializable][AddTypeMenu("Move Formation", -240)]
    public class FormationMoveReactionEffect : ReactionEffect
    {
        public enum Where { Front, Back }

        [Tooltip("이동 위치. Front=최전방(0), Back=최후방(3)")]
        public Where To = Where.Back;

        public override UniTask Apply(BaseCharacter reactor, ReactionExecution execution, BattleContext originalContext)
        {
            reactor?.MoveToFormationIndex(To == Where.Front ? 0 : 3);
            return UniTask.CompletedTask;
        }

        public override string Description => To == Where.Front ? "전열로 이동" : "후열로 이동";

        public override ReactionEffect Clone() => new FormationMoveReactionEffect { To = To };
    }
}
