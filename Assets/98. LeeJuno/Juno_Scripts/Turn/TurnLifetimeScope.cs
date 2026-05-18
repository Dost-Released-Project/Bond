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

        // 씬에 배치된 MonoBehaviour 를 DI 대상으로 등록
        if (_ui != null)
            builder.RegisterComponent(_ui);
        else
            Debug.LogError("[TurnLifetimeScope] _ui 가 연결되지 않았습니다. Inspector 에서 TurnUI 를 연결하세요.", this);
    }
}