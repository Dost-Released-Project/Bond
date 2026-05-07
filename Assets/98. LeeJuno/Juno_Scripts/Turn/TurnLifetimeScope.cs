using System.Collections.Generic;
using System.Threading;
using Cysharp.Threading.Tasks;
using juno_Test;
using UnityEngine;
using VContainer;
using VContainer.Unity;

public class TurnLifetimeScope : LifetimeScope
{
    [SerializeField] private TurnUI _ui;

    protected override void Configure(IContainerBuilder builder)
    {
        builder.RegisterEntryPoint<TurnManager>(Lifetime.Singleton).AsSelf();
        builder.RegisterComponent(_ui);
    }
}