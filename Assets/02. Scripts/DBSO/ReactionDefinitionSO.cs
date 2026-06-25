using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace Reactions
{
    /// <summary>
    /// 한 개의 "선택 가능한 리액션" 정의. 디자이너가 인스펙터에서 저작한다.
    /// - Role         : 이 리액션을 선택할 수 있는 역할
    /// - Template     : Phase/ObserveFilter/Trigger/Effect 가 저작된 고정 골격
    /// - EditableSlots: 플레이어가 런타임에 채우는 빈칸(관찰대상/행동스킬 등)
    /// 런타임 Reaction 은 CreateRuntimeReaction() 으로 Template 의 깊은 복제로 생성된다.
    /// 조회: DBSORegistry.QuerySO&lt;ReactionDefinitionSO&gt;(d => d.Role == role)
    /// </summary>
    [CreateAssetMenu(fileName = "ReactionDefinition", menuName = "Bond/Reactions/Reaction Definition")]
    public class ReactionDefinitionSO : BaseSO
    {
        [Header("역할")]
        [Tooltip("이 리액션을 선택할 수 있는 역할")]
        public RoleType Role = RoleType.None;

        [Header("고정 골격 (Template)")]
        [Tooltip("Phase/ObserveFilter/SubjectCharacterId/Trigger/Effect 저작. 편집 가능 슬롯이 채울 값(SubjectCharacterId, SkillIndex 등)은 비워 둔다.")]
        public Reaction Template = new Reaction();

        [Header("편집 가능한 빈칸")]
        [Tooltip("플레이어가 런타임에 지정하는 부분. 비어 있으면 완전 고정 리액션.")]
        [SerializeReference, SubclassSelector]
        public List<ReactionEditableSlot> EditableSlots = new List<ReactionEditableSlot>();

        [Header("3분할 표시 문구 오버라이드 (비우면 자동 파생)")]
        [Tooltip("대상 칸 문구. 비우면 관찰 편집슬롯 유무/ObserveFilter 에서 파생. 예) \"지정 아군이\"")]
        public string TargetTextOverride;
        [Tooltip("조건 칸 문구. 비우면 Template.Trigger 설명에서 파생. 예) \"공격 받을때\"")]
        public string ConditionTextOverride;
        [Tooltip("행동 칸 문구. 비우면 행동 편집슬롯 유무/BaseEffect 설명에서 파생. 예) \"대신 맞는다.\"")]
        public string ActionTextOverride;

        /// <summary>관찰 대상(아군 지정) 편집슬롯을 갖는가 — 대상 칸 편집 가능 여부.</summary>
        public bool HasObserveEditable => EditableSlots != null && EditableSlots.OfType<ObserveTargetEditableSlot>().Any();

        /// <summary>행동 스킬 편집슬롯을 갖는가 — 행동 칸 편집 가능 여부.</summary>
        public bool HasActionEditable => EditableSlots != null && EditableSlots.OfType<ActionSkillEditableSlot>().Any();

        /// <summary>
        /// 3분할(대상/조건/행동) 표시 문구. 오버라이드가 있으면 그것을, 없으면 Template/EditableSlots 에서 파생한다.
        /// 텍스트는 할당 여부와 무관 — 할당은 색·아이콘만 바꾸므로 여기선 항상 서술 문구를 돌려준다.
        /// </summary>
        public (string target, string condition, string action) ResolvePartTexts()
        {
            string target = !string.IsNullOrEmpty(TargetTextOverride)
                ? TargetTextOverride
                : (HasObserveEditable ? "지정 아군" : DescribeObserveFilter(Template?.ObserveFilter ?? E_ObserveFilter.Self));

            string condition = !string.IsNullOrEmpty(ConditionTextOverride)
                ? ConditionTextOverride
                : (Template?.Trigger?.Description ?? "—");

            string action = !string.IsNullOrEmpty(ActionTextOverride)
                ? ActionTextOverride
                : (HasActionEditable ? "행동 선택" : (Template?.BaseEffect?.Description ?? "—"));

            return (target, condition, action);
        }

        private static string DescribeObserveFilter(E_ObserveFilter f) => f switch
        {
            E_ObserveFilter.Self      => "자신",
            E_ObserveFilter.Ally      => "아군 전체",
            E_ObserveFilter.OtherAlly => "다른 아군",
            E_ObserveFilter.Enemy     => "적",
            E_ObserveFilter.Specific  => "지정 대상",
            _                         => "—",
        };

        /// <summary>
        /// Template 의 깊은 복제로 런타임 Reaction 생성. DefinitionId 를 stamp 하고,
        /// 편집 가능 슬롯 값은 미설정(Clear) 상태로 시작한다.
        /// </summary>
        public Reaction CreateRuntimeReaction()
        {
            var r = Template != null ? Template.Clone() : new Reaction();
            r.DefinitionId = Id;
            if (EditableSlots != null)
                foreach (var slot in EditableSlots)
                    slot?.Clear(r);
            return r;
        }

        /// <summary>모든 편집 가능 슬롯이 채워졌는지 (= 작동 가능 여부). 빈칸이 없으면 true.</summary>
        public bool AllEditablesFilled(Reaction reaction)
        {
            if (EditableSlots == null) return true;
            foreach (var slot in EditableSlots)
                if (slot != null && !slot.IsFilled(reaction)) return false;
            return true;
        }

#if UNITY_EDITOR
        protected override void OnValidate()
        {
            base.OnValidate();
            if (Template == null) return;

            if (Template.Phase == E_ReactionPhase.None)
                Debug.LogWarning($"[ReactionDefinition] '{name}' Template.Phase 가 None — 발화하지 않는다. PreApply/PostApply 지정 필요.", this);

            if (Template.BaseEffect == null)
                Debug.LogWarning($"[ReactionDefinition] '{name}' Template.BaseEffect 가 비어 있다 — 평상시 행동이 없다.", this);

            if (EditableSlots == null) return;
            foreach (var slot in EditableSlots)
            {
                if (slot is ObserveTargetEditableSlot && Template.ObserveFilter != E_ObserveFilter.Specific)
                    Debug.LogWarning($"[ReactionDefinition] '{name}' 관찰대상 편집슬롯이 있으나 Template.ObserveFilter 가 Specific 이 아니다 ({Template.ObserveFilter}).", this);
                if (slot is ActionSkillEditableSlot && (Template.BaseEffect is SkillCastReactionEffect) == false)
                    Debug.LogWarning($"[ReactionDefinition] '{name}' 행동스킬 편집슬롯이 있으나 Template.BaseEffect 가 SkillCastReactionEffect 가 아니다.", this);
            }
        }
#endif
    }
}
