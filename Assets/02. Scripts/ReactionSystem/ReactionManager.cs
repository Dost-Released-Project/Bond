using System;
using _02._Scripts.ReactionSystem.Event;
using VContainer;

namespace _02._Scripts.ReactionSystem
{
    public class ReactionManager
    {
        [Inject]
        private readonly EventBus eventBus;
        
        public void Register<T>(Reaction<T> reaction) where T: EventArgs
        {
            eventBus.Subscribe<T>(reaction.Action);
        }

        public void Unregister<T>(Reaction<T> reaction) where T : EventArgs
        {
            eventBus.Unsubscribe<T>(reaction.Action);
        }
    }
}