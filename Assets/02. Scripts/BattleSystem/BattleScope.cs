using System.Collections.Generic;
using _03._PipeLine;
using juno_Test;
using UnityEngine;
using VContainer;
using VContainer.Unity;

namespace _02._Scripts.BattleSystem
{
    public class BattleScope : LifetimeScope
    {
        [SerializeField] 
        private BattlePipeLineSo _BattlePipeLineSo;
        public TestPlayer[] sceneUnit;
        
        protected override void Configure(IContainerBuilder builder)
        {
            builder.Register<BattleManager>(Lifetime.Scoped);
            builder.RegisterInstance(_BattlePipeLineSo).As<IBattlePipeLine>();
            builder.RegisterInstance(sceneUnit);
        }
    }
}
