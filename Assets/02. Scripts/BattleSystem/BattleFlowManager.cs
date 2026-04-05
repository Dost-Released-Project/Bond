using System.Collections.Generic;
using System.Threading;
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
        private BattleManager _battleManager;
        private TurnManager _turnManager;
        private TestPlayer[] _unit;

        public void Init(BattleManager battleManager, TurnManager turnManager, TestPlayer[] unit)
        {
            _battleManager = battleManager;
            _turnManager = turnManager;
            _unit = unit;
        }

        private async UniTask StartBattle(TestPlayer[] unit, CancellationToken token)
        {
            _turnManager.RegisterUnit(unit);
            await _turnManager.StartBattleAsync(token);
        }

        private async void Update()
        {
            if (Keyboard.current != null && Keyboard.current.digit1Key.wasPressedThisFrame)
            {
                await StartBattle(_unit, default);
            }

            if (Keyboard.current != null && Keyboard.current.digit2Key.wasPressedThisFrame)
            {
                Debug.Log("Digit 2 key pressed");
                _battleManager.SkillApplyLogic(new BattleContext());
            }
        }
    }
}