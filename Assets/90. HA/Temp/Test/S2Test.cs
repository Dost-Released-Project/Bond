using Bond.Expedition;
using UnityEngine;
using UnityEngine.InputSystem;
using VContainer;

namespace _90._HA.Temp.Test
{
    public class S2Test : MonoBehaviour
    {
        [Inject] ExpeditionPayload payload;

        private void Update()
        {
            if (Keyboard.current.numpad0Key.wasPressedThisFrame)
            {
                Debug.Log(payload);
                
            }
        }
    }
}