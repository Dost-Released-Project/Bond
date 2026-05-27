using System;
using System.Collections.Generic;
using BattleSystem;
using PipeLine;
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
        public BaseCharacter MatchedSubject;
        public Reaction Reaction;
        public ReactionResult Result = ReactionResult.Success;

        public ReactionExecution(BaseCharacter agent, Reaction reaction, ReactionResult result, BaseCharacter matchedSubject)
        {
            Agent = agent;
            Reaction = reaction;
            Result = result;
            MatchedSubject = matchedSubject;
        }

        public int CompareTo(ReactionExecution other) => other.Agent.Speed.CompareTo(Agent.Speed);

        public override string ToString()
        {
            return $"Agent: {Agent.Name}\n" +
                   $"Matched: {MatchedSubject?.Name ?? "-"}\n" +
                   $"Trigger: {Reaction.Trigger.Description}";
        }
    }

    [Serializable]
    public class Reaction
    {
        public E_ObserveFilter ObserveFilter = E_ObserveFilter.Self;
        public string SubjectCharacterId; // ObserveFilter == Specific 일 때만 사용
        [SerializeReference, SubclassSelector] public ITrigger Trigger;
        public int SkillIndex; // 반응으로 실행할 스킬의 인덱스
        public E_TargetFilter ReactionSkillTarget;

        /// <summary>
        /// 후보들 중 조건을 만족하는 모든 캐릭터를 반환합니다. 한 명도 만족하지 않으면 비어있는 시퀀스.
        /// </summary>
        public IEnumerable<BaseCharacter> Match(BaseCharacter owner, BattleContext context, IEnumerable<BaseCharacter> participants)
        {
            if (Trigger == null) yield break;

            foreach (var candidate in ResolveSubjects(owner, participants))
            {
                if (Trigger.CheckCondition(candidate, context))
                    yield return candidate;
            }
        }

        private IEnumerable<BaseCharacter> ResolveSubjects(BaseCharacter owner, IEnumerable<BaseCharacter> participants)
        {
            switch (ObserveFilter)
            {
                case E_ObserveFilter.Self:
                    if (owner != null) yield return owner;
                    yield break;

                case E_ObserveFilter.Specific:
                    if (!string.IsNullOrEmpty(SubjectCharacterId)
                        && BaseCharacter.Dict.TryGetValue(SubjectCharacterId, out var specific))
                        yield return specific;
                    yield break;

                case E_ObserveFilter.Ally:
                case E_ObserveFilter.OtherAlly:
                case E_ObserveFilter.Enemy:
                    var ownerSide = owner?.CurrentSlot?.side;
                    if (ownerSide == null) yield break;

                    foreach (var c in participants)
                    {
                        if (c == null || c.IsDead) continue;
                        var cSide = c.CurrentSlot?.side;
                        if (cSide == null) continue;

                        bool match = ObserveFilter switch
                        {
                            E_ObserveFilter.Ally => cSide == ownerSide,
                            E_ObserveFilter.OtherAlly => cSide == ownerSide && c != owner,
                            E_ObserveFilter.Enemy => cSide != ownerSide,
                            _ => false,
                        };
                        if (match) yield return c;
                    }
                    yield break;
            }
        }
    }
}
