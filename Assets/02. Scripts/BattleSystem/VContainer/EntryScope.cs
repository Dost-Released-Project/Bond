using BattleSystem.Interface;
using UnityEngine;
using UnityEngine.Serialization;
using VContainer;
using VContainer.Unity;

namespace BattleSystem.VContainer
{
    public class EntryScope : LifetimeScope
    {
        protected override void Configure(IContainerBuilder builder)
        {
            builder.RegisterEntryPoint<BattleStageEntry>(Lifetime.Singleton).As<IBattleStageEntry>();
        }
    }
}
