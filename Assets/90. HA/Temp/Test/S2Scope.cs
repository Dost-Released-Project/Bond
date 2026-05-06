using _90._HA.Temp.Test;
using UnityEngine;
using VContainer;
using VContainer.Unity;

public class S2Scope : LifetimeScope
{
    protected override void Configure(IContainerBuilder builder)
    {
        builder.RegisterComponentInHierarchy<S2Test>();
    }
}
