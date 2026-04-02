using System.Collections.Generic;
using System.Threading;
using Cysharp.Threading.Tasks;
using VContainer;
using VContainer.Unity;

namespace _02._Scripts.BattleSystem_KWT
{
    public class BattleScope : LifetimeScope
    {
        public Tester tester;
        protected override void Configure(IContainerBuilder builder)
        {
            builder.Register<BattleManager>(Lifetime.Scoped);
            builder.Register<BattleEntryPoint>(Lifetime.Scoped);
            builder.Register<TurnManager>(Lifetime.Scoped);
            
            builder.Register<BattleFlowManager>(Lifetime.Scoped);
            builder.RegisterComponent(tester);
        }
    }
    
    public class BattleEntryPoint
    {
        private readonly TurnManager turnManager;

        [Inject]
        public BattleEntryPoint(TurnManager turnManager)
        {
            this.turnManager = turnManager;
        }

        public async UniTask StartAsync(CancellationToken cancellation,
            IEnumerable<ITurnUseUnit> enemy,
            IEnumerable<ITurnUseUnit> playerUnit)
        {
            turnManager.RegisterUnit(playerUnit);
            turnManager.RegisterUnit(enemy);
            await turnManager.StartBattleAsync(cancellation);
        }
    }
}
