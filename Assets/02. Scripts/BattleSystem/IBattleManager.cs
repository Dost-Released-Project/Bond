using _03._PipeLine;

namespace _02._Scripts.BattleSystem
{
    public interface IBattleManager
    {
        public BattleContext SkillApplyLogic(BattleContext context);
    }
}
