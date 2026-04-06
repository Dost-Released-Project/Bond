using ReactionSystem.Event;
using UnityEngine;
using UnityEngine.InputSystem;
using VContainer;

namespace ReactionSystem
{
    public class Test : MonoBehaviour
    {
        [Inject]
        private ReactionSystem _reactionSystem;
        
        [Inject]
        private EventBus _eventBus;
        
        private void Update()
        {
            if (Keyboard.current.numpad0Key.wasPressedThisFrame)
            {
                var trigger = new Trigger<AttackEventArgs>(args => true);
                _reactionSystem.Register(new Reaction<AttackEventArgs>(null, trigger, args => Debug.Log("sex")));
            }

            if (Keyboard.current.numpad1Key.wasPressedThisFrame)
            {
                _eventBus.Publish<AttackEventArgs>(new AttackEventArgs());
            }
        }
    }
}