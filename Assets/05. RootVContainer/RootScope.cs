using Bond.Expedition;
using Bond.Persistence;
using VContainer;
using VContainer.Unity;

namespace RootVContainer
{
    public class RootScope : LifetimeScope
    {
        protected override void Configure(IContainerBuilder builder)
        {
            builder.Register<ExpeditionPayload>(Lifetime.Singleton);
            builder.Register<SaveLoadSystem>(Lifetime.Singleton);
            // SatageContext 추가될 예정
        }
    }
}