using System.Collections.Generic;
using System.Linq;
using BattleSystem;
using PipeLine.PipeLineBase;
using UnityEngine;
using Reactions;

namespace PipeLine
{
    // [D] Runtime Data: 전투 계산 및 연출 상태를 담는 컨텍스트
    public class BattleContext
    {
        public BaseCharacter caster;
        public int targetMask => (runtimeSkill.Data.Target == SkillTarget.Enemy)?
            runtimeSkill.Data.EnemyTargetMask : runtimeSkill.Data.AllyTargetMask;
        public SkillBase runtimeSkill; // 캐릭터의 스탯, 장비, 버프 등이 적용된 스킬
        
        public bool isCritical;
        public bool isEvaded;

        public float value;
        
        public List<BaseCharacter> targets = new List<BaseCharacter>();
        public IReadOnlyList<ReactionExecution> reactions = null;
        public Dictionary<BaseCharacter, int> targetDamageMap = new Dictionary<BaseCharacter, int>();

        public BattleContext(BaseCharacter caster, SkillBase usedSkill, bool isCritical)
        {
            this.caster = caster;
            this.runtimeSkill = usedSkill;
            this.isCritical = isCritical;
        }
    }

    // 혹시 동일한 Type이 들어가는 파이프라인이 여러 개 생길 수 있으므로, 인터페이스로 구분해줌
    public interface IBattlePipeLine : IPipeLine<BattleContext>
    {
        public void SetReactionSystem(ReactionSystem reactionSystem);
    }

    [CreateAssetMenu(fileName = "BattlePipeLineSO", menuName = "PipeLine/BattlePipeLineSO")]
    public class BattlePipeLineSo : PipeLineSo<BattleContext>, IBattlePipeLine
    {
        protected override bool ShouldBreak(BattleContext context)
        {
            // 회피가 성공했다면 이후 데미지/크리티컬 계산 등을 수행하지 않고 중단합니다.
            return context.isEvaded;
        }

        public void SetReactionSystem(ReactionSystem reactionSystem)
        {
            List<ReactionCall> allReactionCalls = steps.OfType<ReactionCall>().ToList();
            foreach (var cs in allReactionCalls)
            {
                cs.SetReactionSystem(reactionSystem);
            }
        }
    }
    #region [L] Logic Steps (구현부 대기)

    [System.Serializable]
    public class EntryStep : IPipeLineStep<BattleContext>
    {
        public BattleContext Execute(BattleContext context)
        {
            Debug.Log("EntryStep");
            //시전자의 스탯과 스킬의 수치를 결합하는 로직 자유롭게 수정 가능
            //지금은 테스트 용으로 간단하게만 만들어놨음
            float characterStat = context.caster.Stat.atk;
            float skillValue = context.runtimeSkill.Data.Value;
            
            context.value = characterStat + skillValue;
            return context;
        }
    }

    [System.Serializable]
    public class EvasionStep : IPipeLineStep<BattleContext>
    {
        public BattleContext Execute(BattleContext context)
        {
            Debug.Log($"Executing EvasionStep (isEvaded: {context.isEvaded})");
            // TODO: 공격자의 명중률과 방어자의 회피 스탯을 비교하여 회피 여부(isEvaded)를 결정하는 로직이 추가되어야 합니다.
            return context;
        }
    }

    [System.Serializable]
    public class CriticalStep : IPipeLineStep<BattleContext>
    {
        public float criticalBonus = 0.5f;
        public BattleContext Execute(BattleContext context)
        {
            Debug.Log("CriticalStep");
            // 크리티컬 판단은 캐릭터에서 해서 넘어옴
            if (context.isCritical)
            {
                float bonus = context.value * criticalBonus;
                context.value += bonus;
            }
            return context;
        }
    }

    [System.Serializable]
    public class DefenseStep : IPipeLineStep<BattleContext>
    {
        public BattleContext Execute(BattleContext context)
        {
            Debug.Log("Executing DefenseStep");
            // 리액션 콜 하기전에 개별 데미지 적용 로직
            context.targetDamageMap.Clear();
            foreach (var target in context.targets)
            {
                if (target == null || target.IsDead) continue;
                int calculatedDamage = Mathf.Max(0, Mathf.RoundToInt(context.value - target.Stat.def));
                context.targetDamageMap[target] = calculatedDamage;
            }
            return context;
        }
    }
    
    [System.Serializable]
    public class ReactionCall : IPipeLineStep<BattleContext>
    {
        private ReactionSystem reactionSystem;

        public void SetReactionSystem(ReactionSystem reactionSystem)
        {
            this.reactionSystem = reactionSystem;
        }
        public BattleContext Execute(BattleContext context)
        {
            Debug.Log("Executing ReactionCall");
            // TODO: 리액션 시스템 콜 새로운 BattleContext를 생성해서 전투 파이프라인에 들어와야합니다.
            if (reactionSystem != null)
            {
                context.reactions = reactionSystem.Resolve(context);
                foreach (var reaction in context.reactions)
                {
                    Debug.Log($"<color=yellow>Reaction: {reaction}</color>");
                }
            }
            return context;
        }
    }
    
    [System.Serializable]
    public class ApplyStep : IPipeLineStep<BattleContext>
    {
        public BattleContext Execute(BattleContext context)
        {
            Debug.Log("ApplyStep");
            // 적용 로직
            foreach (var pair in context.targetDamageMap)
            {
                var target = pair.Key;
                int damage = pair.Value;
                
                target.ReduceHP(damage);
                //TODO 연출 로직 추가해야함
            }
            return context;
        }
    }

    #endregion
}
