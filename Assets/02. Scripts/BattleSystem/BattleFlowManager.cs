using System;
using _03._PipeLine;
using Cysharp.Threading.Tasks;
using juno_Test;
using UnityEngine;
using UnityEngine.InputSystem;
using VContainer;
using VContainer.Unity;

namespace _02._Scripts.BattleSystem
{
    public class BattleFlowManager : MonoBehaviour, IBattleFlowManager
    {
        public event Action<BaseCharacter[], BaseCharacter[]> OnBattleStart;
        
        private BaseCharacter[] _PlayerUnits;
        private BaseCharacter[] _EnemyUnits;

        public void SetPlayerUnits(BaseCharacter[] playerUnits)
        {
            _PlayerUnits = playerUnits;
        }
        
        public void SetEnemyUnits(BaseCharacter[] enemyUnits)
        {
            _EnemyUnits = enemyUnits;
        }

        // ReSharper disable Unity.PerformanceAnalysis
        public void StartBattle()
        {
            _PlayerUnits = null;
            _EnemyUnits = null;
            OnBattleStart?.Invoke(_PlayerUnits, _EnemyUnits);
        }

        private void Update()
        {
            if (Keyboard.current != null && Keyboard.current.digit1Key.wasPressedThisFrame)
            {
                StartBattle();
                Debug.Log("Digit 1 key pressed, starting battle");
            }

            if (Keyboard.current != null && Keyboard.current.digit2Key.wasPressedThisFrame)
            {
                Debug.Log("Digit 2 key pressed");
            }
        }

        
    }
}
