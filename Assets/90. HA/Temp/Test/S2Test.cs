using Bond.Expedition;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;
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
            
            if (Keyboard.current.numpad9Key.wasPressedThisFrame)
            {
                SceneManager.LoadSceneAsync("Town");
            }
        }
    }
}