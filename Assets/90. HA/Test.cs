using Reactions;
using UnityEngine;
using UnityEngine.InputSystem;
using VContainer;

namespace Ha
{
    public class Test : MonoBehaviour
    {
        [Inject] private ReactionSystem reactionSystem;

        [Inject] private EventBus eventBus;

        private void Start()
        {
            for (int i = 0; i < 10; i++)
            {
                reactionSystem.Register(new Reaction(i));
            }
        }
    }
}