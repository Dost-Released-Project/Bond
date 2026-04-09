using UnityEngine;
using VContainer;
using VContainer.Unity;

/// <summary>
/// VContainer Scope 상속구조 테스트 - 부모 Scope.
///
/// 등록 내용:
///   - SharedService  (Singleton) : 자식 Scope에서 동일 인스턴스를 받는지 검증
///   - ScopedService  (Scoped)    : 자식이 오버라이드하지 않을 때 부모 인스턴스를 공유하는지 검증
///   - ICounter → CounterA (Scoped) : 자식 Scope에서 CounterB로 오버라이드되는지 검증
///
/// 씬 구성:
///   1. 빈 GameObject에 이 컴포넌트를 추가 (이름: ParentScopeRoot)
///   2. ScopeTestChildScope의 Parent 필드에 이 오브젝트 할당
/// </summary>
public class ScopeTestParentScope : LifetimeScope
{
    protected override void Configure(IContainerBuilder builder)
    {
        builder.Register<SharedService>(Lifetime.Singleton);
        builder.Register<ScopedService>(Lifetime.Scoped);  // 자식에서 오버라이드 안 함
        builder.Register<ICounter, CounterA>(Lifetime.Scoped);
        builder.RegisterEntryPoint<ParentScopeEntryPoint>();
    }
}
