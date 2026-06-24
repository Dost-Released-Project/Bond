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
    public class BattleScope : LifetimeScope
    {
        [SerializeField] 
        private BattlePipeLineSo battlePipeLineSo;
        protected override void Configure(IContainerBuilder builder)
        {
            builder.RegisterEntryPoint<BattleManager>(Lifetime.Singleton).As<IBattleManager>();
            builder.Register<ReactionSystem>(Lifetime.Singleton);
            builder.RegisterInstance(battlePipeLineSo).As<IBattlePipeLine>();
        }
    }
}
