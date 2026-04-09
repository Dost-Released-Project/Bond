using UnityEngine;
using UnityEngine.InputSystem;
using VContainer;

namespace Ha
{
    public class Test : MonoBehaviour
    {
        [Inject]
        private Reactions.ReactionSystem _reactionSystem;
        
        [Inject]
        private EventBus _eventBus;
        
        private void Update()
        {
            
        }
    }
}