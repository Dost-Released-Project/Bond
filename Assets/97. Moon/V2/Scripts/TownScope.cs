using _90._HA.Temp.Test;
using Bond.Embark;
using UnityEngine;
using VContainer;
using VContainer.Unity;

public class TownScope : LifetimeScope
{
    protected override void Configure(IContainerBuilder builder)
    {
        // Manager & Service (싱글톤처럼 유지)
        // 인벤토리
        builder.Register<TotalInventory>(Lifetime.Singleton).WithParameter("capacity", 16).AsImplementedInterfaces().AsSelf();
        builder.Register<ExpeditionInventory>(Lifetime.Scoped).WithParameter("capacity", 2).AsImplementedInterfaces().AsSelf();
        builder.Register<InventoryTransferService>(Lifetime.Singleton);
        builder.Register<InventoryUIService>(Lifetime.Singleton);
        // 자원 및 건물
        builder.Register<ResourceManager>(Lifetime.Singleton);
        builder.Register<BuildingService>(Lifetime.Singleton);
        // 장비
        builder.Register<CharacterItemService>(Lifetime.Singleton);
        //
        builder.Register<EmbarkManager>(Lifetime.Scoped);
        builder.Register<PartyManager>(Lifetime.Scoped).AsImplementedInterfaces().AsSelf();
        builder.Register<StageCoach>(Lifetime.Scoped);


        // 씬에 배치된 컴포넌트
        builder.RegisterComponentInHierarchy<InventoryView>();
        builder.RegisterComponentInHierarchy<ResourceView>();
        builder.RegisterComponentInHierarchy<SupplyView>();
        builder.RegisterComponentInHierarchy<SettlementManager>();
        builder.RegisterComponentInHierarchy<ConstructionUI>();
        builder.RegisterComponentInHierarchy<InteractionManager>();
        builder.RegisterComponentInHierarchy<SupplyManager>();
        builder.RegisterComponentInHierarchy<ExpeditionInventoryView>();
        builder.RegisterComponentInHierarchy<EquipmentSlotUI>();
        builder.RegisterComponentInHierarchy<AccessoryBagView>();
        
        // 테스트용 스크립트
        builder.RegisterComponentInHierarchy<S1Test>();
    }
}