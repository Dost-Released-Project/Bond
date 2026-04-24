using System;
using _03._PipeLine;
using Reactions;
using UnityEngine;
using VContainer;
using VContainer.Unity;

namespace _02._Scripts.BattleSystem
{
    public class BattleManager : IBattleManager, IStartable, IDisposable
    {
        private readonly ReactionSystem reactionSystem;
        private readonly IBattlePipeLine skillApplyPipeline;
        private readonly IBattleFlowManager battleFlowManager;
        
        public BattleManager(ReactionSystem reactionSystem, 
            IBattlePipeLine  skillApplyPipeline, IBattleFlowManager battleFlowManager)
        {
            this.battleFlowManager = battleFlowManager;
            this.reactionSystem = reactionSystem;
            this.skillApplyPipeline = skillApplyPipeline;
            Init();
        }
        
        public void Start()
        {
            battleFlowManager.OnBattleStart += StartBattle;
        }

        public void Dispose()
        {
            battleFlowManager.OnBattleStart -= StartBattle;
        }

        private void StartBattle(BaseCharacter[] players, BaseCharacter[] enemies)
        {
            Debug.Log($"BattleManager received battle start event with {players.Length} player units and {enemies.Length} enemy units.");
        }

        private void Init()
        {
            skillApplyPipeline.SetReactionSystem(reactionSystem);
        }

        public BattleContext SkillApplyLogic(BattleContext context)
        {
            // [입구] 로직 파이프라인 실행
            if (skillApplyPipeline != null)
            {
                skillApplyPipeline.Run(context);

                foreach (var reaction in context.reactions)
                {
                    SkillApplyLogic(new BattleContext()); // TODO: 리액션을 배틀컨텍스트로 치환한 후에 전달하는 로직
                }
            }

            return context;
        }

        
    }
}
