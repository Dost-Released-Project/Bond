using VContainer;
using VContainer.Unity;

/// <summary>
/// 씬 B용 Scope.
/// SharedService를 Singleton으로 새로 등록한다.
/// 씬 전환 후 이 Scope의 SharedService ID가 씬 A와 다르면
/// → 씬 A의 Singleton이 파괴되고 새로 생성된 것.
/// </summary>
public class SceneBScope : LifetimeScope
{
    protected override void Configure(IContainerBuilder builder)
    {
        builder.Register<SharedService>(Lifetime.Singleton);
        builder.RegisterEntryPoint<SceneBEntryPoint>();
    }
}
