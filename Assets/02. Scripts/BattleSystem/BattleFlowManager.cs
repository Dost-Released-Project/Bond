using System;
using UnityEngine;
using UnityEngine.InputSystem;
using VContainer;
using VContainer.Unity;

namespace _02._Scripts.BattleSystem_KWT
{
    public class BattleFlowManager : ITickable
    {
        [Inject]
        private BattleManager m_Bm;
        
        public void Tick()
        {
            if (Keyboard.current != null && Keyboard.current.spaceKey.wasPressedThisFrame){
                if(m_Bm != null)
                {
                    Debug.Log(m_Bm.TestCall());
                }
            }
        }
    }
}
