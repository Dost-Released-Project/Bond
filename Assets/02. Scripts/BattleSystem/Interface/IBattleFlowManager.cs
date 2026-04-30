using System;
using UnityEngine;

public interface IBattleFlowManager
{
    event Action<BaseCharacter[], BaseCharacter[]> OnBattleStart;

    void PartySetting(BaseCharacter[] playerUnits);
    
    void EnemySetting(BaseCharacter[] enemyUnits);
    
    void StartBattle();
}
