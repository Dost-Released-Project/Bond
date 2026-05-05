using _90._HA.Temp.Test;
using UnityEngine;
using VContainer;
using VContainer.Unity;

public class S2Scope : LifetimeScope
{
    protected override void Configure(IContainerBuilder builder)
    {
        builder.Register<CharacterItemService>(Lifetime.Singleton);
        builder.Register<InventoryTransferService>(Lifetime.Singleton);
        
        builder.RegisterComponentInHierarchy<S2Test>();
        builder.RegisterComponentInHierarchy<ExpeditionInventoryView>();
    }
}
