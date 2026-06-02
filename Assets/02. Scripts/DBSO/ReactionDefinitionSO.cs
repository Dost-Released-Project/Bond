using System.Collections.Generic;
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

            if (EditableSlots == null) return;
            foreach (var slot in EditableSlots)
            {
                if (slot is ObserveTargetEditableSlot && Template.ObserveFilter != E_ObserveFilter.Specific)
                    Debug.LogWarning($"[ReactionDefinition] '{name}' 관찰대상 편집슬롯이 있으나 Template.ObserveFilter 가 Specific 이 아니다 ({Template.ObserveFilter}).", this);
                if (slot is ActionSkillEditableSlot && (Template.Effect is SkillCastReactionEffect) == false)
                    Debug.LogWarning($"[ReactionDefinition] '{name}' 행동스킬 편집슬롯이 있으나 Template.Effect 가 SkillCastReactionEffect 가 아니다.", this);
            }
        }
#endif
    }
}
