using BattleSystem.Interface;
using PipeLine;
using juno_Test;
using Reactions;
using UnityEngine;
using UnityEngine.Serialization;
using VContainer;
using VContainer.Unity;

namespace BattleSystem.VContainer
{
    /// <summary>
    /// 배틀 씬 전용 VContainer LifetimeScope.
    /// BattleManager, ReactionSystem, IBattlePipeLine, IReactionPortraitCanvas 를 등록한다.
    /// </summary>
    public class BattleScope : LifetimeScope
    {
        [SerializeField]
        private BattlePipeLineSo battlePipeLineSo;
        
        [SerializeField]
        private BattlePipeLineSo tutorialBattlePipeLineSo;

        private bool m_isTutorial;

        /// <summary>
        /// 씬에 배치된 ReactionPortraitCanvas 컴포넌트. Inspector 에서 연결한다.
        /// </summary>
        [SerializeField]
        private ReactionPortraitCanvas reactionPortraitCanvas;

        protected override void Configure(IContainerBuilder builder)
        {
            var payload = Parent?.Container.Resolve<Bond.Expedition.ExpeditionPayload>();
            m_isTutorial = payload != null && payload.IsTutorial;

            builder.RegisterEntryPoint<BattleManager>(Lifetime.Singleton).As<IBattleManager>();
            builder.Register<ReactionSystem>(Lifetime.Singleton);
            if (m_isTutorial)                                                                          
                builder.RegisterInstance(tutorialBattlePipeLineSo).As<IBattlePipeLine>();            
            else                                                                                     
                builder.RegisterInstance(battlePipeLineSo).As<IBattlePipeLine>();      

            // IReactionPortraitCanvas 등록 — BattleManager 생성 시 자동 주입된다
            if (reactionPortraitCanvas != null)
            {
                builder.RegisterComponent(reactionPortraitCanvas).As<IReactionPortraitCanvas>();
            }
            else
            {
                Debug.LogError("[BattleScope] reactionPortraitCanvas 가 연결되지 않았습니다. Inspector 에서 연결하세요.", this);
            }
        }
    }
}
