using System.Collections.Generic;
using System.Threading;
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
        builder.Register<TurnManager>(Lifetime.Singleton);
        builder.RegisterComponent(ui);
        builder.Register<BattleEntryPoint>(Lifetime.Singleton).As<IBattleEntryPoint>();
    }
}

public class BattleEntryPoint : IBattleEntryPoint
{
    private readonly TurnManager _turnManager;

    [Inject]
    public BattleEntryPoint(TurnManager turnManager)
    {
        _turnManager = turnManager;
    }
    
    public async UniTask StartAsync(CancellationToken cancellation, 
        IEnumerable<ITurnUseUnit> unit)
    {
        _turnManager.RegisterUnit(unit);
        await _turnManager.StartBattleAsync(cancellation);
    }
}