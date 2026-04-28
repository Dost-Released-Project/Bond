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
        private readonly ReactionSystem _reactionSystem;
        private readonly IBattlePipeLine _skillApplyPipeline;
        private readonly IExpeditionFlowManager _expeditionFlowManager;
        
        public BattleManager(ReactionSystem reactionSystem, 
            IBattlePipeLine  skillApplyPipeline, IExpeditionFlowManager expeditionFlowManager)
        {
            this._expeditionFlowManager = expeditionFlowManager;
            this._reactionSystem = reactionSystem;
            this._skillApplyPipeline = skillApplyPipeline;
            Init();
        }
        
        void IStartable.Start()
        {
            _expeditionFlowManager.OnBattleStart += StartBattle;
        }

        void IDisposable.Dispose()
        {
            _expeditionFlowManager.OnBattleStart -= StartBattle;
        }

        private void StartBattle(BaseCharacter[] players, BaseCharacter[] enemies)
        {
            Debug.Log($"BattleManager received battle start event with {players.Length} player units and {enemies.Length} enemy units.");
        }

        private void Init()
        {
            _skillApplyPipeline.SetReactionSystem(_reactionSystem);
        }

        public BattleContext SkillApplyLogic(BattleContext context)
        {
            // [입구] 로직 파이프라인 실행
            if (_skillApplyPipeline != null)
            {
                return _skillApplyPipeline.Run(context);
            }

            return context;
        }
    }
}
