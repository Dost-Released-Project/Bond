using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace Reactions
{
    public enum ReactionSource
    {
        Role,
        Trait
    }

    [Serializable]
    public class ReactionExecution : IComparable<ReactionExecution>
    {
        public BaseCharacter Agent;
        public IReadOnlyList<BaseCharacter> MatchedSubjects;
        public Reaction Reaction;
        public ReactionResult Result = ReactionResult.Success;

        public ReactionExecution(BaseCharacter agent, Reaction reaction, ReactionResult result, IReadOnlyList<BaseCharacter> matchedSubjects)
        {
            Agent = agent;
            Reaction = reaction;
            Result = result;
            MatchedSubjects = matchedSubjects;
        }

        public int CompareTo(ReactionExecution other) => other.Agent.Speed.CompareTo(Agent.Speed);

        public override string ToString()
        {
            var subjects = (MatchedSubjects == null || MatchedSubjects.Count == 0)
                ? "-"
                : string.Join(", ", MatchedSubjects.Select(s => s?.Name ?? "?"));
            return $"Agent: {Agent.Name}\n" +
                   $"Matched Subject: {subjects}\n" +
                   $"Trigger: {Reaction.Trigger.Description}";
        }
    }

    [Serializable]
    public class Reaction
    {
        public E_ReactionPhase Phase = E_ReactionPhase.None;
        public E_ObserveFilter ObserveFilter = E_ObserveFilter.Self;
        [Tooltip("ObserveFilter == Specific 일 때만 사용")]
        public string SubjectCharacterId;
        [Tooltip("이 런타임 리액션이 유래한 ReactionDefinitionSO.Id. 슬롯 간 중복 방지/표시용. 인스펙터에서 직접 저작한 경우 비어 있음.")]
        public string DefinitionId;
        [SerializeReference, SubclassSelector] public ITrigger Trigger;
        [SerializeReference, SubclassSelector] public ReactionEffect Effect;

        /// <summary>
        /// 깊은 복제. Trigger/Effect/조건까지 독립 인스턴스로 복사해
        /// 정의(템플릿)로부터 캐릭터별 런타임 리액션을 만들 때 공유 참조가 생기지 않게 한다.
        /// </summary>
        public Reaction Clone()
        {
            var clone = new Reaction
            {
                Phase              = Phase,
                ObserveFilter      = ObserveFilter,
                SubjectCharacterId = SubjectCharacterId,
                DefinitionId       = DefinitionId,
                Effect             = Effect?.Clone(),
            };
            // Trigger 가 Trigger 타입이 아닌 다른 ITrigger 구현이면 참조 공유되지만, 현재 구현체는 Trigger 뿐이라 무방.
            clone.Trigger = Trigger is Trigger t ? t.Clone() : Trigger;
            return clone;
        }
    }
}
