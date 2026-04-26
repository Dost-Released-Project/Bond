using Bond.Embark;
using VContainer;
using VContainer.Unity;

public class TestScope : LifetimeScope
{
    public Ha.Test t;
    protected override void Configure(IContainerBuilder builder)
    {
        builder.Register<PartyManager>(Lifetime.Singleton);
        builder.RegisterComponent(t);
    }
}