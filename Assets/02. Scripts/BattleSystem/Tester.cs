using _02._Scripts.BattleSystem_KWT;
using juno_Test;
using UnityEngine;
using UnityEngine.InputSystem;
using VContainer;

public class Tester : MonoBehaviour
{
    [Inject]
    private readonly BattleFlowManager battleFlowManager;
    
    public TestPlayer[] testPlayers;
    public TestPlayer[] testEnemies;

    private void Update()
    {
        if (Keyboard.current != null && Keyboard.current.digit1Key.wasPressedThisFrame)
        {
            battleFlowManager.SetPlayerUnits(testPlayers);
            battleFlowManager.StartBattle(testEnemies);
        }
    }
}
