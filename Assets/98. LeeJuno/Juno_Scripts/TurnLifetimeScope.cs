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
    private readonly TestPlayer[] _units;
    private readonly BattleFlowManager _battleFlowManager;
    private readonly BattleManager _battleManager;

    [Inject]
    public BattleEntryPoint(TurnManager turnManager, TestPlayer[] units, BattleFlowManager battleFlowManager,
        BattleManager battleManager)
    {
        _turnManager = turnManager;
        _units = units;
        _battleFlowManager = battleFlowManager;
        _battleManager = battleManager;
    }

    public async UniTask StartAsync(CancellationToken cancellation)
    {
        _battleFlowManager.Init(_battleManager, _turnManager, _units);
    }
}