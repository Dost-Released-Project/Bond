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
            
            builder.Register<CharacterItemService>(Lifetime.Singleton);
            builder.Register<InventoryTransferService>(Lifetime.Singleton);
            
            builder.RegisterComponentInHierarchy<ExpeditionInventoryView>();
            builder.RegisterComponentInHierarchy<EquipmentSlotUI>();

            // Journal 연동 Provider (스코프 시작 시 JournalSystem에 자가 등록)
            builder.RegisterEntryPoint<Bond.WT.Journal.BattleEventProvider>(Lifetime.Singleton).AsSelf();
        }
    }
}
