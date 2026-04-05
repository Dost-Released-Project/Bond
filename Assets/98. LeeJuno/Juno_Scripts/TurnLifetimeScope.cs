using System.Collections.Generic;
using System.Threading;
using _02._Scripts.BattleSystem;
using Cysharp.Threading.Tasks;
using juno_Test;
using UnityEngine;
using VContainer;
using VContainer.Unity;

public class TurnLifetimeScope : LifetimeScope
{
    [SerializeField] private TurnUI ui;

    protected override void Configure(IContainerBuilder builder)
    {
        builder.Register<TurnManager>(Lifetime.Scoped);
        builder.RegisterComponent(ui);
        builder.RegisterEntryPoint<BattleEntryPoint>();
    }
}

public class BattleEntryPoint : IAsyncStartable
{
    private readonly TurnManager _turnManager;
    private readonly TestPlayer[]  _units;

    [Inject]
    public BattleEntryPoint(TurnManager turnManager, TestPlayer[] units)
    {
        _turnManager = turnManager;
        _units = units;
    }

    public async UniTask StartAsync(CancellationToken cancellation)
    {
         _turnManager.RegisterUnit(_units);
        await _turnManager.StartBattleAsync(cancellation);
        
    }
}