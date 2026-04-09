using ReactionSystem.Event;
using VContainer;
using VContainer.Unity;

namespace ReactionSystem
{
    public class ReactionScope : LifetimeScope
    {
        public Test t;
        protected override void Configure(IContainerBuilder builder)
        {
            builder.Register<EventBus>(Lifetime.Singleton);
            builder.Register<ReactionSystem>(Lifetime.Singleton);
            builder.RegisterComponent(t);
        }
    }
}