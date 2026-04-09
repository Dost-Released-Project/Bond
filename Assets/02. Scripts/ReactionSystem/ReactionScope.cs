using VContainer;
using VContainer.Unity;

namespace Reactions
{
    public class ReactionScope : LifetimeScope
    {
        public Ha.Test t;
        protected override void Configure(IContainerBuilder builder)
        {
            builder.Register<ReactionSystem>(Lifetime.Singleton);
            builder.Register<EventBus>(Lifetime.Singleton);
            builder.RegisterComponent(t);
        }
    }
}