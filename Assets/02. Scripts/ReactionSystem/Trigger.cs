using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using PipeLine;
using UnityEngine;
using UnityEngine.Serialization;

namespace Reactions
{
    public enum E_ReactionPhase
    {
        None,      // 미지정 — 발화하지 않음 (의식적 선택 강제)
        PreApply,  // ApplyStep 이전 (원본 행동에 영향)
        PostApply, // ApplyStep 이후 (원본 행동의 후속)
    }

    public enum E_ObserveFilter
    {
        Self,
        Ally,       // 자신 포함 같은 진영 전체
        OtherAlly,  // 자신을 제외한 같은 진영
        Enemy,
        Specific
    }

    public enum E_TargetFilter
    {
        None,
        Caster,
        Target,
        Observed,   // 조건을 만족시킨 관찰 대상
    }
    
    public interface ITrigger
    {
        bool CheckCondition(BaseCharacter subject, BattleContext context);
        string Description { get; }
    }

    [Serializable][AddTypeMenu("Trigger", -1000)]
    public class Trigger : ITrigger
    {
        [Serializable]
        public class Condition
        {
            [HideInInspector]
            public List<ICondition> Essential = new List<ICondition>();
            
            [SerializeReference, SubclassSelector]
            public List<ICondition> Additional = new List<ICondition>();
            
            public List<ICondition> Whole => Essential.Concat(Additional).ToList();
        }

        [SerializeField] protected Condition _condition = new Condition();
        public List<ICondition> Conditions => _condition.Whole;

        public Trigger() { }

        public Trigger(params ICondition[] condition)
        {
            _condition.Essential = condition.ToList();
        }

        public bool CheckCondition(BaseCharacter subject, BattleContext context)
        {
            return Conditions.Any() && Conditions.All(condition => condition.IsMet(new ReactionTriggerConditionArgs() { Subject = subject, BattleContext = context }));
        }

        public string Description
        {
            get
            {
                if (Conditions.Count <= 0) return "Conditions are empty";
                
                StringBuilder sb = new StringBuilder();
                sb.AppendLine(Conditions[0].Description);
                for (int i = 1; i < Conditions.Count; i++)
                {
                    sb.AppendLine($" && {Conditions[i].Description}");
                }
                return $"{sb.ToString()}";
            }
        }
    }

    [Serializable]
    public class GetHitTrigger : Trigger
    {
        public GetHitTrigger()
        {
            _condition.Essential = new List<ICondition>()
            {
                new SubjectCondition(E_TargetFilter.Target),
                new SkillTypeCondition(SkillType.OFFENSIVE, SkillType.SPELL),
                new HitCondition()
            };
        }
    }
}