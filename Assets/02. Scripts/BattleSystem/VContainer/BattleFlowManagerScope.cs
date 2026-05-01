using BattleStage;
using BattleSystem.Interface;
using UnityEngine;
using VContainer;
using VContainer.Unity;

namespace BattleSystem.VContainer
{
    public class BattleFlowManagerScope : LifetimeScope
    {
        [SerializeField] private BattleFlowManager expeditionFlowManager;
        
        public CharacterSlot[] slots;
        protected override void Configure(IContainerBuilder builder)
        {
            builder.RegisterComponent(expeditionFlowManager).As<IBattleFlowManager>();
            builder.Register<FormationManager>(Lifetime.Singleton).As<IFormationManager>();
            builder.RegisterInstance(slots);
        }
    }
}
