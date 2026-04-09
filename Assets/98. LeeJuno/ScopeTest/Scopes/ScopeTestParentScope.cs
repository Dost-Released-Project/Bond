using UnityEngine;
using VContainer;
using VContainer.Unity;

/// <summary>
/// VContainer Scope 상속구조 테스트 - 씬 A Scope.
///
/// 상속 구조:
///   RootScope (SharedService Singleton, RootScopedService Scoped)
///   └── ScopeTestParentScope  ← 이 Scope
///       └── ScopeTestChildScope
/// </summary>
public class ScopeTestParentScope : LifetimeScope
{
    protected override void Configure(IContainerBuilder builder)
    {
        // SharedService → RootScope에서 상속, 재등록 불필요
        builder.Register<ParentSingletonService>(Lifetime.Singleton);
        builder.Register<ScopedService>(Lifetime.Scoped);
        builder.Register<ICounter, CounterA>(Lifetime.Scoped);
        builder.RegisterEntryPoint<ParentScopeEntryPoint>();
    }
}
