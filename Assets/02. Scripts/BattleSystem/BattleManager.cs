using System;
using _03._PipeLine;
using Bond.Expedition;
using Reactions;
using UnityEngine;
using VContainer;
using VContainer.Unity;

namespace _02._Scripts.BattleSystem
{
    public class BattleManager : IBattleManager, IStartable, IDisposable
    {
        private readonly ReactionSystem m_reactionSystem;
        private readonly IBattlePipeLine m_skillApplyPipeline;
        private readonly IBattleFlowManager m_expeditionFlowManager;
        
        public BattleManager(ReactionSystem reactionSystem, 
            IBattlePipeLine  skillApplyPipeline, IBattleFlowManager expeditionFlowManager)
        {
            m_expeditionFlowManager = expeditionFlowManager;
            m_reactionSystem = reactionSystem;
            m_skillApplyPipeline = skillApplyPipeline;
            Init();
        }
        
        void IStartable.Start()
        {
            m_expeditionFlowManager.OnBattleStart += StartBattle;
        }

        void IDisposable.Dispose()
        {
            m_expeditionFlowManager.OnBattleStart -= StartBattle;
        }

        private void StartBattle(BaseCharacter[] players, BaseCharacter[] enemies)
        {
            Debug.Log($"BattleManager received battle start event with {players.Length} player units and {enemies.Length} enemy units.");
        }

        private void Init()
        {
            m_skillApplyPipeline.SetReactionSystem(m_reactionSystem);
        }

        public BattleContext SkillApplyLogic(BattleContext context)
        {
            // [입구] 로직 파이프라인 실행
            if (m_skillApplyPipeline != null)
            {
                return m_skillApplyPipeline.Run(context);
            }

            return context;
        }
    }
}
