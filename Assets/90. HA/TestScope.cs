using Bond.PartyManagement;
using VContainer;
using VContainer.Unity;

public class TestScope : LifetimeScope
{
    public Ha.Test t;
    protected override void Configure(IContainerBuilder builder)
    {
        builder.Register<PartySystem>(Lifetime.Singleton);
        builder.RegisterComponent(t);
    }
}