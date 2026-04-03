using System.Threading;
using Cysharp.Threading.Tasks;
using juno_Test;
using UnityEngine;
using VContainer;
using VContainer.Unity;

public class TurnLifetimeScope : LifetimeScope
{
    // 임시) 인스펙터에서 플레이어 할당
    [SerializeField] private TestPlayer[] sceneUnit;
    [SerializeField] private TurnUI ui;

    protected override void Configure(IContainerBuilder builder)
    {
        builder.Register<TurnManager>(Lifetime.Scoped);
        builder.RegisterInstance(sceneUnit);
        builder.RegisterComponent(ui);
        builder.RegisterEntryPoint<BattleEntryPoint>();
    }
}

public class BattleEntryPoint : IAsyncStartable
{
    private readonly TurnManager _turnManager;
    private readonly TestPlayer[] _unit;

    [Inject]
    public BattleEntryPoint(TurnManager turnManager, TestPlayer[] unit)
    {
        _turnManager = turnManager;
        _unit = unit;
    }

    public async UniTask StartAsync(CancellationToken cancellation)
    {
        _turnManager.RegisterUnit(_unit);
        await _turnManager.StartBattleAsync(cancellation);
    }
}