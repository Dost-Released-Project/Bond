using VContainer;
using VContainer.Unity;

/// <summary>
/// 씬 B용 Scope.
/// SharedService는 RootScope에서 상속받으므로 재등록하지 않는다.
/// 씬 전환 후 SceneBEntryPoint에서 Root의 SharedService ID와 동일한지 확인.
///
/// 상속 구조:
///   RootScope (SharedService Singleton)
///   └── SceneBScope  ← 이 Scope
/// </summary>
public class SceneBScope : LifetimeScope
{
    protected override void Configure(IContainerBuilder builder)
    {
        // SharedService → RootScope에서 상속, 재등록 불필요
        builder.RegisterEntryPoint<SceneBEntryPoint>();
    }
}
