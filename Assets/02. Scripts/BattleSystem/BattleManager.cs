using _03._PipeLine;
using VContainer;

namespace _02._Scripts.BattleSystem
{
    public class BattleManager : IBattleManager
    {
        [Inject]
        private readonly IBattlePipeLine skillApplyPipeline;

        public BattleContext SkillApplyLogic(BattleContext context)
        {
            // [입구] 로직 파이프라인 실행
            if (skillApplyPipeline != null)
            {
                return skillApplyPipeline.Run(context);
            }

            return context;
        }
    }
}
