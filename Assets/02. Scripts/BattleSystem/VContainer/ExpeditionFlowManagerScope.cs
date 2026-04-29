using UnityEngine;
using UnityEngine.Serialization;
using VContainer;
using VContainer.Unity;

namespace _02._Scripts.BattleSystem
{
    public class ExpeditionFlowManagerScope : LifetimeScope
    {
        [SerializeField] private BattleFlowManager expeditionFlowManager;
        protected override void Configure(IContainerBuilder builder)
        {
            builder.RegisterComponent(expeditionFlowManager).As<IBattleFlowManager>();
        }
    }
}
