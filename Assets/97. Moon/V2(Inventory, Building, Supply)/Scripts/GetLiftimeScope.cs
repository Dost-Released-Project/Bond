using UnityEngine;
using VContainer;
using VContainer.Unity;

public class GameLifetimeScope : LifetimeScope
{
    protected override void Configure(IContainerBuilder builder)
    {
        builder.Register<ResourceManager>(Lifetime.Singleton);
        builder.Register<TotalInventory>(Lifetime.Singleton).WithParameter("capacity", 16).AsImplementedInterfaces().AsSelf();
        builder.Register<ExpeditionInventory>(Lifetime.Singleton).WithParameter("capacity", 2).AsImplementedInterfaces().AsSelf();
        builder.Register<InventoryTransferService>(Lifetime.Singleton);

        // 씬에 있는 InventoryView를 등록하면, [Inject]가 붙은 메서드에 자동으로 의존성이 주입됩니다.
        builder.RegisterComponentInHierarchy<InventoryView>();
    }
}