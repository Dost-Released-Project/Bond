using PipeLine;

namespace Skills.Effects
{
    public enum SkillEffectType
    {
        None = 0,
        HpChange = 1,
        Buff = 2
    }

    public interface ISkillEffect
    {
        void Execute(BattleContext context, SkillBase skill);
    }
}