using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Reactions;

namespace Reactions.Authoring
{
    /// <summary>
    /// ReactionDefinitionSO 를 코드로 저작하기 위한 플루언트 빌더 (Editor 전용).
    /// 리액션은 고정된 어휘(조건 8종 / 행동 3종 / 슬롯 2종)의 "조합"이므로,
    /// 여기 단축 팩토리들로 어떤 역할·성향 리액션이든 몇 줄로 표현한다.
    ///
    /// 사용 예 (using static Reactions.Authoring.ReactionDefBuilder;):
    ///   Def("RACT_SUP_ATKUP", RoleType.Supporter).Name("지원 공격 강화")
    ///       .Phase(E_ReactionPhase.PreApply).Observe(E_ObserveFilter.Specific)
    ///       .When(SubjectIs(E_TargetFilter.Caster), SkillTypeIs(SkillType.OFFENSIVE))
    ///       .Do(Buff(StatType.DamageMultiplier, 0.3f, turns: 2))
    ///       .Editable(ObserveTarget("지원 대상"))
    ///       .Build("Assets/Data/Reactions");
    /// </summary>
    public class ReactionDefBuilder
    {
        private readonly string _id;
        private readonly RoleType _role;
        private string _displayName;
        private string _description = "";

        private readonly Reaction _template = new Reaction();
        private readonly List<ICondition> _conditions = new List<ICondition>();
        private readonly List<ReactionEditableSlot> _slots = new List<ReactionEditableSlot>();

        private ReactionDefBuilder(string id, RoleType role)
        {
            _id = id;
            _role = role;
            _displayName = id;
        }

        // ── 진입 ──────────────────────────────────────────────
        public static ReactionDefBuilder Def(string id, RoleType role) => new ReactionDefBuilder(id, role);

        // ── 골격 세팅 (체이닝) ────────────────────────────────
        public ReactionDefBuilder Name(string displayName, string description = "")
        {
            _displayName = displayName;
            _description = description ?? "";
            return this;
        }

        public ReactionDefBuilder Phase(E_ReactionPhase phase) { _template.Phase = phase; return this; }
        public ReactionDefBuilder Observe(E_ObserveFilter filter) { _template.ObserveFilter = filter; return this; }
        public ReactionDefBuilder When(params ICondition[] conditions) { if (conditions != null) _conditions.AddRange(conditions); return this; }
        /// <summary>행동을 지정. 여러 개를 주면 CompositeReactionEffect 로 묶는다.</summary>
        public ReactionDefBuilder Do(params ReactionEffect[] effects)
        {
            _template.Effect = (effects == null || effects.Length == 0) ? null
                : effects.Length == 1 ? effects[0]
                : new CompositeReactionEffect { Effects = effects.ToList() };
            return this;
        }
        public ReactionDefBuilder Editable(ReactionEditableSlot slot) { if (slot != null) _slots.Add(slot); return this; }

        // ── 조건 단축 팩토리 ──────────────────────────────────
        public static ICondition SubjectIs(E_TargetFilter filter) => new SubjectCondition(filter);
        public static ICondition SkillTypeIs(params SkillType[] types) => new SkillTypeCondition(types);
        public static ICondition HpBelow(float ratio) => new HpBelowCondition { Threshold = ratio };
        public static ICondition HpAbove(float ratio) => new HpAboveCondition { Threshold = ratio };
        public static ICondition StressAbove(int value = 50) => new StressAboveCondition { Threshold = value };
        public static ICondition PartyStressAbove(float average = 60f) => new PartyStressAverageCondition { Threshold = average };
        public static ICondition Crit() => new CritCondition();
        public static ICondition Evaded() => new EvadeCondition();
        public static ICondition Hit() => new HitCondition();
        public static ICondition Killed() => new KillCondition();
        public static ICondition DamageAtLeast(float absolute)
            => new DamageCondition { Threshold = new DamageCondition.AbsoluteThreshold { Value = absolute } };
        public static ICondition DamageAtLeastMaxHp(float ratio)
            => new DamageCondition { Threshold = new DamageCondition.PercentOfMaxHpThreshold { Ratio = ratio } };

        // ── 행동 단축 팩토리 ──────────────────────────────────
        /// <summary>능력치 버프. value=추가 비율(0.3=+30%). 공격력↑=DamageMultiplier, 방어력↑=DamageReduction.</summary>
        public static ReactionEffect Buff(StatType type, float value, int turns, E_TargetFilter to = E_TargetFilter.Observed, string buffId = null)
            => new BuffReactionEffect
            {
                BuffTarget = to,
                DurationTurns = turns,
                BuffId = buffId,
                Modifiers = new List<StatModifier> { new StatModifier { type = type, mode = ModifierMode.Flat, value = value } }
            };

        public static ReactionEffect Intercept() => new InterceptReactionEffect();

        /// <summary>리액터의 장착 스킬을 발동. skillIndex 가 -1 이면 ActionSkill 편집슬롯으로 런타임에 채운다.</summary>
        public static ReactionEffect CastSkill(E_TargetFilter to, int skillIndex = -1)
            => new SkillCastReactionEffect { SkillTarget = to, SkillIndex = skillIndex };

        /// <summary>리액션 봉인. self=true 면 리액터 자신, false 면 관찰 대상(Observed)을 봉인.</summary>
        public static ReactionEffect Seal(SealKind kind = SealKind.All, int turns = 1, int count = 1, bool self = true)
            => new SealReactionEffect { Kind = kind, DurationTurns = turns, Count = count, TargetSelf = self };

        /// <summary>여러 효과를 순차 실행하는 묶음.</summary>
        public static ReactionEffect Composite(params ReactionEffect[] effects)
            => new CompositeReactionEffect { Effects = effects != null ? effects.ToList() : new List<ReactionEffect>() };

        /// <summary>스트레스 증감. delta&gt;0 증가, delta&lt;0 감소. to=Self 면 리액터 자신.</summary>
        public static ReactionEffect Stress(int delta, E_TargetFilter to = E_TargetFilter.Self)
            => new StressReactionEffect { Delta = delta, Target = to };

        /// <summary>리액터를 후열로 이동.</summary>
        public static ReactionEffect MoveBack() => new FormationMoveReactionEffect { To = FormationMoveReactionEffect.Where.Back };

        /// <summary>리액터를 전열로 이동.</summary>
        public static ReactionEffect MoveFront() => new FormationMoveReactionEffect { To = FormationMoveReactionEffect.Where.Front };

        /// <summary>리액터의 다음 turns 번 자기 턴을 행동 불가로 만든다.</summary>
        public static ReactionEffect SkipTurn(int turns = 1) => new SkipTurnReactionEffect { Turns = turns };

        // ── 편집 슬롯 단축 팩토리 ─────────────────────────────
        public static ReactionEditableSlot ObserveTarget(string label, bool excludeSelf = true)
            => new ObserveTargetEditableSlot { Label = label, ExcludeSelf = excludeSelf };

        public static ReactionEditableSlot ActionSkill(string label, params SkillType[] allowed)
            => new ActionSkillEditableSlot
            {
                Label = label,
                AllowedTypes = (allowed != null && allowed.Length > 0) ? allowed.ToList() : new List<SkillType>()
            };

        // ── 빌드 ──────────────────────────────────────────────
        public ReactionDefinitionSO Build(string folder)
        {
            Validate();

            _template.Trigger = new Trigger().WithConditions(_conditions.ToArray());

            var so = ScriptableObject.CreateInstance<ReactionDefinitionSO>();
            so.Role = _role;
            so.Template = _template;
            so.EditableSlots = _slots;

            ReactionAuthoringIO.SetBaseSoIds(so, _id, _displayName, _description);
            return ReactionAuthoringIO.Persist(so, folder, _id);
        }

        // ReactionDefinitionSO.OnValidate 와 동일한 규칙을 빌드 시점에 미리 검사한다.
        private void Validate()
        {
            if (_template.Phase == E_ReactionPhase.None)
                Debug.LogError($"[ReactionDef:{_id}] Phase 가 None 이라 발화하지 않습니다. PreApply/PostApply 지정 필요.");
            if (_template.Effect == null)
                Debug.LogError($"[ReactionDef:{_id}] Effect 가 비어 있습니다.");
            if (_conditions.Count == 0)
                Debug.LogError($"[ReactionDef:{_id}] 트리거 조건이 하나도 없습니다.");

            if (_slots.Any(s => s is ObserveTargetEditableSlot) && _template.ObserveFilter != E_ObserveFilter.Specific)
                Debug.LogError($"[ReactionDef:{_id}] 관찰대상 편집슬롯이 있으나 ObserveFilter 가 Specific 이 아닙니다 ({_template.ObserveFilter}).");
            if (_slots.Any(s => s is ActionSkillEditableSlot) && (_template.Effect is SkillCastReactionEffect) == false)
                Debug.LogError($"[ReactionDef:{_id}] 행동스킬 편집슬롯이 있으나 Effect 가 CastSkill 이 아닙니다.");
        }
    }
}
