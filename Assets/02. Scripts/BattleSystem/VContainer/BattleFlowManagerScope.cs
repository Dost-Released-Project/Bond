using UnityEngine;
using VContainer;
using VContainer.Unity;

namespace _02._Scripts.BattleSystem
{
    public class BattleFlowManagerScope : LifetimeScope
    {
        [SerializeField] private ExpaditionFlowManager _BattleFlowManager;
        protected override void Configure(IContainerBuilder builder)
        {
            builder.RegisterComponent(_BattleFlowManager).As<IExpaditionFlowManager>();
        }
    }
}
