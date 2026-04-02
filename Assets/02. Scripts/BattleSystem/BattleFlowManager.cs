using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using juno_Test;
using UnityEngine;
using VContainer;
using VContainer.Unity;

namespace _02._Scripts.BattleSystem_KWT
{
    public class BattleFlowManager
    {
        [Inject]
        private readonly BattleManager battleManager;
        [Inject] 
        private readonly TurnManager turnManager;
        [Inject]
        private readonly BattleEntryPoint battleEntryPoint;
        
        private ITurnUseUnit[] _PlayerUnits;

        public void SetPlayerUnits(ITurnUseUnit[] playerUnits)
        {
            _PlayerUnits = playerUnits;
        }

        public void StartBattle(ITurnUseUnit[] enemyUnits)
        {
            battleEntryPoint.StartAsync(default, enemyUnits, _PlayerUnits).Forget();
        }
    }
}
