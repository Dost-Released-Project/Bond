using UnityEngine;
using VContainer;
using VContainer.Unity;

namespace _02._Scripts.BattleSystem
{
    public class BattleFlowManagerScope : LifetimeScope
    {
        [SerializeField] private ExpeditionFlowManager _ExpaditionFlowManager;
        protected override void Configure(IContainerBuilder builder)
        {
            builder.RegisterComponent(_ExpaditionFlowManager).As<IExpeditionFlowManager>();
        }
    }
}
