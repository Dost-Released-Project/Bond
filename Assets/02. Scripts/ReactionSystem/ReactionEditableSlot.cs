using System;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;

namespace Reactions
{
    /// <summary>
    /// 리액션 정의(ReactionDefinitionSO)에서 플레이어가 런타임에 채우는 "빈칸"의 서술자.
    /// 디자이너가 인스펙터에서 어떤 부분을 편집 가능하게 둘지 지정한다.
    /// 순수 데이터(+검증/적용 헬퍼)만 담는다 — 후보 선택 UI 렌더링은 상위 UI 레이어 책임.
    /// </summary>
    [Serializable]
    public abstract class ReactionEditableSlot
    {
        [Tooltip("플레이어 UI에 표시할 라벨 (예: 관찰 대상, 사용할 방어 스킬)")]
        public string Label;

        /// <summary>이 슬롯이 runtime reaction 에서 채워졌는지 (= 작동 가능 조건의 일부).</summary>
        public abstract bool IsFilled(Reaction reaction);

        /// <summary>편집값을 미설정 상태로 되돌린다 (인스턴스화 직후 호출).</summary>
        public abstract void Clear(Reaction reaction);

        /// <summary>저작/디버그용 설명.</summary>
        public abstract string Description { get; }
    }

    /// <summary>
    /// 관찰 대상(파티 내 아군)을 플레이어가 지정 → Reaction.SubjectCharacterId 를 채운다.
    /// 이 슬롯을 쓰는 정의의 Template.ObserveFilter 는 Specific 이어야 의미가 맞는다.
    /// 후보(파티 아군) 열거는 파티 로스터가 필요하므로 UI 레이어에서 수행 — 여기선 적용/검증만.
    /// </summary>
    [Serializable][AddTypeMenu("관찰 대상 (아군 지정)", -1000)]
    public class ObserveTargetEditableSlot : ReactionEditableSlot
    {
        [Tooltip("후보에서 자신을 제외할지")]
        public bool ExcludeSelf = true;

        public void Apply(Reaction reaction, string allyId)
        {
            if (reaction == null) return;
            reaction.SubjectCharacterId = allyId;
        }

        public override bool IsFilled(Reaction reaction)
            => reaction != null && !string.IsNullOrEmpty(reaction.SubjectCharacterId);

        public override void Clear(Reaction reaction)
        {
            if (reaction != null) reaction.SubjectCharacterId = null;
        }

        public override string Description =>
            ExcludeSelf ? "관찰 대상: 자신을 제외한 파티 아군 중 지정" : "관찰 대상: 파티 아군 중 지정";
    }

    /// <summary>
    /// 행동 스킬을 플레이어가 지정 → SkillCastReactionEffect.SkillIndex 를 채운다.
    /// 후보는 reactor 가 장착한 스킬(BaseCharacter.Skills) 중 AllowedTypes 에 속하는 것.
    /// 이 슬롯을 쓰는 정의의 Template.BaseEffect 는 SkillCastReactionEffect 여야 한다.
    /// </summary>
    [Serializable][AddTypeMenu("행동 스킬 (장착 스킬 지정)", -900)]
    public class ActionSkillEditableSlot : ReactionEditableSlot
    {
        [Tooltip("선택 가능한 스킬 타입 제약. 비우면 모든 타입 허용.")]
        public List<SkillType> AllowedTypes = new List<SkillType> { SkillType.DEFENSIVE };

        public bool IsSkillAllowed(SkillBase skill)
        {
            if (skill?.Data == null) return false;
            if (AllowedTypes == null || AllowedTypes.Count == 0) return true;
            return AllowedTypes.Contains(skill.Data.Type);
        }

        /// <summary>reactor 의 장착 스킬 중 제약을 통과하는 (index, skill) 후보를 열거.</summary>
        public IEnumerable<(int index, SkillBase skill)> ResolveCandidates(BaseCharacter reactor)
        {
            if (reactor?.Skills == null) yield break;
            for (int i = 0; i < reactor.Skills.Length; i++)
            {
                var s = reactor.Skills[i];
                if (s != null && IsSkillAllowed(s)) yield return (i, s);
            }
        }

        public void Apply(Reaction reaction, int skillIndex)
        {
            if (reaction?.BaseEffect is SkillCastReactionEffect cast)
                cast.SkillIndex = skillIndex;
        }

        public override bool IsFilled(Reaction reaction)
            => reaction?.BaseEffect is SkillCastReactionEffect cast && cast.SkillIndex >= 0;

        public override void Clear(Reaction reaction)
        {
            if (reaction?.BaseEffect is SkillCastReactionEffect cast) cast.SkillIndex = -1;
        }

        public override string Description
        {
            get
            {
                if (AllowedTypes == null || AllowedTypes.Count == 0) return "행동: 장착한 스킬 중 지정";
                return $"행동: 장착한 {string.Join("/", AllowedTypes)} 스킬 중 지정";
            }
        }
    }
}
