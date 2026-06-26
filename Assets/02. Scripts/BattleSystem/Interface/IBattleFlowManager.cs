using System;

namespace BattleSystem.Interface
{
    public enum BattleEndStatus
    {
        Victory,
        Defeat,
        Retreat
    }

    public interface IBattleFlowManager
    {
        event Action<BaseCharacter[], BaseCharacter[]> OnBattle;
        event Action<BattleEndStatus> OnBattleEnd;

        void PartySetting(BaseCharacter[] playerUnits);
    
        void EnemySetting(BaseCharacter[] enemyUnits);
    
        void BattleSwitch();

        void HandleRetreat();
    }
}
