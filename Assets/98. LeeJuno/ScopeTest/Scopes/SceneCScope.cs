using VContainer;
using VContainer.Unity;

/// <summary>
/// 씬 B용 Scope.
/// 
/// 
/// 
/// </summary>
public class SceneCScope : LifetimeScope
{
    protected override void Configure(IContainerBuilder builder)
    {
        builder.RegisterEntryPoint<SceneCEntryPoint>();
    }
}
