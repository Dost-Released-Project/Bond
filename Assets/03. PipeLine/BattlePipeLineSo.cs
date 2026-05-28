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

        public BaseCharacter interceptedFor; // Intercept 발동 시 원래 보호받은 캐릭터 (연출용)

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
            //return context.isEvaded;
            // 원래는 저랬는데, 리액션 검증이 회피 시에도 진행해야 함으로 제거

            return false;
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
            //context.isEvaded = Random.Range(0f, 100f) > hitRate;
            context.isEvaded = false;

            if (context.isEvaded)
            {
                Debug.Log($"<color=white><b>[회피]</b></color> {context.target.Name}이(가) {context.caster.Name}의 공격을 피했습니다! (명중률: {hitRate}%)");
            }
            else
            {
                Debug.Log($"[EvasionStep] 타겟: {context.target.Name}, 명중률: {hitRate}%, 회피 발생 여부: {context.isEvaded}");
            }
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
            context.isCritical = false;
            
            if (context.isCritical)
            {
                float bonus = context.value * criticalBonus;
                context.value += bonus;
                Debug.Log($"<color=yellow><b>[치명타]</b></color> 크리티컬 발생! 보너스: {bonus}, 타겟: {context.target.Name}");
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

                if (context.value <= 0)
                {
                    Debug.Log($"<color=gray><b>[무효]</b></color> {context.target.Name}의 방어력({def})에 막혀 데미지가 0이 되었습니다.");
                }
                else
                {
                    Debug.Log($"[DefenseStep] 방어력({def}) 차감 후 최종 수치: {context.value} (타겟: {context.target.Name})");
                }
            }
            
            return UniTask.FromResult(context);
        }
    }
    
    [System.Serializable]
    public class ReactionCall : IPipeLineStep<BattleContext>
    {
        public E_ReactionPhase Phase = E_ReactionPhase.None;

        private ReactionSystem reactionSystem;

        public void SetReactionSystem(ReactionSystem reactionSystem)
        {
            this.reactionSystem = reactionSystem;
        }

        public async UniTask<BattleContext> Execute(BattleContext context)
        {
            if (context.target == null) return context;

            Debug.Log($"<color=lightblue>Executing ReactionCall [{Phase}]</color>");
            if (reactionSystem != null)
            {
                var executions = reactionSystem.Resolve(context, Phase);
                foreach (var execution in executions)
                {
                    Debug.Log($"<color=lightblue>Reaction:\n" +
                              $"{execution.ToString()}</color>");
                    await execution.Agent.ExecuteReaction(execution, context);
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
            if (context.isEvaded || context.target == null || context.target.IsDead) 
            {
                Debug.Log($"[ApplyStep] 타겟이 유효하지 않아(사망 또는 회피) 스킬 적용을 취소합니다.");
                return UniTask.FromResult(context);
            }

            Debug.Log($"<color=cyan><b>[스킬 적용]</b></color> {context.caster.Name} -> {context.target.Name} ({context.runtimeSkill.Data.DisplayName} 발동!)");

            // 다형성을 이용한 스킬 실행 위임
            context.runtimeSkill.UseSkill(context);

            // [V] 연출 로직 (Rule 2 준수)
            // 연출(Visual)은 여기서 직접 호출하지 않으며,
            // context.target.ReduceHP() 등 내부에서 발생하는 데이터 변경 이벤트를 
            // Visualizer나 UIView 레이어가 구독(Observer)하여 수동적으로 처리합니다.
            
            return UniTask.FromResult(context);
        }
    }

    #endregion
}