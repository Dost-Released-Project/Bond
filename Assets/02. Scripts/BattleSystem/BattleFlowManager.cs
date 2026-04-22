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
        private IBattleEntryPoint battleEntryPoint;
        private IBattleManager battleManager;
        private BaseCharacter[] _PlayerUnits;
        
        [Inject]
        public void Construct(IBattleEntryPoint battleEntryPoint, 
            IBattleManager battleManager, BaseCharacter[] playerUnits)
        {
            this.battleEntryPoint = battleEntryPoint;
            this.battleManager = battleManager;
            _PlayerUnits = playerUnits;
        }

        public void SetPlayerUnits(BaseCharacter[] playerUnits)
        {
            _PlayerUnits = playerUnits;
        }

        // ReSharper disable Unity.PerformanceAnalysis
        public void StartBattle(BaseCharacter[] units)
        {
            battleEntryPoint.StartAsync(default, units).Forget();
        }

        private void Update()
        {
            if (Keyboard.current != null && Keyboard.current.digit1Key.wasPressedThisFrame)
            {
                StartBattle(_PlayerUnits);
            }

            if (Keyboard.current != null && Keyboard.current.digit2Key.wasPressedThisFrame)
            {
                Debug.Log("Digit 2 key pressed");
                battleManager.SkillApplyLogic(new BattleContext());
            }
        }
    }
}
