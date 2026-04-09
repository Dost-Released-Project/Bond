using UnityEngine;
using VContainer;
using VContainer.Unity;

/// <summary>
/// VContainer Scope 상속구조 테스트 - 자식 Scope.
///
/// 등록 내용:
///   - ChildOnlyService (Scoped) : 부모에 없는 자식 전용 서비스
///   - ICounter → CounterB (Scoped) : 부모의 CounterA를 오버라이드
///
/// 씬 구성:
///   1. 빈 GameObject에 이 컴포넌트를 추가 (이름: ChildScopeRoot)
///   2. Inspector의 Parent 필드에 ParentScopeRoot 오브젝트 할당 ← 핵심!
/// </summary>
public class ScopeTestChildScope : LifetimeScope
{
    protected override void Configure(IContainerBuilder builder)
    {
        // 부모에 없는 자식 전용 서비스
        builder.Register<ChildOnlyService>(Lifetime.Scoped);

        // 부모의 ICounter(CounterA)를 CounterB로 오버라이드
        builder.Register<ICounter, CounterB>(Lifetime.Scoped);

        builder.RegisterEntryPoint<ChildScopeEntryPoint>();
    }
}
