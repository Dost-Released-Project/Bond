using System.Collections.Generic;
using _03._PipeLine;
using Cysharp.Threading.Tasks;
using juno_Test;
using UnityEngine;
using UnityEngine.InputSystem;
using VContainer;
using VContainer.Unity;

namespace _02._Scripts.BattleSystem
{
    public class BattleFlowManager : MonoBehaviour
    {
       

        [Inject]
        private readonly BattleManager battleManager;

        
        public void SetPlayerUnits(TestPlayer[] playerUnits)
        {
        }

        // ReSharper disable Unity.PerformanceAnalysis
        public void StartBattle(IEnumerable<ITurnUseUnit> units)
        {
           
        }

        private void Update()
        {
            if (Keyboard.current != null && Keyboard.current.digit1Key.wasPressedThisFrame)
            {
                //StartBattle();
            }

            if (Keyboard.current != null && Keyboard.current.digit2Key.wasPressedThisFrame)
            {
                Debug.Log("Digit 2 key pressed");
                battleManager.SkillApplyLogic(new BattleContext());
            }
        }
    }
}
