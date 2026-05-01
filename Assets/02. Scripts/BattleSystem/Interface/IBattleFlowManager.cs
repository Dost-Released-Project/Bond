using System;

namespace BattleSystem.Interface
{
    public interface IBattleFlowManager
    {
        event Action<BaseCharacter[], BaseCharacter[]> OnBattle;

        void PartySetting(BaseCharacter[] playerUnits);
    
        void EnemySetting(BaseCharacter[] enemyUnits);
    
        void StartBattle();
    }
}
