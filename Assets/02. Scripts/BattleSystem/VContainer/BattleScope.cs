using _03._PipeLine;
using juno_Test;
using Reactions;
using UnityEngine;
using VContainer;
using VContainer.Unity;

namespace _02._Scripts.BattleSystem
{
    public class BattleScope : LifetimeScope
    {
        [SerializeField] 
        private BattlePipeLineSo _BattlePipeLineSo;
        public BaseCharacter[] sceneUnit;
        
        protected override void Configure(IContainerBuilder builder)
        {
            builder.Register<BattleManager>(Lifetime.Singleton).As<IBattleManager>();
            builder.Register<ReactionSystem>(Lifetime.Scoped);
            builder.RegisterInstance(_BattlePipeLineSo).As<IBattlePipeLine>();
            builder.RegisterInstance(sceneUnit);
        }
    }
}
