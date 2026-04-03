using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using juno_Test;
using UnityEngine;
using UnityEngine.InputSystem;
using VContainer;
using VContainer.Unity;

namespace _02._Scripts.BattleSystem_KWT
{
    public class BattleFlowManager : ITickable
    {
        [Inject] 
        private readonly IBattleEntryPoint battleEntryPoint;
        
        [Inject]
        private TestPlayer[] _PlayerUnits;

        public void SetPlayerUnits(TestPlayer[] playerUnits)
        {
            _PlayerUnits = playerUnits;
        }

        public void StartBattle(TestPlayer[] units)
        {
            battleEntryPoint.StartAsync(default, units).Forget();
        }
        
        public void Tick()
        {
            if (Keyboard.current != null && Keyboard.current.digit1Key.wasPressedThisFrame)
            {
                StartBattle(_PlayerUnits);
            }
        }
    }
}
