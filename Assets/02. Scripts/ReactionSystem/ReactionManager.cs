using System;
using _02._Scripts.ReactionSystem.Event;

namespace _02._Scripts.ReactionSystem
{
    public class ReactionManager
    {
        public readonly EventManager eventManager;
        public void Register<T>(Reaction<T> reaction) where T: EventArgs
        {
            eventManager.AddListener<T>(reaction.Action);
        }

        public void Unregister<T>(Reaction<T> reaction) where T : EventArgs
        {
            eventManager.RemoveListener<T>(reaction.Action);
        }
    }
}