using _03._PipeLine;
using juno_Test;
using VContainer;
using VContainer.Unity;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.Serialization;

namespace _02._Scripts.BattleSystem_KWT
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
