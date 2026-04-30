using Bond.Expedition;
using VContainer;
using VContainer.Unity;

public class RootScope : LifetimeScope
{
    protected override void Configure(IContainerBuilder builder)
    {
        builder.Register<ExpeditionPayload>(Lifetime.Singleton);
        // SatageContext 추가될 예정
    }
}