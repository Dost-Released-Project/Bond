using UnityEngine;
using VContainer;
using VContainer.Unity;

/// <summary>
/// 앱 전체 생명주기를 가지는 Root Scope.
/// VContainerSettings의 RootLifetimeScope 필드에 이 컴포넌트가 붙은 프리팹을 할당하면
/// 씬 전환과 무관하게 살아남는 최상위 컨테이너가 된다.
///
/// 등록된 Singleton은 씬이 바뀌어도 파괴되지 않는다.
/// </summary>
public class RootScope : LifetimeScope
{
    protected override void Configure(IContainerBuilder builder)
    {
        builder.Register<SharedService>(Lifetime.Singleton);
        builder.Register<RootScopedService>(Lifetime.Scoped);  // 씬 Scope에서 새 인스턴스 생성되는지 테스트
        builder.RegisterEntryPoint<RootScopeEntryPoint>();
    }
}
