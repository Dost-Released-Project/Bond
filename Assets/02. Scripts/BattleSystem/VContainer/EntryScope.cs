using BattleSystem.Interface;
using UnityEngine;
using UnityEngine.Serialization;
using VContainer;
using VContainer.Unity;

namespace BattleSystem.VContainer
{
    public class EntryScope : LifetimeScope
    {
        public CharacterSlot[] playerCharacterSlots;
        public CharacterSlot[] enemyCharacterSlots;
        
        protected override void Configure(IContainerBuilder builder)
        {
            builder.RegisterEntryPoint<BattleStageEntry>(Lifetime.Singleton).As<IBattleStageEntry>();
        }
    }
}
