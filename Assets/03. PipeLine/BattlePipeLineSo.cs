using System.Collections.Generic;
using System.Linq;
using BattleSystem;
using Cysharp.Threading.Tasks;
using PipeLine.PipeLineBase;
using UnityEngine;
using Reactions;

namespace PipeLine
{
    // [D] Runtime Data: 전투 계산 및 연출 상태를 담는 컨텍스트
    public class BattleContext
    {
        public BaseCharacter caster;
        public BaseCharacter target; // 개별 타겟
        public int targetMask => (runtimeSkill.Data.Target == SkillTarget.Enemy)?
            runtimeSkill.Data.EnemyTargetMask : runtimeSkill.Data.AllyTargetMask;
        public SkillBase runtimeSkill; // 캐릭터의 스탯, 장비, 버프 등이 적용된 스킬
        
        public bool isCritical;
        public bool isEvaded;
        public bool isReaction;

        public float value;
        
        public IReadOnlyList<ReactionExecution> reactions = null;

        // BaseCharacter가 처음에 생성할 때 사용하는 생성자
        public BattleContext(BaseCharacter caster, SkillBase usedSkill, bool isCritical)
        {
            this.caster = caster;
            this.runtimeSkill = usedSkill;
            this.isCritical = isCritical;
        }

        // BattleManager에서 개별 타겟용으로 복제할 때 사용하는 생성자
        public BattleContext(BattleContext origin, BaseCharacter target)
        {
            this.caster = origin.caster;
            this.runtimeSkill = origin.runtimeSkill;
            this.isCritical = origin.isCritical;
            this.target = target;
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
        public UniTask<BattleContext> Execute(BattleContext context)
        {
            // 시전자의 스탯과 스킬의 수치를 결합하는 로직 (SkillType에 따라 유연하게 참조)
            float characterStat = 0f;
            SkillType skillType = context.runtimeSkill.Data.Type;
            Stat casterStat = context.caster.Stat;

            switch (skillType)
            {
                case SkillType.OFFENSIVE:
                    characterStat = casterStat.atk;
                    Debug.Log($"[EntryStep] OFFENSIVE 타입 스킬 발동 - 시전자 물리 공격력(atk) 참조: {characterStat}");
                    break;
                case SkillType.SPELL:
                    characterStat = casterStat.Sp_Atk;
                    Debug.Log($"[EntryStep] SPELL 타입 스킬 발동 - 시전자 주문 공격력(Sp_Atk) 참조: {characterStat}");
                    break;
                case SkillType.SUPPORT:
                    characterStat = casterStat.INT;
                    Debug.Log($"[EntryStep] SUPPORT 타입 스킬 발동 - 시전자 지력(INT) 참조: {characterStat}");
                    break;
                case SkillType.DEFENSIVE:
                    characterStat = casterStat.def;
                    Debug.Log($"[EntryStep] DEFENSIVE 타입 스킬 발동 - 시전자 방어력(def) 참조: {characterStat}");
                    break;
                default:
                    Debug.LogWarning($"[EntryStep] 정의되지 않은 SkillType({skillType}) 입니다. 스탯 수치 0으로 계산됩니다.");
                    break;
            }

            float skillValue = context.runtimeSkill.Data.Value;
            context.value = characterStat + skillValue;
            
            Debug.Log($"[EntryStep] 최종 산출 수치: {context.value} (스탯 {characterStat} + 스킬 위력 {skillValue})");
            return UniTask.FromResult(context);
        }
    }

    [System.Serializable]
    public class EvasionStep : IPipeLineStep<BattleContext>
    {
        public UniTask<BattleContext> Execute(BattleContext context)
        {
            if (context.target == null || context.target.IsDead) return UniTask.FromResult(context);
            
            // 지원/힐 스킬은 회피 판정을 생략
            if (context.runtimeSkill.Data.Type == SkillType.SUPPORT)
            {
                context.isEvaded = false;
                return UniTask.FromResult(context);
            }

            float hitRate = context.caster.Stat.acc - (context.target.Stat.AGI * 0.5f);
            context.isEvaded = Random.Range(0f, 100f) > hitRate;
            
            Debug.Log($"[EvasionStep] 타겟: {context.target.Name}, 명중률: {hitRate}%, 회피 발생 여부: {context.isEvaded}");
            return UniTask.FromResult(context);
        }
    }

    [System.Serializable]
    public class CriticalStep : IPipeLineStep<BattleContext>
    {
        public float criticalBonus = 0.5f;
        public UniTask<BattleContext> Execute(BattleContext context)
        {
            if (context.isEvaded || context.target == null || context.target.IsDead) return UniTask.FromResult(context);

            // TODO: 개별 타겟 치명타 확률 로직 (현재는 임시로 시전자 crt 사용)
            //context.isCritical = Random.Range(0f, 100f) < context.caster.Stat.crt;
            context.isCritical = true;
            
            if (context.isCritical)
            {
                float bonus = context.value * criticalBonus;
                context.value += bonus;
                Debug.Log($"[CriticalStep] 크리티컬 발생! 보너스: {bonus}, 타겟: {context.target.Name}");
            }
            return UniTask.FromResult(context);
        }
    }

    [System.Serializable]
    public class DefenseStep : IPipeLineStep<BattleContext>
    {
        public UniTask<BattleContext> Execute(BattleContext context)
        {
            if (context.isEvaded || context.target == null || context.target.IsDead) return UniTask.FromResult(context);

            if (context.runtimeSkill.Data.Type == SkillType.SUPPORT)
            {
                Debug.Log($"[DefenseStep] SUPPORT 타입이므로 방어력 차감 생략 (타겟: {context.target.Name})");
            }
            else
            {
                int def = context.target.Stat.def;
                context.value = Mathf.Max(0, context.value - def);
                Debug.Log($"[DefenseStep] 방어력({def}) 차감 후 최종 수치: {context.value} (타겟: {context.target.Name})");
            }
            
            return UniTask.FromResult(context);
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
        
        public async UniTask<BattleContext> Execute(BattleContext context)
        {
            if (context.isEvaded || context.target == null || context.target.IsDead) return context;

            Debug.Log("Executing ReactionCall");
            if (reactionSystem != null)
            {
                var executions = reactionSystem.Resolve(context);
                foreach (var execution in executions)
                {
                    Debug.Log($"<color=yellow>Reaction: {execution.ToString()}</color>");
                    await execution.Agent.ExecuteReaction(execution.Reaction, context);
                }
            }
            return context;
        }
    }
    
    [System.Serializable]
    public class ApplyStep : IPipeLineStep<BattleContext>
    {
        public UniTask<BattleContext> Execute(BattleContext context)
        {
            if (context.isEvaded || context.target == null || context.target.IsDead) return UniTask.FromResult(context);

            int finalAmount = Mathf.RoundToInt(context.value);

            if (context.runtimeSkill.Data.Type == SkillType.SUPPORT)
            {
                context.target.RecoverHp(finalAmount);
                Debug.Log($"[ApplyStep] 타겟 {context.target.Name}에게 {finalAmount}만큼 회복 적용");
            }
            else
            {
                context.target.ReduceHP(finalAmount);
                Debug.Log($"[ApplyStep] 타겟 {context.target.Name}에게 {finalAmount} 데미지 적용");
            }

            //TODO 연출 로직 추가해야함
            return UniTask.FromResult(context);
        }
    }

    #endregion
}