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
        Self,       // 리액터 자신 (효과 대상 지정용. 조건 필터로는 미사용)
        FrontmostEnemy, // 가장 가까운(최전열) 적 — 효과 대상 지정용
        BackmostEnemy,  // 가장 먼(최후열) 적 — 효과 대상 지정용
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

        /// <summary>
        /// 저작 도구(빌더)용: 조건들을 Additional 목록에 추가하고 자신을 반환한다.
        /// Additional 은 인스펙터에 노출되므로 생성 후에도 확인·수정이 쉽다. (런타임은 Essential+Additional 모두 평가)
        /// </summary>
        public Trigger WithConditions(params ICondition[] conditions)
        {
            if (conditions != null) _condition.Additional.AddRange(conditions);
            return this;
        }

        public bool CheckCondition(BaseCharacter subject, BattleContext context)
        {
            return Conditions.Any() && Conditions.All(condition => condition.IsMet(new ReactionTriggerConditionArgs() { Subject = subject, BattleContext = context }));
        }

        /// <summary>
        /// 조건 리스트(Essential/Additional)까지 독립 복사한 새 Trigger 반환.
        /// 주의: GetHitTrigger 등 서브클래스를 Clone 하면 동일 조건을 가진 base Trigger 가 나온다
        /// — 런타임은 Conditions(=Essential+Additional)만 읽으므로 동작 동일, 구체 타입은 보존 안 됨.
        /// </summary>
        public Trigger Clone()
        {
            var clone = new Trigger();
            clone._condition.Essential  = CopyConditions(_condition.Essential);
            clone._condition.Additional = CopyConditions(_condition.Additional);
            return clone;

            static List<ICondition> CopyConditions(List<ICondition> src)
            {
                var list = new List<ICondition>(src?.Count ?? 0);
                if (src != null)
                    foreach (var c in src)
                        list.Add((c as ReactionTriggerCondition)?.Copy() ?? c);
                return list;
            }
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