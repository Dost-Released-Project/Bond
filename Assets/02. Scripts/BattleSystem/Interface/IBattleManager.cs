using _03._PipeLine;

namespace BattleSystem.Interface
{
    public interface IBattleManager
    {
        public BattleContext SkillApplyLogic(BattleContext context);
    }
}
