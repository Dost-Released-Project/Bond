using _02._Scripts.BattleSystem.Interface;
using _03._PipeLine;
using juno_Test;
using Reactions;
using UnityEngine;
using VContainer;
using VContainer.Unity;

namespace _02._Scripts.BattleSystem
{
    public class BattleScope : LifetimeScope
    {
        [SerializeField] 
        private BattlePipeLineSo _BattlePipeLineSo;
        
        public CharacterSlot[] playerUnits;
        public CharacterSlot[] enemyUnits;
        
        protected override void Configure(IContainerBuilder builder)
        {
            builder.Register<BattleManager>(Lifetime.Singleton).As<IBattleManager>();
            builder.Register<ReactionSystem>(Lifetime.Singleton);
            builder.Register<FormationManager>(Lifetime.Singleton).As<IFormationManager>();
            builder.RegisterInstance(_BattlePipeLineSo).As<IBattlePipeLine>();
            builder.RegisterInstance(playerUnits);
            builder.RegisterInstance(enemyUnits);
        }
    }
}
