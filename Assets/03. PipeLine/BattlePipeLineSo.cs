using System.Collections.Generic;
using System.Linq;
using PipeLine.PipeLineBase;
using UnityEngine;
using Reactions;

namespace PipeLine
{
    // [D] Runtime Data: 전투 계산 및 연출 상태를 담는 컨텍스트
    public class BattleContext
    {
        public BaseCharacter caster;
        public BaseCharacter[] targets;
        public SkillBase runtimeSkill; // 캐릭터의 스탯, 장비, 버프 등이 적용된 스킬
        
        public bool isCritical;
        public bool isEvaded;
        
        public IReadOnlyList<ReactionExecution> reactions = null;
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
            Debug.Log("Executing EntryStep");
            // TODO: 공격자의 스탯 정보를 바탕으로 기본 데미지를 설정하는 로직이 추가되어야 합니다.
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
        public BattleContext Execute(BattleContext context)
        {
            Debug.Log("Executing CriticalStep");
            // TODO: 공격자의 크리티컬 확률을 계산하고 데미지에 배율을 적용하는 로직이 추가되어야 합니다.
            return context;
        }
    }

    [System.Serializable]
    public class DefenseStep : IPipeLineStep<BattleContext>
    {
        public BattleContext Execute(BattleContext context)
        {
            Debug.Log("Executing DefenseStep");
            // TODO: 방어자의 방어력 수치를 기반으로 최종 데미지를 감쇄시키는 로직이 추가되어야 합니다.
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

    #endregion
}
