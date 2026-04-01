using VContainer;
using VContainer.Unity;

namespace _02._Scripts.BattleSystem_KWT
{
    public class BattleScope : LifetimeScope
    {
        protected override void Configure(IContainerBuilder builder)
        {
            // 1. 순수 로직 클래스 등록
            builder.Register<BattleManager>(Lifetime.Scoped);
        
            // 2. 흐름 제어 클래스 등록 (인터페이스를 쓰면 더 좋음)
            //builder.Register<BattleFlowManager>(Lifetime.Scoped);
            
            builder.RegisterEntryPoint<BattleFlowManager>(Lifetime.Scoped);
        }
    }
}
