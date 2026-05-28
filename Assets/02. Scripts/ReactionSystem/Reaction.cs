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
                   $"Matched: {subjects}\n" +
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
        [SerializeReference, SubclassSelector] public ITrigger Trigger;
        [SerializeReference, SubclassSelector] public ReactionEffect Effect;
    }
}
