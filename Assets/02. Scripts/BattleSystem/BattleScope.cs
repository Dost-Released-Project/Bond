using System.Collections.Generic;
using System.Threading;
using Cysharp.Threading.Tasks;
using juno_Test;
using VContainer;
using VContainer.Unity;

namespace _02._Scripts.BattleSystem_KWT
{
    public class BattleScope : LifetimeScope
    {
        public TestPlayer[] sceneUnit;
        protected override void Configure(IContainerBuilder builder)
        {
            builder.Register<BattleManager>(Lifetime.Scoped);
            builder.RegisterInstance(sceneUnit);
        }
    }
}
