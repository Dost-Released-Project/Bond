using UnityEngine;
using VContainer;
using VContainer.Unity;

public class GameLifetimeScope : LifetimeScope
{
    protected override void Configure(IContainerBuilder builder)
    {
        // Manager & Service (싱글톤처럼 유지)
        // 인벤토리
        builder.Register<TotalInventory>(Lifetime.Singleton).WithParameter("capacity", 16).AsImplementedInterfaces().AsSelf();
        builder.Register<ExpeditionInventory>(Lifetime.Singleton).WithParameter("capacity", 2).AsImplementedInterfaces().AsSelf();
        builder.Register<InventoryTransferService>(Lifetime.Singleton);
        // 자원 및 건물
        builder.Register<ResourceManager>(Lifetime.Singleton);
        builder.Register<BuildingService>(Lifetime.Singleton);

        // View (씬에 배치된 컴포넌트)
        builder.RegisterComponentInHierarchy<InventoryView>();
        builder.RegisterComponentInHierarchy<ResourceView>();
    }
}