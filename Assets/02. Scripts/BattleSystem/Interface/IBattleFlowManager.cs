using System;

namespace BattleSystem.Interface
{
    public interface IBattleFlowManager
    {
        event Action<BaseCharacter[], BaseCharacter[]> OnBattle;
        event Action<bool> OnBattleEnd;

        void PartySetting(BaseCharacter[] playerUnits);
    
        void EnemySetting(BaseCharacter[] enemyUnits);
    
        void BattleSwitch();

        void HandleRetreat();
    }
}
